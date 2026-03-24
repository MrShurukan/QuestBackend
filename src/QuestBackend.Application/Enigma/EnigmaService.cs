using Microsoft.EntityFrameworkCore;
using QuestBackend.Application.Abstractions;
using QuestBackend.Application.Shared;
using QuestBackend.Application.Teams;
using QuestBackend.Contracts;
using QuestBackend.Domain.Config;
using QuestBackend.Domain.Enigma;
using QuestBackend.Domain.Shared;
using QuestBackend.Domain.Teams;

namespace QuestBackend.Application.Enigma;

public sealed class EnigmaService
{
    private readonly IClock _clock;
    private readonly ICurrentPrincipal _currentPrincipal;
    private readonly IEnigmaEvaluator _enigmaEvaluator;
    private readonly IQuestDayLifecycleGate _questDayLifecycleGate;
    private readonly IQuestDbContext _dbContext;
    private readonly TeamService _teamService;

    public EnigmaService(
        IQuestDbContext dbContext,
        TeamService teamService,
        IQuestDayLifecycleGate questDayLifecycleGate,
        IEnigmaEvaluator enigmaEvaluator,
        ICurrentPrincipal currentPrincipal,
        IClock clock)
    {
        _dbContext = dbContext;
        _teamService = teamService;
        _questDayLifecycleGate = questDayLifecycleGate;
        _enigmaEvaluator = enigmaEvaluator;
        _currentPrincipal = currentPrincipal;
        _clock = clock;
    }

    public async Task<EnigmaStateResponse> GetStateAsync(CancellationToken cancellationToken = default)
    {
        Team team = await EnsureCurrentTeamAsync(cancellationToken);
        EnigmaProfile profile = await GetCurrentProfileAsync(cancellationToken);
        GlobalSettings settings = await GetGlobalSettingsAsync(cancellationToken);

        HashSet<Guid> unlockedTagIds = await GetSolvedTagIdsForTeamAsync(team.Id, cancellationToken);
        Dictionary<Guid, int> draftPositions = await GetDraftPositionsAsync(team.Id, profile.Id, cancellationToken);

        EnigmaAttempt? latestAttempt = await _dbContext.EnigmaAttempts
            .AsNoTracking()
            .Where(x => x.TeamId == team.Id && x.EnigmaProfileId == profile.Id)
            .OrderByDescending(x => x.AttemptedAt)
            .FirstOrDefaultAsync(cancellationToken);

        int cooldownMinutes = settings.EnigmaCooldownMinutes > 0 ? settings.EnigmaCooldownMinutes : profile.AttemptCooldownMinutes;

        List<EnigmaRotorStateDto> rotors = profile.RotorDefinitions
            .OrderBy(x => x.DisplayOrder)
            .Select(
                x =>
                {
                    bool unlocked = unlockedTagIds.Contains(x.TagId);
                    int draft = draftPositions.TryGetValue(x.TagId, out int d) ? d : x.PositionMin;
                    draft = Clamp(draft, x.PositionMin, x.PositionMax);
                    return new EnigmaRotorStateDto(
                        x.Id,
                        x.TagId,
                        x.Tag.Name,
                        x.ColorOverride ?? x.Tag.Color,
                        x.Label,
                        x.DisplayOrder,
                        x.PositionMin,
                        x.PositionMax,
                        x.IsActive,
                        unlocked,
                        draft);
                })
            .ToList();

        return new EnigmaStateResponse(
            profile.Id,
            profile.Mode.ToString(),
            cooldownMinutes,
            latestAttempt?.CooldownAppliedUntil,
            rotors,
            _clock.UtcNow);
    }

    public async Task SaveDraftPositionsAsync(UpdateEnigmaDraftPositionsRequest request, CancellationToken cancellationToken = default)
    {
        Team team = await EnsureCurrentTeamAsync(cancellationToken);
        QuestLifecycleDecision lifecycleDecision = await _questDayLifecycleGate.GetDecisionAsync(cancellationToken);
        if (!lifecycleDecision.AllowsSubmissions)
        {
            throw new AppException(
                409,
                lifecycleDecision.Status == Domain.QuestDay.QuestDayStatus.DayClosed ? "День завершён." : "Квест ещё не начат.");
        }

        EnigmaProfile profile = await GetCurrentProfileAsync(cancellationToken);
        HashSet<Guid> unlockedTagIds = await GetSolvedTagIdsForTeamAsync(team.Id, cancellationToken);

        Dictionary<Guid, EnigmaRotorDefinition> rotorByTag = profile.RotorDefinitions
            .Where(x => x.IsActive)
            .ToDictionary(x => x.TagId, x => x);

        Dictionary<Guid, int> existing = await GetDraftPositionsAsync(team.Id, profile.Id, cancellationToken);
        foreach (KeyValuePair<Guid, int> pair in request.Positions)
        {
            if (!rotorByTag.TryGetValue(pair.Key, out EnigmaRotorDefinition? def))
            {
                continue;
            }

            if (!unlockedTagIds.Contains(pair.Key))
            {
                continue;
            }

            existing[pair.Key] = Clamp(pair.Value, def.PositionMin, def.PositionMax);
        }

        TeamEnigmaDraft? draft = await _dbContext.TeamEnigmaDrafts
            .FirstOrDefaultAsync(x => x.TeamId == team.Id && x.EnigmaProfileId == profile.Id, cancellationToken);

        if (draft is null)
        {
            draft = new TeamEnigmaDraft
            {
                TeamId = team.Id,
                EnigmaProfileId = profile.Id,
                PositionsJson = AppJson.Serialize(existing),
            };
            await _dbContext.TeamEnigmaDrafts.AddAsync(draft, cancellationToken);
        }
        else
        {
            draft.PositionsJson = AppJson.Serialize(existing);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<SubmitEnigmaAttemptResponse> SubmitAttemptAsync(SubmitEnigmaAttemptRequest request, CancellationToken cancellationToken = default)
    {
        Team team = await EnsureCurrentTeamAsync(cancellationToken);
        QuestLifecycleDecision lifecycleDecision = await _questDayLifecycleGate.GetDecisionAsync(cancellationToken);
        if (!lifecycleDecision.AllowsSubmissions)
        {
            return new SubmitEnigmaAttemptResponse(
                lifecycleDecision.Status == Domain.QuestDay.QuestDayStatus.DayClosed ? "day_closed" : "not_started",
                lifecycleDecision.Message,
                null,
                _clock.UtcNow);
        }

        EnigmaProfile profile = await GetCurrentProfileAsync(cancellationToken);
        GlobalSettings settings = await GetGlobalSettingsAsync(cancellationToken);
        int cooldownMinutes = settings.EnigmaCooldownMinutes > 0 ? settings.EnigmaCooldownMinutes : profile.AttemptCooldownMinutes;

        EnigmaAttempt? latestAttempt = await _dbContext.EnigmaAttempts
            .Where(x => x.TeamId == team.Id && x.EnigmaProfileId == profile.Id)
            .OrderByDescending(x => x.AttemptedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (latestAttempt?.CooldownAppliedUntil is DateTimeOffset cooldownUntil && cooldownUntil > _clock.UtcNow)
        {
            EnigmaAttempt blockedAttempt = new()
            {
                TeamId = team.Id,
                EnigmaProfileId = profile.Id,
                AttemptedAt = _clock.UtcNow,
                SubmittedByUserId = _currentPrincipal.ParticipantUserId,
                InputJson = AppJson.Serialize(request.RotorPositions),
                Result = EnigmaAttemptResult.CooldownBlocked,
                CooldownAppliedUntil = cooldownUntil,
                EvaluationSnapshotJson = "{}",
                CreatedAt = _clock.UtcNow,
                UpdatedAt = _clock.UtcNow,
            };

            await _dbContext.EnigmaAttempts.AddAsync(blockedAttempt, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return new SubmitEnigmaAttemptResponse("cooldown", "Enigma cooldown is still active.", cooldownUntil, _clock.UtcNow);
        }

        HashSet<Guid> unlockedTagIds = await GetSolvedTagIdsForTeamAsync(team.Id, cancellationToken);
        Dictionary<Guid, int> normalized = NormalizePositionsForEvaluation(profile, request.RotorPositions, unlockedTagIds);
        EnigmaEvaluationResult evaluation = _enigmaEvaluator.Evaluate(profile, normalized);
        DateTimeOffset nextAllowedAt = _clock.UtcNow.AddMinutes(cooldownMinutes);

        EnigmaAttempt attempt = new()
        {
            TeamId = team.Id,
            EnigmaProfileId = profile.Id,
            AttemptedAt = _clock.UtcNow,
            SubmittedByUserId = _currentPrincipal.ParticipantUserId,
            InputJson = AppJson.Serialize(normalized),
            Result = evaluation.IsSuccess ? EnigmaAttemptResult.Success : EnigmaAttemptResult.Failure,
            CooldownAppliedUntil = nextAllowedAt,
            EvaluationSnapshotJson = evaluation.EvaluationSnapshotJson,
            CreatedAt = _clock.UtcNow,
            UpdatedAt = _clock.UtcNow,
        };

        await _dbContext.EnigmaAttempts.AddAsync(attempt, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new SubmitEnigmaAttemptResponse(
            evaluation.IsSuccess ? "success" : "failure",
            evaluation.IsSuccess ? profile.SuccessMessage : profile.FailureMessage,
            nextAllowedAt,
            _clock.UtcNow);
    }

    private static Dictionary<Guid, int> NormalizePositionsForEvaluation(
        EnigmaProfile profile,
        IReadOnlyDictionary<Guid, int> submitted,
        HashSet<Guid> unlockedTagIds)
    {
        Dictionary<Guid, int> expected = AppJson.Deserialize<Dictionary<Guid, int>>(profile.SecretCombinationJson) ?? [];
        Dictionary<Guid, EnigmaRotorDefinition> rotorByTag = profile.RotorDefinitions.ToDictionary(x => x.TagId, x => x);

        Dictionary<Guid, int> normalized = [];
        foreach (Guid tagId in expected.Keys)
        {
            rotorByTag.TryGetValue(tagId, out EnigmaRotorDefinition? def);
            int min = def?.PositionMin ?? 0;
            int max = def?.PositionMax ?? 0;
            if (!unlockedTagIds.Contains(tagId))
            {
                normalized[tagId] = min;
                continue;
            }

            if (!submitted.TryGetValue(tagId, out int value))
            {
                normalized[tagId] = min;
                continue;
            }

            normalized[tagId] = def is null ? value : Clamp(value, def.PositionMin, def.PositionMax);
        }

        return normalized;
    }

    private async Task<HashSet<Guid>> GetSolvedTagIdsForTeamAsync(Guid teamId, CancellationToken cancellationToken)
    {
        List<Guid> tagIds = await _dbContext.TeamQuestionStates
            .AsNoTracking()
            .Where(x => x.TeamId == teamId && x.IsSolved)
            .Select(x => x.Question.TagId)
            .Distinct()
            .ToListAsync(cancellationToken);

        return tagIds.ToHashSet();
    }

    private async Task<Dictionary<Guid, int>> GetDraftPositionsAsync(Guid teamId, Guid profileId, CancellationToken cancellationToken)
    {
        TeamEnigmaDraft? draft = await _dbContext.TeamEnigmaDrafts
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.TeamId == teamId && x.EnigmaProfileId == profileId, cancellationToken);

        if (draft is null)
        {
            return [];
        }

        return AppJson.Deserialize<Dictionary<Guid, int>>(draft.PositionsJson) ?? [];
    }

    private static int Clamp(int value, int min, int max)
    {
        if (value < min)
        {
            return min;
        }

        return value > max ? max : value;
    }

    private async Task<Team> EnsureCurrentTeamAsync(CancellationToken cancellationToken)
    {
        Team? team = await _teamService.GetCurrentParticipantTeamEntityAsync(cancellationToken);
        return team ?? throw new AppException(409, "Нужно состоять в активной команде.");
    }

    private async Task<EnigmaProfile> GetCurrentProfileAsync(CancellationToken cancellationToken)
    {
        GlobalSettings settings = await GetGlobalSettingsAsync(cancellationToken);

        EnigmaProfile? profile = null;
        if (settings.CurrentEnigmaProfileId is Guid currentProfileId)
        {
            profile = await _dbContext.EnigmaProfiles
                .Include(x => x.RotorDefinitions.OrderBy(y => y.DisplayOrder))
                .ThenInclude(x => x.Tag)
                .SingleOrDefaultAsync(x => x.Id == currentProfileId, cancellationToken);
        }

        profile ??= await _dbContext.EnigmaProfiles
            .Include(x => x.RotorDefinitions.OrderBy(y => y.DisplayOrder))
            .ThenInclude(x => x.Tag)
            .OrderByDescending(x => x.IsActive)
            .ThenBy(x => x.Name)
            .FirstAsync(cancellationToken);

        return profile;
    }

    private async Task<GlobalSettings> GetGlobalSettingsAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.GlobalSettings.FirstOrDefaultAsync(cancellationToken)
            ?? new GlobalSettings();
    }
}

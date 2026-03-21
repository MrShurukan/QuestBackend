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

        Dictionary<Guid, int> rewardCounts = await _dbContext.TeamRotorRewards
            .AsNoTracking()
            .Where(x => x.TeamId == team.Id && !x.IsRevoked)
            .GroupBy(x => x.TagId)
            .Select(x => new { x.Key, Count = x.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, cancellationToken);

        EnigmaAttempt? latestAttempt = await _dbContext.EnigmaAttempts
            .AsNoTracking()
            .Where(x => x.TeamId == team.Id && x.EnigmaProfileId == profile.Id)
            .OrderByDescending(x => x.AttemptedAt)
            .FirstOrDefaultAsync(cancellationToken);

        int cooldownMinutes = settings.EnigmaCooldownMinutes > 0 ? settings.EnigmaCooldownMinutes : profile.AttemptCooldownMinutes;

        return new EnigmaStateResponse(
            profile.Id,
            profile.Mode.ToString(),
            cooldownMinutes,
            latestAttempt?.CooldownAppliedUntil,
            profile.RotorDefinitions
                .OrderBy(x => x.DisplayOrder)
                .Select(
                    x => new EnigmaRotorDefinitionDto(
                        x.Id,
                        x.TagId,
                        x.Tag.Name,
                        x.ColorOverride ?? x.Tag.Color,
                        x.Label,
                        x.DisplayOrder,
                        x.PositionMin,
                        x.PositionMax,
                        x.IsActive,
                        rewardCounts.TryGetValue(x.TagId, out int count) ? count : 0))
                .ToList(),
            _clock.UtcNow);
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

        EnigmaEvaluationResult evaluation = _enigmaEvaluator.Evaluate(profile, request.RotorPositions);
        DateTimeOffset nextAllowedAt = _clock.UtcNow.AddMinutes(cooldownMinutes);

        EnigmaAttempt attempt = new()
        {
            TeamId = team.Id,
            EnigmaProfileId = profile.Id,
            AttemptedAt = _clock.UtcNow,
            SubmittedByUserId = _currentPrincipal.ParticipantUserId,
            InputJson = AppJson.Serialize(request.RotorPositions),
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

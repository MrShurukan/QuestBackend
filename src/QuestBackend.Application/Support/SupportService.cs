using Microsoft.EntityFrameworkCore;
using QuestBackend.Application.Abstractions;
using QuestBackend.Application.Shared;
using QuestBackend.Application.Teams;
using QuestBackend.Contracts;
using QuestBackend.Domain.Enigma;
using QuestBackend.Domain.Participants;
using QuestBackend.Domain.Progress;
using QuestBackend.Domain.Questions;
using QuestBackend.Domain.Shared;
using QuestBackend.Domain.Teams;

namespace QuestBackend.Application.Support;

public sealed class SupportService
{
    private const int MinParticipantPasswordLength = 8;

    private readonly IAuditWriter _auditWriter;
    private readonly IClock _clock;
    private readonly IQuestDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IQuestionRoutingResolver _questionRoutingResolver;

    public SupportService(
        IQuestDbContext dbContext,
        IAuditWriter auditWriter,
        IClock clock,
        IPasswordHasher passwordHasher,
        IQuestionRoutingResolver questionRoutingResolver)
    {
        _dbContext = dbContext;
        _auditWriter = auditWriter;
        _clock = clock;
        _passwordHasher = passwordHasher;
        _questionRoutingResolver = questionRoutingResolver;
    }

    public async Task<IReadOnlyList<TeamSummaryResponse>> GetTeamsAsync(CancellationToken cancellationToken = default)
    {
        List<Team> teams = await _dbContext.Teams
            .AsNoTracking()
            .Include(x => x.Memberships.Where(m => m.Status == TeamMembershipStatus.Active))
            .ThenInclude(x => x.ParticipantUser)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return teams.Select(t => TeamService.ToResponse(t, null)).ToList();
    }

    public async Task<TeamSupportDetailsResponse> GetTeamDetailsAsync(Guid teamId, CancellationToken cancellationToken = default)
    {
        Team team = await _dbContext.Teams
            .AsNoTracking()
            .Include(x => x.Memberships)
            .ThenInclude(x => x.ParticipantUser)
            .SingleAsync(x => x.Id == teamId, cancellationToken);

        List<TeamQuestionState> questionStates = await _dbContext.TeamQuestionStates
            .AsNoTracking()
            .Include(x => x.Question)
            .ThenInclude(x => x.Tag)
            .Where(x => x.TeamId == teamId)
            .ToListAsync(cancellationToken);

        HashSet<Guid> availableQuestionIds = await GetAvailableQuestionIdsAsync(cancellationToken);
        HashSet<Guid> questionIds = [.. availableQuestionIds, .. questionStates.Select(x => x.QuestionId)];

        List<Question> questions = await _dbContext.Questions
            .AsNoTracking()
            .Include(x => x.Tag)
            .Where(x => questionIds.Contains(x.Id))
            .OrderBy(x => x.Tag.Name)
            .ThenBy(x => x.Title)
            .ToListAsync(cancellationToken);

        Dictionary<Guid, TeamQuestionState> statesByQuestionId = questionStates.ToDictionary(x => x.QuestionId);

        return new TeamSupportDetailsResponse(
            TeamService.ToResponse(team, null),
            questions
                .Select(
                    x => new TeamSupportQuestionResponse(
                        x.Id,
                        x.TagId,
                        x.Tag.Name,
                        x.Tag.Color,
                        x.Title,
                        ResolveQuestionState(statesByQuestionId.GetValueOrDefault(x.Id)),
                        statesByQuestionId.GetValueOrDefault(x.Id)?.IsSolved ?? false,
                        availableQuestionIds.Contains(x.Id),
                        statesByQuestionId.GetValueOrDefault(x.Id)?.FirstUnlockedAt,
                        statesByQuestionId.GetValueOrDefault(x.Id)?.LastAttemptAt,
                        statesByQuestionId.GetValueOrDefault(x.Id)?.NextAllowedAnswerAt))
                .ToList(),
            await BuildTimelineAsync(team, teamId, cancellationToken));
    }

    public async Task UnlockQuestionAsync(Guid teamId, Guid questionId, TeamQuestionAdjustmentRequest request, CancellationToken cancellationToken = default)
    {
        TeamQuestionState? state = await _dbContext.TeamQuestionStates
            .SingleOrDefaultAsync(x => x.TeamId == teamId && x.QuestionId == questionId, cancellationToken);

        if (state is null)
        {
            state = new TeamQuestionState
            {
                TeamId = teamId,
                QuestionId = questionId,
                FirstUnlockedAt = _clock.UtcNow,
                CreatedAt = _clock.UtcNow,
                UpdatedAt = _clock.UtcNow,
            };

            await _dbContext.TeamQuestionStates.AddAsync(state, cancellationToken);
        }
        else
        {
            state.UpdatedAt = _clock.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditWriter.WriteAsync("SupportUnlockQuestion", nameof(TeamQuestionState), state.Id.ToString(), AppJson.Serialize(new { teamId, questionId }), request.Reason, cancellationToken);
    }

    public async Task MarkQuestionSolvedAsync(Guid teamId, Guid questionId, TeamQuestionAdjustmentRequest request, CancellationToken cancellationToken = default)
    {
        TeamQuestionState? state = await _dbContext.TeamQuestionStates
            .SingleOrDefaultAsync(x => x.TeamId == teamId && x.QuestionId == questionId, cancellationToken);

        if (state is null)
        {
            state = new TeamQuestionState
            {
                TeamId = teamId,
                QuestionId = questionId,
                FirstUnlockedAt = _clock.UtcNow,
                CreatedAt = _clock.UtcNow,
                UpdatedAt = _clock.UtcNow,
            };

            await _dbContext.TeamQuestionStates.AddAsync(state, cancellationToken);
        }

        state.IsSolved = true;
        state.SolvedAt = _clock.UtcNow;
        state.RewardGrantedAt ??= _clock.UtcNow;
        state.UpdatedAt = _clock.UtcNow;

        bool rewardExists = await _dbContext.TeamRotorRewards.AnyAsync(x => x.TeamId == teamId && x.SourceQuestionId == questionId, cancellationToken);
        if (!rewardExists)
        {
            Guid tagId = await _dbContext.Questions
                .Where(x => x.Id == questionId)
                .Select(x => x.TagId)
                .SingleAsync(cancellationToken);

            TeamRotorReward reward = new()
            {
                TeamId = teamId,
                TagId = tagId,
                SourceQuestionId = questionId,
                RewardType = "RotorHint",
                GrantedAt = _clock.UtcNow,
                PayloadJson = "{}",
                CreatedAt = _clock.UtcNow,
                UpdatedAt = _clock.UtcNow,
            };

            await _dbContext.TeamRotorRewards.AddAsync(reward, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditWriter.WriteAsync("SupportSolveQuestion", nameof(TeamQuestionState), state.Id.ToString(), AppJson.Serialize(new { teamId, questionId }), request.Reason, cancellationToken);
    }

    public async Task CloseQuestionAsync(Guid teamId, Guid questionId, TeamQuestionAdjustmentRequest request, CancellationToken cancellationToken = default)
    {
        TeamQuestionState? state = await _dbContext.TeamQuestionStates
            .SingleOrDefaultAsync(x => x.TeamId == teamId && x.QuestionId == questionId, cancellationToken);

        if (state is not null)
        {
            _dbContext.TeamQuestionStates.Remove(state);
        }

        await RevokeQuestionRewardsAsync(teamId, questionId, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditWriter.WriteAsync("SupportCloseQuestion", nameof(TeamQuestionState), $"{teamId}:{questionId}", AppJson.Serialize(new { teamId, questionId }), request.Reason, cancellationToken);
    }

    public async Task UnsolveQuestionAsync(Guid teamId, Guid questionId, TeamQuestionAdjustmentRequest request, CancellationToken cancellationToken = default)
    {
        TeamQuestionState state = await _dbContext.TeamQuestionStates
            .SingleOrDefaultAsync(x => x.TeamId == teamId && x.QuestionId == questionId, cancellationToken)
            ?? throw new AppException(404, "Вопрос не открыт для этой команды.");

        state.IsSolved = false;
        state.SolvedAt = null;
        state.SolvedByUserId = null;
        state.RewardGrantedAt = null;
        state.NextAllowedAnswerAt = null;
        state.UpdatedAt = _clock.UtcNow;

        await RevokeQuestionRewardsAsync(teamId, questionId, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditWriter.WriteAsync("SupportUnsolveQuestion", nameof(TeamQuestionState), state.Id.ToString(), AppJson.Serialize(new { teamId, questionId }), request.Reason, cancellationToken);
    }

    public async Task RevokeQuestionRewardAsync(Guid teamId, Guid questionId, TeamQuestionAdjustmentRequest request, CancellationToken cancellationToken = default)
    {
        await RevokeQuestionRewardsAsync(teamId, questionId, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditWriter.WriteAsync("SupportRevokeQuestionReward", nameof(TeamRotorReward), $"{teamId}:{questionId}", AppJson.Serialize(new { teamId, questionId }), request.Reason, cancellationToken);
    }

    public async Task AdjustRewardAsync(Guid teamId, TeamRewardAdjustmentRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Revoke)
        {
            List<TeamRotorReward> rewards = await _dbContext.TeamRotorRewards
                .Where(x => x.TeamId == teamId && x.TagId == request.TagId && !x.IsRevoked)
                .ToListAsync(cancellationToken);

            foreach (TeamRotorReward reward in rewards)
            {
                reward.IsRevoked = true;
                reward.UpdatedAt = _clock.UtcNow;
            }
        }
        else
        {
            TeamRotorReward reward = new()
            {
                TeamId = teamId,
                TagId = request.TagId,
                SourceQuestionId = request.SourceQuestionId,
                RewardType = request.RewardType,
                GrantedAt = _clock.UtcNow,
                CreatedAt = _clock.UtcNow,
                UpdatedAt = _clock.UtcNow,
            };

            await _dbContext.TeamRotorRewards.AddAsync(reward, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditWriter.WriteAsync("SupportAdjustReward", nameof(TeamRotorReward), teamId.ToString(), AppJson.Serialize(request), null, cancellationToken);
    }

    public async Task RemoveMemberAsync(Guid teamId, Guid membershipId, TeamMemberRemovalRequest request, CancellationToken cancellationToken = default)
    {
        TeamMembership membership = await _dbContext.TeamMemberships
            .SingleAsync(x => x.Id == membershipId && x.TeamId == teamId, cancellationToken);

        membership.Status = TeamMembershipStatus.Removed;
        membership.RemovedAt = _clock.UtcNow;
        membership.RemovalReason = request.Reason;
        membership.UpdatedAt = _clock.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditWriter.WriteAsync("SupportRemoveMember", nameof(TeamMembership), membership.Id.ToString(), AppJson.Serialize(new { teamId, membershipId }), request.Reason, cancellationToken);
    }

    public async Task ResetParticipantPasswordAsync(Guid participantId, ParticipantPasswordResetRequest request, CancellationToken cancellationToken = default)
    {
        if (request.NewPassword.Length < MinParticipantPasswordLength)
        {
            throw new AppException(400, $"Пароль не короче {MinParticipantPasswordLength} символов.");
        }

        ParticipantUser? participant = await _dbContext.ParticipantUsers
            .SingleOrDefaultAsync(x => x.Id == participantId, cancellationToken);

        if (participant is null)
        {
            throw new AppException(404, "Участник не найден.");
        }

        if (participant.Provider != ParticipantAuthProviders.Local)
        {
            throw new AppException(400, "Сброс пароля доступен только для локальной учётной записи.");
        }

        participant.PasswordHash = _passwordHasher.Hash(request.NewPassword);
        participant.UpdatedAt = _clock.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditWriter.WriteAsync(
            "SupportResetParticipantPassword",
            nameof(ParticipantUser),
            participantId.ToString(),
            "{}",
            request.Reason,
            cancellationToken);
    }

    private async Task<HashSet<Guid>> GetAvailableQuestionIdsAsync(CancellationToken cancellationToken)
    {
        List<Guid> qrIds = await _dbContext.QrCodes
            .AsNoTracking()
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        HashSet<Guid> questionIds = [];
        foreach (Guid qrId in qrIds)
        {
            QuestionRoutingResolution resolution = await _questionRoutingResolver.ResolveAsync(qrId, cancellationToken);
            if (resolution.Question is null)
            {
                continue;
            }

            if (resolution.Result is QrScanResolutionResult.Resolved or QrScanResolutionResult.OverrideResolved)
            {
                questionIds.Add(resolution.Question.Id);
            }
        }

        return questionIds;
    }

    private async Task<List<TeamSupportTimelineEntryResponse>> BuildTimelineAsync(
        Team team,
        Guid teamId,
        CancellationToken cancellationToken)
    {
        List<TeamSupportTimelineEntryResponse> items =
        [
            new(
                $"team-created:{team.Id}",
                team.CreatedAt,
                "team-created",
                "Команда создана",
                $"Команда «{team.Name}» создана.",
                null),
        ];

        items.AddRange(
            team.Memberships.Select(
                membership => new TeamSupportTimelineEntryResponse(
                    $"member-joined:{membership.Id}",
                    membership.JoinedAt,
                    "member-joined",
                    "Участник вступил в команду",
                    $"{membership.ParticipantUser.DisplayName} присоединился к команде.",
                    null)));

        items.AddRange(
            team.Memberships
                .Where(membership => membership.RemovedAt is not null)
                .Select(
                    membership => new TeamSupportTimelineEntryResponse(
                        $"member-removed:{membership.Id}",
                        membership.RemovedAt!.Value,
                        "member-removed",
                        "Участник исключён",
                        $"{membership.ParticipantUser.DisplayName} исключён из команды.",
                        membership.RemovalReason)));

        List<QrScanEvent> scanEvents = await _dbContext.QrScanEvents
            .AsNoTracking()
            .Include(x => x.QrCode)
            .Include(x => x.ResolvedQuestion)
            .Where(
                x => x.TeamId == teamId
                    && (x.ResolutionResult == QrScanResolutionResult.Resolved || x.ResolutionResult == QrScanResolutionResult.OverrideResolved))
            .OrderByDescending(x => x.OccurredAt)
            .Take(200)
            .ToListAsync(cancellationToken);

        items.AddRange(
            scanEvents.Select(
                scan => new TeamSupportTimelineEntryResponse(
                    $"qr-opened:{scan.Id}",
                    scan.OccurredAt,
                    "question-opened",
                    "Открыт вопрос",
                    scan.ResolvedQuestion is null
                        ? $"QR «{scan.QrCode.Label}» был успешно обработан."
                        : $"QR «{scan.QrCode.Label}» открыл вопрос «{scan.ResolvedQuestion.Title}».",
                    null)));

        List<TeamAnswerAttempt> answerAttempts = await _dbContext.TeamAnswerAttempts
            .AsNoTracking()
            .Include(x => x.Question)
            .ThenInclude(x => x.Tag)
            .Where(x => x.TeamId == teamId)
            .OrderByDescending(x => x.AttemptedAt)
            .Take(200)
            .ToListAsync(cancellationToken);

        items.AddRange(
            answerAttempts.Select(
                attempt => new TeamSupportTimelineEntryResponse(
                    $"answer-attempt:{attempt.Id}",
                    attempt.AttemptedAt,
                    attempt.Result == AnswerAttemptResult.Correct ? "question-solved" : "question-attempt",
                    DescribeAnswerAttemptTitle(attempt.Result),
                    $"Вопрос «{attempt.Question.Title}».",
                    null)));

        List<EnigmaAttempt> enigmaAttempts = await _dbContext.EnigmaAttempts
            .AsNoTracking()
            .Include(x => x.EnigmaProfile)
            .Where(x => x.TeamId == teamId)
            .OrderByDescending(x => x.AttemptedAt)
            .Take(100)
            .ToListAsync(cancellationToken);

        items.AddRange(
            enigmaAttempts.Select(
                attempt => new TeamSupportTimelineEntryResponse(
                    $"enigma-attempt:{attempt.Id}",
                    attempt.AttemptedAt,
                    attempt.Result == EnigmaAttemptResult.Success ? "enigma-solved" : "enigma-attempt",
                    DescribeEnigmaAttemptTitle(attempt.Result),
                    $"Профиль Enigma: «{attempt.EnigmaProfile.Name}».",
                    null)));

        if (team.FinalTaskPhotoUploadedAt is DateTimeOffset photoUploadedAt)
        {
            items.Add(
                new TeamSupportTimelineEntryResponse(
                    $"final-photo:{team.Id}",
                    photoUploadedAt,
                    "final-photo-uploaded",
                    "Загружено финальное фото",
                    "Команда выгрузила финальное фото после Enigma.",
                    null));
        }

        List<AuditEntryResponse> supportAudit = await _dbContext.AdminAuditLogs
            .AsNoTracking()
            .Where(x => x.ActionType.StartsWith("Support") && (x.EntityId == teamId.ToString() || x.DiffJson.Contains(teamId.ToString())))
            .OrderByDescending(x => x.OccurredAt)
            .Take(100)
            .Select(
                x => new AuditEntryResponse(
                    x.Id,
                    x.AdminUserId,
                    x.ActionType,
                    x.EntityType,
                    x.EntityId,
                    x.OccurredAt,
                    x.DiffJson,
                    x.Reason,
                    x.CorrelationId))
            .ToListAsync(cancellationToken);

        items.AddRange(
            supportAudit.Select(
                entry => new TeamSupportTimelineEntryResponse(
                    $"support-audit:{entry.Id}",
                    entry.OccurredAt,
                    "support-action",
                    DescribeSupportAuditTitle(entry.ActionType),
                    $"Админ-действие: {entry.ActionType}.",
                    entry.Reason)));

        return items
            .OrderByDescending(x => x.OccurredAt)
            .ToList();
    }

    private async Task RevokeQuestionRewardsAsync(Guid teamId, Guid questionId, CancellationToken cancellationToken)
    {
        List<TeamRotorReward> rewards = await _dbContext.TeamRotorRewards
            .Where(x => x.TeamId == teamId && x.SourceQuestionId == questionId && !x.IsRevoked)
            .ToListAsync(cancellationToken);

        foreach (TeamRotorReward reward in rewards)
        {
            reward.IsRevoked = true;
            reward.UpdatedAt = _clock.UtcNow;
        }
    }

    private static string ResolveQuestionState(TeamQuestionState? state)
    {
        if (state?.IsSolved == true)
        {
            return "solved";
        }

        return state is not null ? "open" : "closed";
    }

    private static string DescribeAnswerAttemptTitle(AnswerAttemptResult result)
    {
        return result switch
        {
            AnswerAttemptResult.Correct => "Верный ответ",
            AnswerAttemptResult.Wrong => "Неверный ответ",
            AnswerAttemptResult.CooldownBlocked => "Попытка ответа заблокирована кулдауном",
            AnswerAttemptResult.AlreadySolved => "Попытка после уже решённого вопроса",
            AnswerAttemptResult.NotUnlocked => "Попытка по закрытому вопросу",
            AnswerAttemptResult.DayClosed => "Попытка после завершения дня",
            AnswerAttemptResult.QuestNotStarted => "Попытка до старта квеста",
            _ => "Попытка ответа",
        };
    }

    private static string DescribeEnigmaAttemptTitle(EnigmaAttemptResult result)
    {
        return result switch
        {
            EnigmaAttemptResult.Success => "Успешная попытка Enigma",
            EnigmaAttemptResult.Failure => "Неуспешная попытка Enigma",
            EnigmaAttemptResult.CooldownBlocked => "Попытка Enigma заблокирована кулдауном",
            EnigmaAttemptResult.DayClosed => "Попытка Enigma после завершения дня",
            _ => "Попытка Enigma",
        };
    }

    private static string DescribeSupportAuditTitle(string actionType)
    {
        return actionType switch
        {
            "SupportUnlockQuestion" => "Администратор открыл вопрос",
            "SupportSolveQuestion" => "Администратор засчитал решение",
            "SupportCloseQuestion" => "Администратор закрыл вопрос",
            "SupportUnsolveQuestion" => "Администратор отозвал решение",
            "SupportRevokeQuestionReward" => "Администратор отозвал награду",
            "SupportAdjustReward" => "Администратор скорректировал награду",
            "SupportRemoveMember" => "Администратор исключил участника",
            _ => "Действие поддержки",
        };
    }
}

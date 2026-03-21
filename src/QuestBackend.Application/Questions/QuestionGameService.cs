using Microsoft.EntityFrameworkCore;
using QuestBackend.Application.Abstractions;
using QuestBackend.Application.Shared;
using QuestBackend.Application.Teams;
using QuestBackend.Contracts;
using QuestBackend.Domain.Config;
using QuestBackend.Domain.Enigma;
using QuestBackend.Domain.Progress;
using QuestBackend.Domain.Questions;
using QuestBackend.Domain.Shared;
using QuestBackend.Domain.Teams;

namespace QuestBackend.Application.Questions;

public sealed class QuestionGameService
{
    private readonly IAnswerEvaluator _answerEvaluator;
    private readonly IClock _clock;
    private readonly ICurrentPrincipal _currentPrincipal;
    private readonly IQuestionRoutingResolver _questionRoutingResolver;
    private readonly IQuestDayLifecycleGate _questDayLifecycleGate;
    private readonly IQuestDbContext _dbContext;
    private readonly TeamService _teamService;

    public QuestionGameService(
        IQuestDbContext dbContext,
        IQuestionRoutingResolver questionRoutingResolver,
        IQuestDayLifecycleGate questDayLifecycleGate,
        IAnswerEvaluator answerEvaluator,
        ICurrentPrincipal currentPrincipal,
        TeamService teamService,
        IClock clock)
    {
        _dbContext = dbContext;
        _questionRoutingResolver = questionRoutingResolver;
        _questDayLifecycleGate = questDayLifecycleGate;
        _answerEvaluator = answerEvaluator;
        _currentPrincipal = currentPrincipal;
        _teamService = teamService;
        _clock = clock;
    }

    public async Task<QrResolutionResponse> ResolveQrScanAsync(string slug, CancellationToken cancellationToken = default)
    {
        var qrCodeProjection = await _dbContext.QrCodes
            .AsNoTracking()
            .Where(x => x.Slug == slug)
            .Select(x => new { x.Id, x.Label })
            .FirstOrDefaultAsync(cancellationToken);

        if (qrCodeProjection is null)
        {
            return new QrResolutionResponse("not_found", "QR code not found.", null, null, null, _clock.UtcNow);
        }

        QuestLifecycleDecision lifecycleDecision = await _questDayLifecycleGate.GetDecisionAsync(cancellationToken);
        if (!lifecycleDecision.AllowsUnlock)
        {
            QrScanResolutionResult lifecycleResult = lifecycleDecision.Status == Domain.QuestDay.QuestDayStatus.DayClosed
                ? QrScanResolutionResult.DayClosed
                : QrScanResolutionResult.NotStarted;

            await WriteScanEventAsync(qrCodeProjection.Id, null, null, lifecycleResult, cancellationToken);
            return new QrResolutionResponse(
                lifecycleDecision.Status == Domain.QuestDay.QuestDayStatus.DayClosed ? "day_closed" : "not_started",
                lifecycleDecision.Message,
                qrCodeProjection.Id,
                null,
                null,
                _clock.UtcNow);
        }

        if (!_currentPrincipal.IsParticipantAuthenticated || _currentPrincipal.ParticipantUserId is null)
        {
            await WriteScanEventAsync(qrCodeProjection.Id, null, null, QrScanResolutionResult.RequiresAuthentication, cancellationToken);
            return new QrResolutionResponse("requires_auth", "Participant authentication is required.", qrCodeProjection.Id, null, null, _clock.UtcNow);
        }

        Team? team = await _teamService.GetCurrentParticipantTeamEntityAsync(cancellationToken);
        if (team is null)
        {
            await WriteScanEventAsync(qrCodeProjection.Id, null, null, QrScanResolutionResult.RequiresTeam, cancellationToken);
            return new QrResolutionResponse("requires_team", "Participant must join or create a team first.", qrCodeProjection.Id, null, null, _clock.UtcNow);
        }

        QuestionRoutingResolution resolution = await _questionRoutingResolver.ResolveAsync(qrCodeProjection.Id, cancellationToken);
        if (resolution.Question is null || resolution.QrCode is null)
        {
            await WriteScanEventAsync(qrCodeProjection.Id, team.Id, null, resolution.Result, cancellationToken);
            return new QrResolutionResponse("unavailable", resolution.Message, qrCodeProjection.Id, null, null, _clock.UtcNow);
        }

        TeamQuestionState? questionState = await _dbContext.TeamQuestionStates
            .Include(x => x.Question)
            .ThenInclude(x => x.Tag)
            .SingleOrDefaultAsync(x => x.TeamId == team.Id && x.QuestionId == resolution.Question.Id, cancellationToken);

        if (questionState is null)
        {
            questionState = new TeamQuestionState
            {
                TeamId = team.Id,
                QuestionId = resolution.Question.Id,
                FirstUnlockedAt = _clock.UtcNow,
                UnlockedByQrCodeId = resolution.QrCode.Id,
                UnlockedByUserId = _currentPrincipal.ParticipantUserId,
                CreatedAt = _clock.UtcNow,
                UpdatedAt = _clock.UtcNow,
            };

            await _dbContext.TeamQuestionStates.AddAsync(questionState, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            questionState = await _dbContext.TeamQuestionStates
                .AsNoTracking()
                .Include(x => x.Question)
                .ThenInclude(x => x.Tag)
                .SingleAsync(x => x.Id == questionState.Id, cancellationToken);
        }

        await WriteScanEventAsync(qrCodeProjection.Id, team.Id, resolution.Question.Id, resolution.Result, cancellationToken);

        QuestionDetailsResponse question = ToQuestionDetailsResponse(questionState);
        return new QrResolutionResponse("resolved", resolution.Message, qrCodeProjection.Id, resolution.Question.Id, question, _clock.UtcNow);
    }

    public async Task<IReadOnlyList<QuestionSummaryResponse>> GetKnownQuestionsAsync(CancellationToken cancellationToken = default)
    {
        Team team = await EnsureCurrentTeamAsync(cancellationToken);

        List<TeamQuestionState> states = await _dbContext.TeamQuestionStates
            .AsNoTracking()
            .Include(x => x.Question)
            .ThenInclude(x => x.Tag)
            .Where(x => x.TeamId == team.Id)
            .OrderByDescending(x => x.FirstUnlockedAt)
            .ToListAsync(cancellationToken);

        return states.Select(ToQuestionSummaryResponse).ToList();
    }

    public async Task<QuestionDetailsResponse> GetQuestionDetailsAsync(Guid questionId, CancellationToken cancellationToken = default)
    {
        Team team = await EnsureCurrentTeamAsync(cancellationToken);

        TeamQuestionState state = await _dbContext.TeamQuestionStates
            .AsNoTracking()
            .Include(x => x.Question)
            .ThenInclude(x => x.Tag)
            .SingleOrDefaultAsync(x => x.TeamId == team.Id && x.QuestionId == questionId, cancellationToken)
            ?? throw new AppException(404, "Вопрос не открыт для этой команды.");

        return ToQuestionDetailsResponse(state);
    }

    public async Task<SubmitAnswerResponse> SubmitAnswerAsync(Guid questionId, SubmitAnswerRequest request, CancellationToken cancellationToken = default)
    {
        Team team = await EnsureCurrentTeamAsync(cancellationToken);
        QuestLifecycleDecision lifecycleDecision = await _questDayLifecycleGate.GetDecisionAsync(cancellationToken);
        if (!lifecycleDecision.AllowsSubmissions)
        {
            return new SubmitAnswerResponse(
                lifecycleDecision.Status == Domain.QuestDay.QuestDayStatus.DayClosed ? "day_closed" : "not_started",
                false,
                false,
                null,
                lifecycleDecision.Message,
                _clock.UtcNow);
        }

        TeamQuestionState state = await _dbContext.TeamQuestionStates
            .Include(x => x.Question)
            .ThenInclude(x => x.Tag)
            .SingleOrDefaultAsync(x => x.TeamId == team.Id && x.QuestionId == questionId, cancellationToken)
            ?? throw new AppException(404, "Вопрос не открыт для этой команды.");

        if (state.IsSolved)
        {
            return new SubmitAnswerResponse("already_solved", true, false, state.NextAllowedAnswerAt, "Question is already solved.", _clock.UtcNow);
        }

        if (state.NextAllowedAnswerAt is DateTimeOffset nextAllowedAt && nextAllowedAt > _clock.UtcNow)
        {
            TeamAnswerAttempt blockedAttempt = new()
            {
                TeamId = team.Id,
                QuestionId = state.QuestionId,
                SubmittedByUserId = _currentPrincipal.ParticipantUserId,
                RawAnswer = request.Answer,
                NormalizedAnswer = request.Answer,
                Result = AnswerAttemptResult.CooldownBlocked,
                AttemptedAt = _clock.UtcNow,
                CooldownAppliedUntil = nextAllowedAt,
                EvaluationSnapshotJson = "{}",
                CreatedAt = _clock.UtcNow,
                UpdatedAt = _clock.UtcNow,
            };

            await _dbContext.TeamAnswerAttempts.AddAsync(blockedAttempt, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return new SubmitAnswerResponse("cooldown", false, false, nextAllowedAt, "Cooldown is still active for this question.", _clock.UtcNow);
        }

        GlobalSettings settings = await GetGlobalSettingsAsync(cancellationToken);
        AnswerEvaluationResult evaluation = _answerEvaluator.Evaluate(state.Question, request.Answer);
        TeamAnswerAttempt attempt = new()
        {
            TeamId = team.Id,
            QuestionId = state.QuestionId,
            SubmittedByUserId = _currentPrincipal.ParticipantUserId,
            RawAnswer = request.Answer,
            NormalizedAnswer = evaluation.NormalizedAnswer,
            Result = evaluation.IsCorrect ? AnswerAttemptResult.Correct : AnswerAttemptResult.Wrong,
            AttemptedAt = _clock.UtcNow,
            EvaluationSnapshotJson = evaluation.EvaluationSnapshotJson,
            CreatedAt = _clock.UtcNow,
            UpdatedAt = _clock.UtcNow,
        };

        bool rewardGranted = false;
        if (evaluation.IsCorrect)
        {
            state.IsSolved = true;
            state.SolvedAt = _clock.UtcNow;
            state.SolvedByUserId = _currentPrincipal.ParticipantUserId;
            state.RewardGrantedAt = _clock.UtcNow;
            state.LastAttemptAt = _clock.UtcNow;
            state.NextAllowedAnswerAt = null;
            state.UpdatedAt = _clock.UtcNow;

            bool rewardExists = await _dbContext.TeamRotorRewards.AnyAsync(
                x => x.TeamId == team.Id && x.SourceQuestionId == state.QuestionId,
                cancellationToken);

            if (!rewardExists)
            {
                TeamRotorReward reward = new()
                {
                    TeamId = team.Id,
                    TagId = state.Question.TagId,
                    SourceQuestionId = state.QuestionId,
                    RewardType = "RotorHint",
                    GrantedAt = _clock.UtcNow,
                    PayloadJson = AppJson.Serialize(new { state.Question.FooterHint, state.Question.TagId }),
                    CreatedAt = _clock.UtcNow,
                    UpdatedAt = _clock.UtcNow,
                };

                await _dbContext.TeamRotorRewards.AddAsync(reward, cancellationToken);
                rewardGranted = true;
            }
        }
        else
        {
            DateTimeOffset nextAttemptAt = _clock.UtcNow.AddMinutes(settings.AnswerCooldownMinutes);
            state.LastAttemptAt = _clock.UtcNow;
            state.NextAllowedAnswerAt = nextAttemptAt;
            state.UpdatedAt = _clock.UtcNow;
            attempt.CooldownAppliedUntil = nextAttemptAt;
        }

        await _dbContext.TeamAnswerAttempts.AddAsync(attempt, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new SubmitAnswerResponse(
            evaluation.IsCorrect ? "correct" : "wrong",
            state.IsSolved,
            rewardGranted,
            state.NextAllowedAnswerAt,
            evaluation.IsCorrect ? "Correct answer." : "Wrong answer.",
            _clock.UtcNow);
    }

    private async Task WriteScanEventAsync(
        Guid qrCodeId,
        Guid? teamId,
        Guid? questionId,
        QrScanResolutionResult result,
        CancellationToken cancellationToken)
    {
        QrScanEvent scanEvent = new()
        {
            QrCodeId = qrCodeId,
            ResolvedQuestionId = questionId,
            TeamId = teamId,
            ParticipantUserId = _currentPrincipal.ParticipantUserId,
            OccurredAt = _clock.UtcNow,
            ResolutionResult = result,
            ResolutionMetaJson = AppJson.Serialize(new { slugResolved = true }),
            CreatedAt = _clock.UtcNow,
            UpdatedAt = _clock.UtcNow,
        };

        await _dbContext.QrScanEvents.AddAsync(scanEvent, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<Team> EnsureCurrentTeamAsync(CancellationToken cancellationToken)
    {
        Team? team = await _teamService.GetCurrentParticipantTeamEntityAsync(cancellationToken);
        return team ?? throw new AppException(409, "Нужно состоять в активной команде.");
    }

    private async Task<GlobalSettings> GetGlobalSettingsAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.GlobalSettings.FirstOrDefaultAsync(cancellationToken)
            ?? new GlobalSettings();
    }

    private static QuestionSummaryResponse ToQuestionSummaryResponse(TeamQuestionState state) =>
        new(
            state.QuestionId,
            state.Question.TagId,
            state.Question.Tag.Name,
            state.Question.Tag.Color,
            state.Question.Title,
            state.IsSolved,
            state.NextAllowedAnswerAt,
            state.LastAttemptAt,
            state.FirstUnlockedAt);

    private static QuestionDetailsResponse ToQuestionDetailsResponse(TeamQuestionState state) =>
        new(
            state.QuestionId,
            state.Question.TagId,
            state.Question.Tag.Name,
            state.Question.Tag.Color,
            state.Question.Title,
            state.Question.BodyRichText,
            state.Question.FooterHint,
            state.Question.ImageUrl,
            state.IsSolved,
            state.NextAllowedAnswerAt,
            state.FirstUnlockedAt,
            state.SolvedAt);
}

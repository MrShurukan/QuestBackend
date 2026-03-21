using Microsoft.EntityFrameworkCore;
using QuestBackend.Application.Abstractions;
using QuestBackend.Application.Shared;
using QuestBackend.Application.Teams;
using QuestBackend.Contracts;
using QuestBackend.Domain.Enigma;
using QuestBackend.Domain.Participants;
using QuestBackend.Domain.Progress;
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

    public SupportService(IQuestDbContext dbContext, IAuditWriter auditWriter, IClock clock, IPasswordHasher passwordHasher)
    {
        _dbContext = dbContext;
        _auditWriter = auditWriter;
        _clock = clock;
        _passwordHasher = passwordHasher;
    }

    public async Task<IReadOnlyList<TeamSummaryResponse>> GetTeamsAsync(CancellationToken cancellationToken = default)
    {
        List<Team> teams = await _dbContext.Teams
            .AsNoTracking()
            .Include(x => x.Memberships.Where(m => m.Status == TeamMembershipStatus.Active))
            .ThenInclude(x => x.ParticipantUser)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return teams.Select(TeamService.ToResponse).ToList();
    }

    public async Task<TeamSupportDetailsResponse> GetTeamDetailsAsync(Guid teamId, CancellationToken cancellationToken = default)
    {
        Team team = await _dbContext.Teams
            .AsNoTracking()
            .Include(x => x.Memberships.Where(m => m.Status == TeamMembershipStatus.Active))
            .ThenInclude(x => x.ParticipantUser)
            .SingleAsync(x => x.Id == teamId, cancellationToken);

        List<TeamQuestionState> questions = await _dbContext.TeamQuestionStates
            .AsNoTracking()
            .Include(x => x.Question)
            .ThenInclude(x => x.Tag)
            .Where(x => x.TeamId == teamId)
            .OrderByDescending(x => x.FirstUnlockedAt)
            .ToListAsync(cancellationToken);

        List<AuditEntryResponse> audit = await _dbContext.AdminAuditLogs
            .AsNoTracking()
            .Where(x => x.EntityId == teamId.ToString() || x.DiffJson.Contains(teamId.ToString()))
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

        return new TeamSupportDetailsResponse(
            TeamService.ToResponse(team),
            questions
                .Select(
                    x => new QuestionSummaryResponse(
                        x.QuestionId,
                        x.Question.TagId,
                        x.Question.Tag.Name,
                        x.Question.Tag.Color,
                        x.Question.Title,
                        x.IsSolved,
                        x.NextAllowedAnswerAt,
                        x.LastAttemptAt,
                        x.FirstUnlockedAt))
                .ToList(),
            audit);
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

    public async Task RevokeQuestionRewardAsync(Guid teamId, Guid questionId, TeamQuestionAdjustmentRequest request, CancellationToken cancellationToken = default)
    {
        List<TeamRotorReward> rewards = await _dbContext.TeamRotorRewards
            .Where(x => x.TeamId == teamId && x.SourceQuestionId == questionId && !x.IsRevoked)
            .ToListAsync(cancellationToken);

        foreach (TeamRotorReward reward in rewards)
        {
            reward.IsRevoked = true;
            reward.UpdatedAt = _clock.UtcNow;
        }

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
}

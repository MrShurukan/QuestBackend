using Microsoft.EntityFrameworkCore;
using QuestBackend.Application.Abstractions;
using QuestBackend.Application.Shared;
using QuestBackend.Contracts;
using QuestBackend.Domain.Shared;
using QuestBackend.Domain.Teams;

namespace QuestBackend.Application.Teams;

public sealed class TeamService
{
    private readonly IClock _clock;
    private readonly ICurrentPrincipal _currentPrincipal;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IQuestDbContext _dbContext;
    private readonly IQuestDayLifecycleGate _questDayLifecycleGate;

    public TeamService(
        IQuestDbContext dbContext,
        IPasswordHasher passwordHasher,
        ICurrentPrincipal currentPrincipal,
        IClock clock,
        IQuestDayLifecycleGate questDayLifecycleGate)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _currentPrincipal = currentPrincipal;
        _clock = clock;
        _questDayLifecycleGate = questDayLifecycleGate;
    }

    public async Task<IReadOnlyList<TeamSummaryResponse>> GetAvailableTeamsAsync(CancellationToken cancellationToken = default)
    {
        int maxMembers = await ResolveMaxTeamMembersAsync(cancellationToken);

        List<Team> teams = await _dbContext.Teams
            .AsNoTracking()
            .Include(x => x.Memberships.Where(m => m.Status == TeamMembershipStatus.Active))
            .ThenInclude(x => x.ParticipantUser)
            .Where(x => x.Status == TeamStatus.Active && !x.IsHidden)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return teams
            .Where(t => t.Memberships.Count(m => m.Status == TeamMembershipStatus.Active) < maxMembers)
            .Select(ToResponse)
            .ToList();
    }

    public async Task<TeamSummaryResponse> CreateTeamAsync(CreateTeamRequest request, CancellationToken cancellationToken = default)
    {
        Guid participantId = EnsureParticipant();

        bool exists = await _dbContext.Teams.AnyAsync(x => x.Name == request.Name, cancellationToken);
        if (exists)
        {
            throw new AppException(409, "Команда с таким названием уже существует.");
        }

        await EnsureParticipantHasNoActiveTeamAsync(participantId, cancellationToken);

        Team team = new()
        {
            Name = request.Name.Trim(),
            JoinSecretHash = _passwordHasher.Hash(request.JoinSecret),
            CreatedByUserId = participantId,
            CreatedAt = _clock.UtcNow,
            UpdatedAt = _clock.UtcNow,
        };

        TeamMembership membership = new()
        {
            Team = team,
            ParticipantUserId = participantId,
            JoinedAt = _clock.UtcNow,
            CreatedAt = _clock.UtcNow,
            UpdatedAt = _clock.UtcNow,
        };

        await _dbContext.Teams.AddAsync(team, cancellationToken);
        await _dbContext.TeamMemberships.AddAsync(membership, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        Team loaded = await LoadTeamAsync(team.Id, cancellationToken);
        return ToResponse(loaded);
    }

    public async Task<TeamSummaryResponse> JoinTeamAsync(JoinTeamRequest request, CancellationToken cancellationToken = default)
    {
        Guid participantId = EnsureParticipant();
        await EnsureParticipantHasNoActiveTeamAsync(participantId, cancellationToken);

        Team team = await _dbContext.Teams
            .SingleOrDefaultAsync(x => x.Id == request.TeamId, cancellationToken)
            ?? throw new AppException(404, "Команда не найдена.");

        if (team.IsLocked || team.Status != TeamStatus.Active)
        {
            throw new AppException(409, "Команда недоступна для вступления.");
        }

        if (!_passwordHasher.Verify(request.JoinSecret, team.JoinSecretHash))
        {
            throw new AppException(401, "Неверный секрет команды.");
        }

        int maxMembers = await ResolveMaxTeamMembersAsync(cancellationToken);
        int activeCount = await _dbContext.TeamMemberships.CountAsync(
            x => x.TeamId == team.Id && x.Status == TeamMembershipStatus.Active,
            cancellationToken);

        if (activeCount >= maxMembers)
        {
            throw new AppException(409, $"В команде уже максимальное число участников ({maxMembers}).");
        }

        TeamMembership membership = new()
        {
            TeamId = team.Id,
            ParticipantUserId = participantId,
            JoinedAt = _clock.UtcNow,
            CreatedAt = _clock.UtcNow,
            UpdatedAt = _clock.UtcNow,
        };

        await _dbContext.TeamMemberships.AddAsync(membership, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        Team loaded = await LoadTeamAsync(team.Id, cancellationToken);
        return ToResponse(loaded);
    }

    public async Task<TeamSummaryResponse?> GetMyTeamAsync(CancellationToken cancellationToken = default)
    {
        if (_currentPrincipal.ParticipantUserId is null)
        {
            return null;
        }

        TeamMembership? membership = await _dbContext.TeamMemberships
            .AsNoTracking()
            .Where(x => x.ParticipantUserId == _currentPrincipal.ParticipantUserId.Value && x.Status == TeamMembershipStatus.Active)
            .FirstOrDefaultAsync(cancellationToken);

        if (membership is null)
        {
            return null;
        }

        Team team = await LoadTeamAsync(membership.TeamId, cancellationToken);
        return ToResponse(team);
    }

    /// <summary>Validates captain, enigma solved, no photo yet, lifecycle — call before writing file to disk.</summary>
    public async Task EnsureFinalTaskPhotoUploadAllowedAsync(CancellationToken cancellationToken = default)
    {
        _ = await LoadAndValidateTeamForFinalPhotoAsync(cancellationToken);
    }

    /// <summary>Captain-only, after enigma solved, one photo per team. Caller saves file to disk first, then passes relative URL.</summary>
    public async Task<TeamSummaryResponse> RecordFinalTaskPhotoAsync(string relativeUrl, CancellationToken cancellationToken = default)
    {
        Team team = await LoadAndValidateTeamForFinalPhotoAsync(cancellationToken);

        team.FinalTaskPhotoUrl = relativeUrl;
        team.FinalTaskPhotoUploadedAt = _clock.UtcNow;
        team.UpdatedAt = _clock.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        Team reloaded = await LoadTeamAsync(team.Id, cancellationToken);
        return ToResponse(reloaded);
    }

    private async Task<Team> LoadAndValidateTeamForFinalPhotoAsync(CancellationToken cancellationToken)
    {
        Guid participantId = EnsureParticipant();

        QuestLifecycleDecision lifecycle = await _questDayLifecycleGate.GetDecisionAsync(cancellationToken);
        if (!lifecycle.AllowsSubmissions)
        {
            throw new AppException(409, lifecycle.Message);
        }

        TeamMembership? membership = await _dbContext.TeamMemberships
            .FirstOrDefaultAsync(
                x => x.ParticipantUserId == participantId && x.Status == TeamMembershipStatus.Active,
                cancellationToken);

        if (membership is null)
        {
            throw new AppException(404, "Нет активной команды.");
        }

        Team team = await _dbContext.Teams
            .Include(x => x.Memberships.Where(m => m.Status == TeamMembershipStatus.Active))
            .ThenInclude(x => x.ParticipantUser)
            .SingleAsync(x => x.Id == membership.TeamId, cancellationToken);

        if (team.CreatedByUserId != participantId)
        {
            throw new AppException(403, "Загрузить фото может только капитан команды.");
        }

        if (team.EnigmaSolvedAt is null)
        {
            throw new AppException(409, "Фото доступно после успешной расшифровки Enigma.");
        }

        if (!string.IsNullOrEmpty(team.FinalTaskPhotoUrl))
        {
            throw new AppException(409, "Фотография уже выгружена. Разрешена только одна.");
        }

        return team;
    }

    internal async Task<Team?> GetCurrentParticipantTeamEntityAsync(CancellationToken cancellationToken = default)
    {
        if (_currentPrincipal.ParticipantUserId is null)
        {
            return null;
        }

        TeamMembership? membership = await _dbContext.TeamMemberships
            .AsNoTracking()
            .Where(x => x.ParticipantUserId == _currentPrincipal.ParticipantUserId.Value && x.Status == TeamMembershipStatus.Active)
            .FirstOrDefaultAsync(cancellationToken);

        if (membership is null)
        {
            return null;
        }

        return await _dbContext.Teams
            .SingleOrDefaultAsync(x => x.Id == membership.TeamId, cancellationToken);
    }

    public static TeamSummaryResponse ToResponse(Team team) =>
        new(
            team.Id,
            team.Name,
            team.Status.ToString(),
            team.IsLocked,
            team.IsHidden,
            team.IsDisqualified,
            team.EnigmaSolvedAt is not null,
            team.EnigmaSolvedAt,
            team.CreatedByUserId,
            team.FinalTaskPhotoUrl,
            team.FinalTaskPhotoUploadedAt,
            team.Memberships
                .Where(x => x.Status == TeamMembershipStatus.Active)
                .OrderBy(x => x.JoinedAt)
                .Select(
                    x => new TeamMemberResponse(
                        x.Id,
                        x.ParticipantUserId,
                        x.ParticipantUser.DisplayName,
                        x.Status.ToString(),
                        x.JoinedAt,
                        x.ParticipantUser.AvatarUrl,
                        x.ParticipantUser.Provider))
                .ToList());

    private Guid EnsureParticipant()
    {
        if (!_currentPrincipal.IsParticipantAuthenticated || _currentPrincipal.ParticipantUserId is null)
        {
            throw new AppException(401, "Требуется вход участника.");
        }

        return _currentPrincipal.ParticipantUserId.Value;
    }

    private async Task<int> ResolveMaxTeamMembersAsync(CancellationToken cancellationToken)
    {
        int max = await _dbContext.GlobalSettings
            .AsNoTracking()
            .Select(x => x.MaxTeamMembers)
            .FirstOrDefaultAsync(cancellationToken);

        if (max < 1)
        {
            return 4;
        }

        return max;
    }

    private async Task EnsureParticipantHasNoActiveTeamAsync(Guid participantId, CancellationToken cancellationToken)
    {
        bool hasActiveTeam = await _dbContext.TeamMemberships
            .AnyAsync(x => x.ParticipantUserId == participantId && x.Status == TeamMembershipStatus.Active, cancellationToken);

        if (hasActiveTeam)
        {
            throw new AppException(409, "Участник уже состоит в активной команде.");
        }
    }

    private async Task<Team> LoadTeamAsync(Guid teamId, CancellationToken cancellationToken)
    {
        return await _dbContext.Teams
            .AsNoTracking()
            .Include(x => x.Memberships.Where(m => m.Status == TeamMembershipStatus.Active))
            .ThenInclude(x => x.ParticipantUser)
            .SingleAsync(x => x.Id == teamId, cancellationToken);
    }
}

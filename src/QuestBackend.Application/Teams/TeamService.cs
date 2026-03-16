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

    public TeamService(IQuestDbContext dbContext, IPasswordHasher passwordHasher, ICurrentPrincipal currentPrincipal, IClock clock)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _currentPrincipal = currentPrincipal;
        _clock = clock;
    }

    public async Task<IReadOnlyList<TeamSummaryResponse>> GetAvailableTeamsAsync(CancellationToken cancellationToken = default)
    {
        List<Team> teams = await _dbContext.Teams
            .AsNoTracking()
            .Include(x => x.Memberships.Where(m => m.Status == TeamMembershipStatus.Active))
            .ThenInclude(x => x.ParticipantUser)
            .Where(x => x.Status == TeamStatus.Active && !x.IsHidden)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return teams.Select(ToResponse).ToList();
    }

    public async Task<TeamSummaryResponse> CreateTeamAsync(CreateTeamRequest request, CancellationToken cancellationToken = default)
    {
        Guid participantId = EnsureParticipant();

        bool exists = await _dbContext.Teams.AnyAsync(x => x.Name == request.Name, cancellationToken);
        if (exists)
        {
            throw new AppException(409, "Team with this name already exists.");
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
            ?? throw new AppException(404, "Team not found.");

        if (team.IsLocked || team.Status != TeamStatus.Active)
        {
            throw new AppException(409, "Team is not available for joining.");
        }

        if (!_passwordHasher.Verify(request.JoinSecret, team.JoinSecretHash))
        {
            throw new AppException(401, "Invalid team join secret.");
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
            team.Memberships
                .Where(x => x.Status == TeamMembershipStatus.Active)
                .OrderBy(x => x.JoinedAt)
                .Select(
                    x => new TeamMemberResponse(
                        x.Id,
                        x.ParticipantUserId,
                        x.ParticipantUser.DisplayName,
                        x.Status.ToString(),
                        x.JoinedAt))
                .ToList());

    private Guid EnsureParticipant()
    {
        if (!_currentPrincipal.IsParticipantAuthenticated || _currentPrincipal.ParticipantUserId is null)
        {
            throw new AppException(401, "Participant authentication is required.");
        }

        return _currentPrincipal.ParticipantUserId.Value;
    }

    private async Task EnsureParticipantHasNoActiveTeamAsync(Guid participantId, CancellationToken cancellationToken)
    {
        bool hasActiveTeam = await _dbContext.TeamMemberships
            .AnyAsync(x => x.ParticipantUserId == participantId && x.Status == TeamMembershipStatus.Active, cancellationToken);

        if (hasActiveTeam)
        {
            throw new AppException(409, "Participant already belongs to an active team.");
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

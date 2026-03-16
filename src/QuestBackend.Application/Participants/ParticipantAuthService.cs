using Microsoft.EntityFrameworkCore;
using QuestBackend.Application.Abstractions;
using QuestBackend.Contracts;
using QuestBackend.Domain.Participants;
using QuestBackend.Domain.Shared;

namespace QuestBackend.Application.Participants;

public sealed class ParticipantAuthService
{
    private readonly IClock _clock;
    private readonly ICurrentPrincipal _currentPrincipal;
    private readonly IQuestDbContext _dbContext;

    public ParticipantAuthService(IQuestDbContext dbContext, ICurrentPrincipal currentPrincipal, IClock clock)
    {
        _dbContext = dbContext;
        _currentPrincipal = currentPrincipal;
        _clock = clock;
    }

    public async Task<ParticipantUser> DevLoginAsync(DevParticipantLoginRequest request, CancellationToken cancellationToken = default)
    {
        ParticipantUser? participant = await _dbContext.ParticipantUsers
            .SingleOrDefaultAsync(
                x => x.Provider == "dev" && x.ProviderSubject == request.ProviderSubject,
                cancellationToken);

        if (participant is null)
        {
            participant = new ParticipantUser
            {
                Provider = "dev",
                ProviderSubject = request.ProviderSubject,
                DisplayName = request.DisplayName,
                AvatarUrl = request.AvatarUrl,
                LastSeenAt = _clock.UtcNow,
            };

            await _dbContext.ParticipantUsers.AddAsync(participant, cancellationToken);
        }
        else
        {
            participant.DisplayName = request.DisplayName;
            participant.AvatarUrl = request.AvatarUrl;
            participant.LastSeenAt = _clock.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return participant;
    }

    public async Task<ParticipantProfileResponse?> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        if (!_currentPrincipal.IsParticipantAuthenticated || _currentPrincipal.ParticipantUserId is null)
        {
            return null;
        }

        ParticipantUser? participant = await _dbContext.ParticipantUsers
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == _currentPrincipal.ParticipantUserId.Value, cancellationToken);

        return participant is null ? null : ToResponse(participant);
    }

    public static ParticipantProfileResponse ToResponse(ParticipantUser participant) =>
        new(
            participant.Id,
            participant.Provider,
            participant.ProviderSubject,
            participant.DisplayName,
            participant.AvatarUrl,
            participant.IsBlocked);
}

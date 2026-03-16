using Microsoft.EntityFrameworkCore;
using QuestBackend.Application.Abstractions;
using QuestBackend.Application.Shared;
using QuestBackend.Domain.Audit;
using QuestBackend.Domain.Shared;

namespace QuestBackend.Infrastructure.Audit;

public sealed class ConfigSnapshotService : IConfigSnapshotService
{
    private readonly IClock _clock;
    private readonly ICurrentPrincipal _currentPrincipal;
    private readonly IQuestDbContext _dbContext;

    public ConfigSnapshotService(IQuestDbContext dbContext, ICurrentPrincipal currentPrincipal, IClock clock)
    {
        _dbContext = dbContext;
        _currentPrincipal = currentPrincipal;
        _clock = clock;
    }

    public async Task<ConfigSnapshot> CreateSnapshotAsync(string snapshotType, string? comment, CancellationToken cancellationToken = default)
    {
        var snapshotPayload = new
        {
            snapshotType,
            createdAt = _clock.UtcNow,
            tags = await _dbContext.QuestionTags.AsNoTracking().Select(x => new { x.Id, x.Code, x.Name, x.Color, x.IsActive }).ToListAsync(cancellationToken),
            questions = await _dbContext.Questions.AsNoTracking().Select(x => new { x.Id, x.TagId, x.Title, x.Status, x.IsActive, x.IsArchived }).ToListAsync(cancellationToken),
            pools = await _dbContext.QuestionPools.AsNoTracking().Select(x => new { x.Id, x.TagId, x.Name, x.IsActive }).ToListAsync(cancellationToken),
            qrCodes = await _dbContext.QrCodes.AsNoTracking().Select(x => new { x.Id, x.TagId, x.Slug, x.Label, x.IsActive, x.SlotIndex }).ToListAsync(cancellationToken),
            routingProfiles = await _dbContext.RoutingProfiles.AsNoTracking().Select(x => new { x.Id, x.Name, x.IsActive }).ToListAsync(cancellationToken),
            questDayStates = await _dbContext.QuestDayStates.AsNoTracking().Select(x => new { x.Id, x.DayCode, x.Status }).ToListAsync(cancellationToken),
            enigmaProfiles = await _dbContext.EnigmaProfiles.AsNoTracking().Select(x => new { x.Id, x.Name, x.IsActive, x.Mode }).ToListAsync(cancellationToken),
            settings = await _dbContext.GlobalSettings.AsNoTracking().Select(x => new { x.Id, x.CurrentQuestDayStateId, x.CurrentRoutingProfileId, x.CurrentEnigmaProfileId }).ToListAsync(cancellationToken),
        };

        ConfigSnapshot snapshot = new()
        {
            SnapshotType = snapshotType,
            Comment = comment,
            CreatedByAdminUserId = _currentPrincipal.AdminUserId,
            PayloadJson = AppJson.Serialize(snapshotPayload),
            CreatedAt = _clock.UtcNow,
            UpdatedAt = _clock.UtcNow,
        };

        await _dbContext.ConfigSnapshots.AddAsync(snapshot, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return snapshot;
    }
}

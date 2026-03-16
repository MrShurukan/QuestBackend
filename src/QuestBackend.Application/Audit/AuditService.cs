using Microsoft.EntityFrameworkCore;
using QuestBackend.Application.Abstractions;
using QuestBackend.Contracts;

namespace QuestBackend.Application.Audit;

public sealed class AuditService
{
    private readonly IQuestDbContext _dbContext;

    public AuditService(IQuestDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<AuditEntryResponse>> GetEntriesAsync(int take = 200, CancellationToken cancellationToken = default)
    {
        take = Math.Clamp(take, 1, 500);

        return await _dbContext.AdminAuditLogs
            .AsNoTracking()
            .OrderByDescending(x => x.OccurredAt)
            .Take(take)
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
    }
}

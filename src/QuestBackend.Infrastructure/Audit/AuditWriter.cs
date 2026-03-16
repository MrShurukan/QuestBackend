using QuestBackend.Application.Abstractions;
using QuestBackend.Domain.Audit;
using QuestBackend.Domain.Shared;

namespace QuestBackend.Infrastructure.Audit;

public sealed class AuditWriter : IAuditWriter
{
    private readonly IClock _clock;
    private readonly ICurrentPrincipal _currentPrincipal;
    private readonly IQuestDbContext _dbContext;

    public AuditWriter(IQuestDbContext dbContext, ICurrentPrincipal currentPrincipal, IClock clock)
    {
        _dbContext = dbContext;
        _currentPrincipal = currentPrincipal;
        _clock = clock;
    }

    public async Task WriteAsync(
        string actionType,
        string entityType,
        string entityId,
        string diffJson,
        string? reason,
        CancellationToken cancellationToken = default)
    {
        AdminAuditLog entry = new()
        {
            AdminUserId = _currentPrincipal.AdminUserId,
            ActionType = actionType,
            EntityType = entityType,
            EntityId = entityId,
            DiffJson = diffJson,
            Reason = reason,
            CorrelationId = _currentPrincipal.CorrelationId,
            OccurredAt = _clock.UtcNow,
        };

        await _dbContext.AdminAuditLogs.AddAsync(entry, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}

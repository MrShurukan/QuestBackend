namespace QuestBackend.Contracts;

public sealed record AuditEntryResponse(
    Guid Id,
    Guid? AdminUserId,
    string ActionType,
    string EntityType,
    string EntityId,
    DateTimeOffset OccurredAt,
    string DiffJson,
    string? Reason,
    string? CorrelationId);

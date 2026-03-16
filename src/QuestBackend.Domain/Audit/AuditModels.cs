using QuestBackend.Domain.Shared;

namespace QuestBackend.Domain.Audit;

public sealed class AdminAuditLog : EntityBase
{
    public Guid? AdminUserId { get; set; }

    public string ActionType { get; set; } = string.Empty;

    public string EntityType { get; set; } = string.Empty;

    public string EntityId { get; set; } = string.Empty;

    public DateTimeOffset OccurredAt { get; set; }

    public string DiffJson { get; set; } = "{}";

    public string? Reason { get; set; }

    public string? CorrelationId { get; set; }
}

public sealed class ConfigSnapshot : EntityBase
{
    public string SnapshotType { get; set; } = string.Empty;

    public string PayloadJson { get; set; } = "{}";

    public string? Comment { get; set; }

    public Guid? CreatedByAdminUserId { get; set; }
}

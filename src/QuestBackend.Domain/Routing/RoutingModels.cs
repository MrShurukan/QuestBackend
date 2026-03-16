using QuestBackend.Domain.Questions;
using QuestBackend.Domain.Shared;
using QuestBackend.Domain.Tags;

namespace QuestBackend.Domain.Routing;

public enum QuestionSelectionMode
{
    PoolSlotRotation = 1,
}

public sealed class QuestionPool : EntityBase
{
    public Guid TagId { get; set; }

    public QuestionTag Tag { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public bool IsArchived { get; set; }

    public string? Description { get; set; }

    public int SortOrder { get; set; }

    public List<QuestionPoolEntry> Entries { get; set; } = [];
}

public sealed class QuestionPoolEntry : EntityBase
{
    public Guid PoolId { get; set; }

    public QuestionPool Pool { get; set; } = null!;

    public Guid QuestionId { get; set; }

    public Question Question { get; set; } = null!;

    public int Position { get; set; }

    public bool IsEnabled { get; set; } = true;

    public string? Notes { get; set; }
}

public sealed class QrCode : EntityBase
{
    public Guid TagId { get; set; }

    public QuestionTag Tag { get; set; } = null!;

    public string Slug { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    public int SlotIndex { get; set; }

    public bool IsActive { get; set; } = true;

    public string? Notes { get; set; }

    public DateTimeOffset? LastRotatedAt { get; set; }

    public List<QrBindingOverride> Overrides { get; set; } = [];
}

public sealed class RoutingProfile : EntityBase
{
    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public string? Description { get; set; }

    public DateTimeOffset? ActivatedAt { get; set; }

    public Guid? ActivatedByAdminUserId { get; set; }

    public List<RoutingProfileTagState> TagStates { get; set; } = [];
}

public sealed class RoutingProfileTagState : EntityBase
{
    public Guid RoutingProfileId { get; set; }

    public RoutingProfile RoutingProfile { get; set; } = null!;

    public Guid TagId { get; set; }

    public QuestionTag Tag { get; set; } = null!;

    public Guid? ActivePoolId { get; set; }

    public QuestionPool? ActivePool { get; set; }

    public int RotationOffset { get; set; }

    public QuestionSelectionMode SelectionMode { get; set; } = QuestionSelectionMode.PoolSlotRotation;

    public bool IsEnabled { get; set; } = true;
}

public sealed class QrBindingOverride : EntityBase
{
    public Guid QrCodeId { get; set; }

    public QrCode QrCode { get; set; } = null!;

    public Guid QuestionId { get; set; }

    public Question Question { get; set; } = null!;

    public Guid? ScopeProfileId { get; set; }

    public RoutingProfile? ScopeProfile { get; set; }

    public string? Reason { get; set; }

    public bool IsActive { get; set; } = true;

    public Guid? CreatedByAdminUserId { get; set; }

    public DateTimeOffset CreatedOn { get; set; } = DateTimeOffset.UtcNow;
}

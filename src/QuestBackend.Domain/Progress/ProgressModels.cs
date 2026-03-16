using QuestBackend.Domain.Questions;
using QuestBackend.Domain.Routing;
using QuestBackend.Domain.Shared;
using QuestBackend.Domain.Teams;

namespace QuestBackend.Domain.Progress;

public enum AnswerAttemptResult
{
    Correct = 1,
    Wrong = 2,
    CooldownBlocked = 3,
    NotUnlocked = 4,
    DayClosed = 5,
    QuestNotStarted = 6,
    AlreadySolved = 7,
}

public enum QrScanResolutionResult
{
    Resolved = 1,
    OverrideResolved = 2,
    NotStarted = 3,
    DayClosed = 4,
    InactiveQr = 5,
    InactiveTag = 6,
    NoPoolMatch = 7,
    RequiresAuthentication = 8,
    RequiresTeam = 9,
}

public sealed class TeamQuestionState : EntityBase
{
    public Guid TeamId { get; set; }

    public Team Team { get; set; } = null!;

    public Guid QuestionId { get; set; }

    public Question Question { get; set; } = null!;

    public DateTimeOffset FirstUnlockedAt { get; set; }

    public Guid? UnlockedByQrCodeId { get; set; }

    public QrCode? UnlockedByQrCode { get; set; }

    public Guid? UnlockedByUserId { get; set; }

    public bool IsSolved { get; set; }

    public DateTimeOffset? SolvedAt { get; set; }

    public Guid? SolvedByUserId { get; set; }

    public DateTimeOffset? RewardGrantedAt { get; set; }

    public DateTimeOffset? LastAttemptAt { get; set; }

    public DateTimeOffset? NextAllowedAnswerAt { get; set; }

    public List<TeamAnswerAttempt> Attempts { get; set; } = [];
}

public sealed class TeamAnswerAttempt : EntityBase
{
    public Guid TeamId { get; set; }

    public Team Team { get; set; } = null!;

    public Guid QuestionId { get; set; }

    public Question Question { get; set; } = null!;

    public Guid? SubmittedByUserId { get; set; }

    public string RawAnswer { get; set; } = string.Empty;

    public string NormalizedAnswer { get; set; } = string.Empty;

    public AnswerAttemptResult Result { get; set; }

    public DateTimeOffset AttemptedAt { get; set; }

    public DateTimeOffset? CooldownAppliedUntil { get; set; }

    public string EvaluationSnapshotJson { get; set; } = "{}";
}

public sealed class QrScanEvent : EntityBase
{
    public Guid QrCodeId { get; set; }

    public QrCode QrCode { get; set; } = null!;

    public Guid? ResolvedQuestionId { get; set; }

    public Question? ResolvedQuestion { get; set; }

    public Guid? TeamId { get; set; }

    public Team? Team { get; set; }

    public Guid? ParticipantUserId { get; set; }

    public DateTimeOffset OccurredAt { get; set; }

    public QrScanResolutionResult ResolutionResult { get; set; }

    public string ResolutionMetaJson { get; set; } = "{}";
}

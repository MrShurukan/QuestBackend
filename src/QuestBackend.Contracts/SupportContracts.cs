namespace QuestBackend.Contracts;

public sealed record TeamRewardAdjustmentRequest(Guid TagId, Guid? SourceQuestionId, bool Revoke, string RewardType);

public sealed record TeamQuestionAdjustmentRequest(string? Reason);

public sealed record TeamMemberRemovalRequest(string? Reason);

public sealed record ParticipantPasswordResetRequest(string NewPassword, string? Reason);

public sealed record TeamSupportQuestionResponse(
    Guid Id,
    Guid TagId,
    string TagName,
    string TagColor,
    string Title,
    string State,
    bool IsSolved,
    bool IsAvailableNow,
    DateTimeOffset? FirstUnlockedAt,
    DateTimeOffset? LastAttemptAt,
    DateTimeOffset? NextAllowedAnswerAt);

public sealed record TeamSupportTimelineEntryResponse(
    string Id,
    DateTimeOffset OccurredAt,
    string Kind,
    string Title,
    string Description,
    string? Reason);

public sealed record TeamSupportDetailsResponse(
    TeamSummaryResponse Team,
    IReadOnlyList<TeamSupportQuestionResponse> Questions,
    IReadOnlyList<TeamSupportTimelineEntryResponse> Timeline);

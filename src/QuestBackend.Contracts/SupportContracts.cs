namespace QuestBackend.Contracts;

public sealed record TeamRewardAdjustmentRequest(Guid TagId, Guid? SourceQuestionId, bool Revoke, string RewardType);

public sealed record TeamQuestionAdjustmentRequest(string? Reason);

public sealed record TeamMemberRemovalRequest(string? Reason);

public sealed record TeamSupportDetailsResponse(
    TeamSummaryResponse Team,
    IReadOnlyList<QuestionSummaryResponse> Questions,
    IReadOnlyList<AuditEntryResponse> AuditTrail);

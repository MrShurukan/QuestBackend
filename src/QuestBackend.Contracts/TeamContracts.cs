namespace QuestBackend.Contracts;

public sealed record CreateTeamRequest(string Name, string JoinSecret);

public sealed record JoinTeamRequest(Guid TeamId, string JoinSecret);

public sealed record UpdateTeamJoinSecretRequest(string JoinSecret);

public sealed record TeamSummaryResponse(
    Guid Id,
    string Name,
    string Status,
    bool IsLocked,
    bool IsHidden,
    bool IsDisqualified,
    bool EnigmaSolved,
    DateTimeOffset? EnigmaSolvedAt,
    Guid? CreatedByParticipantId,
    string? FinalTaskPhotoUrl,
    DateTimeOffset? FinalTaskPhotoUploadedAt,
    string? JoinSecretForCaptain,
    IReadOnlyList<TeamMemberResponse> Members);

public sealed record TeamMemberResponse(
    Guid MembershipId,
    Guid ParticipantId,
    string DisplayName,
    string Status,
    DateTimeOffset JoinedAt,
    string? AvatarUrl,
    string Provider);

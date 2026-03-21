namespace QuestBackend.Contracts;

public sealed record CreateTeamRequest(string Name, string JoinSecret);

public sealed record JoinTeamRequest(Guid TeamId, string JoinSecret);

public sealed record TeamSummaryResponse(
    Guid Id,
    string Name,
    string Status,
    bool IsLocked,
    bool IsHidden,
    bool IsDisqualified,
    IReadOnlyList<TeamMemberResponse> Members);

public sealed record TeamMemberResponse(
    Guid MembershipId,
    Guid ParticipantId,
    string DisplayName,
    string Status,
    DateTimeOffset JoinedAt,
    string? AvatarUrl,
    string Provider);

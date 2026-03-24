using QuestBackend.Domain.Enigma;
using QuestBackend.Domain.Participants;
using QuestBackend.Domain.Progress;
using QuestBackend.Domain.Shared;

namespace QuestBackend.Domain.Teams;

public enum TeamStatus
{
    Active = 1,
    Locked = 2,
    Archived = 3,
}

public enum TeamMembershipStatus
{
    Active = 1,
    Removed = 2,
}

public sealed class Team : EntityBase
{
    public string Name { get; set; } = string.Empty;

    public string JoinSecretHash { get; set; } = string.Empty;

    public TeamStatus Status { get; set; } = TeamStatus.Active;

    public Guid? CreatedByUserId { get; set; }

    public string? Notes { get; set; }

    public bool IsLocked { get; set; }

    public bool IsHidden { get; set; }

    public bool IsDisqualified { get; set; }

    public List<TeamMembership> Memberships { get; set; } = [];

    public List<TeamQuestionState> QuestionStates { get; set; } = [];

    public List<TeamRotorReward> RotorRewards { get; set; } = [];

    public List<EnigmaAttempt> EnigmaAttempts { get; set; } = [];

    public List<TeamEnigmaDraft> EnigmaDrafts { get; set; } = [];
}

public sealed class TeamMembership : EntityBase
{
    public Guid TeamId { get; set; }

    public Team Team { get; set; } = null!;

    public Guid ParticipantUserId { get; set; }

    public ParticipantUser ParticipantUser { get; set; } = null!;

    public TeamMembershipStatus Status { get; set; } = TeamMembershipStatus.Active;

    public DateTimeOffset JoinedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? RemovedAt { get; set; }

    public Guid? RemovedByAdminUserId { get; set; }

    public string? RemovalReason { get; set; }
}

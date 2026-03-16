using QuestBackend.Domain.Shared;
using QuestBackend.Domain.Teams;

namespace QuestBackend.Domain.Participants;

public sealed class ParticipantUser : EntityBase
{
    public string Provider { get; set; } = string.Empty;

    public string ProviderSubject { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string? AvatarUrl { get; set; }

    public DateTimeOffset? LastSeenAt { get; set; }

    public bool IsBlocked { get; set; }

    public List<TeamMembership> Memberships { get; set; } = [];
}

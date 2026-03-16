namespace QuestBackend.Application.Shared;

public static class QuestAuthConstants
{
    public const string AdminScheme = "AdminCookie";
    public const string ParticipantScheme = "ParticipantCookie";

    public const string AdminIdClaim = "quest_admin_id";
    public const string ParticipantIdClaim = "quest_participant_id";
    public const string DisplayNameClaim = "quest_display_name";
}

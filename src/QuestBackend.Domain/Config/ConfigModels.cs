using QuestBackend.Domain.Shared;

namespace QuestBackend.Domain.Config;

public sealed class GlobalSettings : EntityBase
{
    public int AnswerCooldownMinutes { get; set; } = 5;

    public int EnigmaCooldownMinutes { get; set; } = 5;

    public int MaxTeamMembers { get; set; } = 4;

    public string DefaultAnswerNormalization { get; set; } = "{\"trimWhitespace\":true,\"ignoreCase\":true,\"collapseInnerWhitespace\":true}";

    public Guid? CurrentQuestDayStateId { get; set; }

    public Guid? CurrentRoutingProfileId { get; set; }

    public Guid? CurrentEnigmaProfileId { get; set; }

    public string FlagsJson { get; set; } = "{}";

    public string Timezone { get; set; } = "UTC";
}

using QuestBackend.Domain.Questions;
using QuestBackend.Domain.Shared;
using QuestBackend.Domain.Tags;
using QuestBackend.Domain.Teams;

namespace QuestBackend.Domain.Enigma;

public enum EnigmaMode
{
    HistoricalLike = 1,
    SimpleCombination = 2,
}

public enum EnigmaAttemptResult
{
    Success = 1,
    Failure = 2,
    CooldownBlocked = 3,
    DayClosed = 4,
}

public sealed class EnigmaProfile : EntityBase
{
    public string Name { get; set; } = string.Empty;

    public EnigmaMode Mode { get; set; } = EnigmaMode.SimpleCombination;

    public bool IsActive { get; set; }

    public int AttemptCooldownMinutes { get; set; } = 5;

    public string SuccessMessage { get; set; } = "Сообщение расшифровано.";

    public string FailureMessage { get; set; } = "Комбинация неверна.";

    public string SecretCombinationJson { get; set; } = "{}";

    public string ConfigJson { get; set; } = "{}";

    public List<EnigmaRotorDefinition> RotorDefinitions { get; set; } = [];
}

public sealed class EnigmaRotorDefinition : EntityBase
{
    public Guid EnigmaProfileId { get; set; }

    public EnigmaProfile EnigmaProfile { get; set; } = null!;

    public Guid TagId { get; set; }

    public QuestionTag Tag { get; set; } = null!;

    public int DisplayOrder { get; set; }

    public string Label { get; set; } = string.Empty;

    public string? ColorOverride { get; set; }

    public int PositionMin { get; set; } = 1;

    public int PositionMax { get; set; } = 9;

    public bool IsActive { get; set; } = true;

    public string MetaJson { get; set; } = "{}";
}

public sealed class TeamRotorReward : EntityBase
{
    public Guid TeamId { get; set; }

    public Team Team { get; set; } = null!;

    public Guid TagId { get; set; }

    public QuestionTag Tag { get; set; } = null!;

    public Guid? SourceQuestionId { get; set; }

    public Question? SourceQuestion { get; set; }

    public string RewardType { get; set; } = "RotorHint";

    public DateTimeOffset GrantedAt { get; set; }

    public Guid? GrantedByAdminUserId { get; set; }

    public bool IsRevoked { get; set; }

    public string PayloadJson { get; set; } = "{}";
}

public sealed class EnigmaAttempt : EntityBase
{
    public Guid TeamId { get; set; }

    public Team Team { get; set; } = null!;

    public Guid EnigmaProfileId { get; set; }

    public EnigmaProfile EnigmaProfile { get; set; } = null!;

    public DateTimeOffset AttemptedAt { get; set; }

    public Guid? SubmittedByUserId { get; set; }

    public string InputJson { get; set; } = "{}";

    public EnigmaAttemptResult Result { get; set; }

    public DateTimeOffset? CooldownAppliedUntil { get; set; }

    public string EvaluationSnapshotJson { get; set; } = "{}";
}

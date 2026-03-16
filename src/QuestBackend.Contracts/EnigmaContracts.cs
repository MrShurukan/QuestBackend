namespace QuestBackend.Contracts;

public sealed record EnigmaRotorDefinitionDto(
    Guid Id,
    Guid TagId,
    string TagName,
    string Color,
    string Label,
    int DisplayOrder,
    int PositionMin,
    int PositionMax,
    bool IsActive,
    int RewardCount);

public sealed record EnigmaStateResponse(
    Guid ProfileId,
    string Mode,
    int AttemptCooldownMinutes,
    DateTimeOffset? NextAllowedAttemptAt,
    IReadOnlyList<EnigmaRotorDefinitionDto> Rotors,
    DateTimeOffset ServerTime);

public sealed record SubmitEnigmaAttemptRequest(IReadOnlyDictionary<Guid, int> RotorPositions);

public sealed record SubmitEnigmaAttemptResponse(
    string Result,
    string Message,
    DateTimeOffset? NextAllowedAttemptAt,
    DateTimeOffset ServerTime);

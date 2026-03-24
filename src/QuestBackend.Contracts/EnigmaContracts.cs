namespace QuestBackend.Contracts;

/// <summary>Rotor definition without team runtime fields (admin / config).</summary>
public sealed record EnigmaRotorDefinitionDto(
    Guid Id,
    Guid TagId,
    string TagName,
    string Color,
    string Label,
    int DisplayOrder,
    int PositionMin,
    int PositionMax,
    bool IsActive);

/// <summary>Rotor as shown to a player: config + unlock + saved dial position.</summary>
public sealed record EnigmaRotorStateDto(
    Guid Id,
    Guid TagId,
    string TagName,
    string Color,
    string Label,
    int DisplayOrder,
    int PositionMin,
    int PositionMax,
    bool IsActive,
    bool IsUnlocked,
    int DraftPosition);

public sealed record EnigmaStateResponse(
    Guid ProfileId,
    string Mode,
    int AttemptCooldownMinutes,
    DateTimeOffset? NextAllowedAttemptAt,
    IReadOnlyList<EnigmaRotorStateDto> Rotors,
    DateTimeOffset ServerTime);

public sealed record SubmitEnigmaAttemptRequest(IReadOnlyDictionary<Guid, int> RotorPositions);

public sealed record SubmitEnigmaAttemptResponse(
    string Result,
    string Message,
    DateTimeOffset? NextAllowedAttemptAt,
    DateTimeOffset ServerTime);

public sealed record UpdateEnigmaDraftPositionsRequest(IReadOnlyDictionary<Guid, int> Positions);

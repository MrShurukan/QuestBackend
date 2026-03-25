namespace QuestBackend.Contracts;

public sealed record TagUpsertRequest(string Code, string Name, string Color, bool IsActive, int SortOrder, string? Description);

public sealed record TagResponse(Guid Id, string Code, string Name, string Color, bool IsActive, int SortOrder, string? Description);

public sealed record QuestionUpsertRequest(
    Guid TagId,
    string Title,
    string BodyRichText,
    string FooterHint,
    string? ImageUrl,
    string Status,
    bool IsActive,
    bool IsArchived,
    string? SupportNotes,
    QuestionAnswerSchemaDto AnswerSchema);

public sealed record QuestionResponse(
    Guid Id,
    Guid TagId,
    string Title,
    string BodyRichText,
    string FooterHint,
    string? ImageUrl,
    string Status,
    bool IsActive,
    bool IsArchived,
    string? SupportNotes,
    QuestionAnswerSchemaDto AnswerSchema);

public sealed record QuestionImageUploadResponse(string ImageUrl);

public sealed record QuestionPoolEntryRequest(Guid QuestionId, int Position, bool IsEnabled, string? Notes);

public sealed record QuestionPoolUpsertRequest(Guid TagId, string Name, bool IsActive, bool IsArchived, string? Description, int SortOrder, IReadOnlyList<QuestionPoolEntryRequest> Entries);

public sealed record QuestionPoolEntryResponse(Guid Id, Guid QuestionId, int Position, bool IsEnabled, string? Notes);

public sealed record QuestionPoolResponse(
    Guid Id,
    Guid TagId,
    string Name,
    bool IsActive,
    bool IsArchived,
    string? Description,
    int SortOrder,
    IReadOnlyList<QuestionPoolEntryResponse> Entries);

public sealed record QrCodeUpsertRequest(Guid TagId, string Slug, string Label, int SlotIndex, bool IsActive, string? Notes);

public sealed record QrCodeResponse(Guid Id, Guid TagId, string Slug, string Label, int SlotIndex, bool IsActive, string? Notes);

public sealed record RoutingProfileTagStateRequest(Guid TagId, Guid? ActivePoolId, int RotationOffset, string SelectionMode, bool IsEnabled);

public sealed record RoutingProfileUpsertRequest(string Name, bool IsActive, string? Description, IReadOnlyList<RoutingProfileTagStateRequest> TagStates);

public sealed record RoutingProfileTagStateResponse(Guid Id, Guid TagId, Guid? ActivePoolId, int RotationOffset, string SelectionMode, bool IsEnabled);

public sealed record RoutingProfileResponse(
    Guid Id,
    string Name,
    bool IsActive,
    string? Description,
    IReadOnlyList<RoutingProfileTagStateResponse> TagStates);

public sealed record QrBindingOverrideRequest(Guid QrCodeId, Guid QuestionId, Guid? ScopeProfileId, bool IsActive, string? Reason);

public sealed record QrBindingOverrideResponse(Guid Id, Guid QrCodeId, Guid QuestionId, Guid? ScopeProfileId, bool IsActive, string? Reason);

public sealed record RoutingPreviewRowResponse(
    Guid QrCodeId,
    string QrLabel,
    string QrSlug,
    Guid TagId,
    string TagName,
    Guid? QuestionId,
    string? QuestionTitle,
    string ResolutionMode);

public sealed record EnigmaRotorDefinitionRequest(Guid TagId, string Label, string? ColorOverride, int DisplayOrder, int PositionMin, int PositionMax, bool IsActive);

public sealed record EnigmaProfileUpsertRequest(
    string Name,
    string Mode,
    bool IsActive,
    int AttemptCooldownMinutes,
    string SuccessMessage,
    string FailureMessage,
    IReadOnlyDictionary<Guid, int> SecretCombination,
    IReadOnlyList<EnigmaRotorDefinitionRequest> Rotors);

public sealed record EnigmaProfileResponse(
    Guid Id,
    string Name,
    string Mode,
    bool IsActive,
    int AttemptCooldownMinutes,
    string SuccessMessage,
    string FailureMessage,
    IReadOnlyDictionary<Guid, int> SecretCombination,
    IReadOnlyList<EnigmaRotorDefinitionDto> Rotors);

public sealed record GlobalSettingsUpdateRequest(
    int AnswerCooldownMinutes,
    int EnigmaCooldownMinutes,
    int MaxTeamMembers,
    string DefaultAnswerNormalization,
    Guid? CurrentQuestDayStateId,
    Guid? CurrentRoutingProfileId,
    Guid? CurrentEnigmaProfileId,
    string FlagsJson,
    string Timezone);

public sealed record GlobalSettingsResponse(
    Guid Id,
    int AnswerCooldownMinutes,
    int EnigmaCooldownMinutes,
    int MaxTeamMembers,
    string DefaultAnswerNormalization,
    Guid? CurrentQuestDayStateId,
    Guid? CurrentRoutingProfileId,
    Guid? CurrentEnigmaProfileId,
    string FlagsJson,
    string Timezone);

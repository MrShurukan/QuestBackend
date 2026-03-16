namespace QuestBackend.Contracts;

public sealed record QuestionAnswerSchemaDto(
    string Kind,
    IReadOnlyList<string> AcceptedAnswers,
    decimal? ExpectedNumericValue,
    decimal? NumericTolerance,
    bool TrimWhitespace,
    bool IgnoreCase,
    bool CollapseInnerWhitespace,
    bool RemovePunctuation);

public sealed record QuestionSummaryResponse(
    Guid Id,
    Guid TagId,
    string TagName,
    string TagColor,
    string Title,
    bool IsSolved,
    DateTimeOffset? NextAllowedAnswerAt,
    DateTimeOffset? LastAttemptAt,
    DateTimeOffset FirstUnlockedAt);

public sealed record QuestionDetailsResponse(
    Guid Id,
    Guid TagId,
    string TagName,
    string TagColor,
    string Title,
    string BodyRichText,
    string FooterHint,
    string? ImageUrl,
    bool IsSolved,
    DateTimeOffset? NextAllowedAnswerAt,
    DateTimeOffset FirstUnlockedAt,
    DateTimeOffset? SolvedAt);

public sealed record SubmitAnswerRequest(string Answer);

public sealed record SubmitAnswerResponse(
    string Result,
    bool IsSolved,
    bool RewardGranted,
    DateTimeOffset? NextAllowedAnswerAt,
    string Message,
    DateTimeOffset ServerTime);

public sealed record QrResolutionResponse(
    string State,
    string Message,
    Guid? QrCodeId,
    Guid? QuestionId,
    QuestionDetailsResponse? Question,
    DateTimeOffset ServerTime);

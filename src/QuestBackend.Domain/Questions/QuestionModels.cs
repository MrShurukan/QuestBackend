using QuestBackend.Domain.Routing;
using QuestBackend.Domain.Shared;
using QuestBackend.Domain.Tags;

namespace QuestBackend.Domain.Questions;

public enum QuestionStatus
{
    Draft = 1,
    Active = 2,
    Disabled = 3,
    Archived = 4,
}

public enum AnswerValidationKind
{
    ExactText = 1,
    NormalizedText = 2,
    Numeric = 3,
}

public sealed class AnswerNormalizationOptions
{
    public bool TrimWhitespace { get; set; } = true;

    public bool IgnoreCase { get; set; } = true;

    public bool CollapseInnerWhitespace { get; set; } = true;

    public bool RemovePunctuation { get; set; }
}

public sealed class AnswerSchema
{
    public AnswerValidationKind Kind { get; set; } = AnswerValidationKind.NormalizedText;

    public List<string> AcceptedAnswers { get; set; } = [];

    public decimal? ExpectedNumericValue { get; set; }

    public decimal? NumericTolerance { get; set; }

    public AnswerNormalizationOptions Normalization { get; set; } = new();
}

public sealed class Question : EntityBase
{
    public Guid TagId { get; set; }

    public QuestionTag Tag { get; set; } = null!;

    public string Title { get; set; } = string.Empty;

    public string BodyRichText { get; set; } = string.Empty;

    public string FooterHint { get; set; } = string.Empty;

    public string? ImageUrl { get; set; }

    public QuestionStatus Status { get; set; } = QuestionStatus.Draft;

    public bool IsActive { get; set; } = true;

    public bool IsArchived { get; set; }

    public AnswerSchema AnswerSchema { get; set; } = new();

    public string UiMetaJson { get; set; } = "{}";

    public string? SupportNotes { get; set; }

    public List<QuestionPoolEntry> PoolEntries { get; set; } = [];
}

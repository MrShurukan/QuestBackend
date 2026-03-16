using QuestBackend.Domain.Questions;
using QuestBackend.Domain.Routing;
using QuestBackend.Domain.Shared;

namespace QuestBackend.Domain.Tags;

public sealed class QuestionTag : EntityBase
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Color { get; set; } = "#000000";

    public bool IsActive { get; set; } = true;

    public int SortOrder { get; set; }

    public string? Description { get; set; }

    public string UiMetaJson { get; set; } = "{}";

    public List<Question> Questions { get; set; } = [];

    public List<QrCode> QrCodes { get; set; } = [];

    public List<QuestionPool> Pools { get; set; } = [];
}

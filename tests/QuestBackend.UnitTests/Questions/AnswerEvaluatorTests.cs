using FluentAssertions;
using QuestBackend.Application.Questions;
using QuestBackend.Domain.Questions;

namespace QuestBackend.UnitTests.Questions;

public sealed class AnswerEvaluatorTests
{
    [Fact]
    public void Evaluate_ShouldAcceptNormalizedCaseInsensitiveAnswer()
    {
        Question question = new()
        {
            AnswerSchema = new AnswerSchema
            {
                Kind = AnswerValidationKind.NormalizedText,
                AcceptedAnswers = ["Enigma"],
                Normalization = new AnswerNormalizationOptions
                {
                    TrimWhitespace = true,
                    IgnoreCase = true,
                    CollapseInnerWhitespace = true,
                },
            },
        };

        AnswerEvaluator evaluator = new();

        var result = evaluator.Evaluate(question, "  enigma ");

        result.IsCorrect.Should().BeTrue();
        result.NormalizedAnswer.Should().Be("ENIGMA");
    }

    [Fact]
    public void Evaluate_ShouldRespectNumericTolerance()
    {
        Question question = new()
        {
            AnswerSchema = new AnswerSchema
            {
                Kind = AnswerValidationKind.Numeric,
                ExpectedNumericValue = 42m,
                NumericTolerance = 0.5m,
            },
        };

        AnswerEvaluator evaluator = new();

        var result = evaluator.Evaluate(question, "42.4");

        result.IsCorrect.Should().BeTrue();
    }
}

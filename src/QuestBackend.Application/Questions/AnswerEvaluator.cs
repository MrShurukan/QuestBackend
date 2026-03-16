using System.Globalization;
using System.Text.RegularExpressions;
using QuestBackend.Application.Abstractions;
using QuestBackend.Application.Shared;
using QuestBackend.Domain.Questions;

namespace QuestBackend.Application.Questions;

public sealed class AnswerEvaluator : IAnswerEvaluator
{
    private static readonly Regex MultiSpaceRegex = new(@"\s+", RegexOptions.Compiled);
    private static readonly Regex PunctuationRegex = new(@"[\p{P}\p{S}]+", RegexOptions.Compiled);

    public AnswerEvaluationResult Evaluate(Question question, string rawAnswer)
    {
        string normalizedAnswer = Normalize(rawAnswer, question.AnswerSchema.Normalization);
        bool isCorrect = question.AnswerSchema.Kind switch
        {
            AnswerValidationKind.ExactText => question.AnswerSchema.AcceptedAnswers.Contains(rawAnswer),
            AnswerValidationKind.NormalizedText => question.AnswerSchema.AcceptedAnswers
                .Select(x => Normalize(x, question.AnswerSchema.Normalization))
                .Contains(normalizedAnswer),
            AnswerValidationKind.Numeric => EvaluateNumeric(question.AnswerSchema, normalizedAnswer),
            _ => false,
        };

        return new AnswerEvaluationResult(
            isCorrect,
            normalizedAnswer,
            AppJson.Serialize(
                new
                {
                    question.AnswerSchema.Kind,
                    question.AnswerSchema.AcceptedAnswers,
                    question.AnswerSchema.ExpectedNumericValue,
                    question.AnswerSchema.NumericTolerance,
                    normalizedAnswer,
                }));
    }

    private static bool EvaluateNumeric(AnswerSchema schema, string normalizedAnswer)
    {
        if (!decimal.TryParse(normalizedAnswer, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal actual))
        {
            return false;
        }

        if (schema.ExpectedNumericValue is null)
        {
            return false;
        }

        decimal tolerance = schema.NumericTolerance ?? 0m;
        decimal delta = Math.Abs(actual - schema.ExpectedNumericValue.Value);
        return delta <= tolerance;
    }

    private static string Normalize(string input, AnswerNormalizationOptions options)
    {
        string value = input;

        if (options.TrimWhitespace)
        {
            value = value.Trim();
        }

        if (options.CollapseInnerWhitespace)
        {
            value = MultiSpaceRegex.Replace(value, " ");
        }

        if (options.RemovePunctuation)
        {
            value = PunctuationRegex.Replace(value, string.Empty);
        }

        if (options.IgnoreCase)
        {
            value = value.ToUpperInvariant();
        }

        return value;
    }
}

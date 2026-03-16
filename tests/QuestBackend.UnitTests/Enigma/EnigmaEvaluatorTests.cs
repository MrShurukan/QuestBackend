using FluentAssertions;
using QuestBackend.Application.Enigma;
using QuestBackend.Domain.Enigma;

namespace QuestBackend.UnitTests.Enigma;

public sealed class EnigmaEvaluatorTests
{
    [Fact]
    public void Evaluate_ShouldReturnSuccess_WhenCombinationMatches()
    {
        Guid blueTagId = Guid.NewGuid();
        Guid redTagId = Guid.NewGuid();

        EnigmaProfile profile = new()
        {
            SecretCombinationJson = "{\"" + blueTagId + "\":4,\"" + redTagId + "\":7}",
        };

        EnigmaEvaluator evaluator = new();

        var result = evaluator.Evaluate(
            profile,
            new Dictionary<Guid, int>
            {
                [blueTagId] = 4,
                [redTagId] = 7,
            });

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Evaluate_ShouldReturnFailure_WhenCombinationDoesNotMatch()
    {
        Guid tagId = Guid.NewGuid();

        EnigmaProfile profile = new()
        {
            SecretCombinationJson = "{\"" + tagId + "\":4}",
        };

        EnigmaEvaluator evaluator = new();

        var result = evaluator.Evaluate(profile, new Dictionary<Guid, int> { [tagId] = 2 });

        result.IsSuccess.Should().BeFalse();
    }
}

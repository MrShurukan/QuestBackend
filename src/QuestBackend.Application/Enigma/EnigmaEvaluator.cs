using QuestBackend.Application.Abstractions;
using QuestBackend.Application.Shared;
using QuestBackend.Domain.Enigma;

namespace QuestBackend.Application.Enigma;

public sealed class EnigmaEvaluator : IEnigmaEvaluator
{
    public EnigmaEvaluationResult Evaluate(EnigmaProfile profile, IReadOnlyDictionary<Guid, int> rotorPositions)
    {
        Dictionary<Guid, int> expected = AppJson.Deserialize<Dictionary<Guid, int>>(profile.SecretCombinationJson) ?? [];

        bool isSuccess = expected.Count > 0
            && expected.Count == rotorPositions.Count
            && expected.All(x => rotorPositions.TryGetValue(x.Key, out int position) && position == x.Value);

        return new EnigmaEvaluationResult(
            isSuccess,
            AppJson.Serialize(
                new
                {
                    expected,
                    actual = rotorPositions,
                    profile.Mode,
                }));
    }
}

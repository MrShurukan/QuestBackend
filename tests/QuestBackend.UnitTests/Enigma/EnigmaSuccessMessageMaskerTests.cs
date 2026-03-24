using FluentAssertions;
using QuestBackend.Application.Enigma;

namespace QuestBackend.UnitTests.Enigma;

public sealed class EnigmaSuccessMessageMaskerTests
{
    [Fact]
    public void Mask_PreservesSpacesAndPunctuationAndDigits()
    {
        Guid a = Guid.Parse("11111111-1111-1111-1111-111111111111");
        Guid b = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var pos = new Dictionary<Guid, int> { [a] = 3, [b] = 5 };
        int seed = EnigmaSuccessMessageMasker.ComputeSeed(pos);
        string input = "Аб 12, test!";
        string masked = EnigmaSuccessMessageMasker.Mask(input, seed);
        masked[2].Should().Be(' ');
        masked[3].Should().Be('1');
        masked[4].Should().Be('2');
        masked[5].Should().Be(',');
        masked[^1].Should().Be('!');
    }

    [Fact]
    public void Mask_IsDeterministicForSameSeed()
    {
        string input = "Привет Hello";
        int seed = 42;
        EnigmaSuccessMessageMasker.Mask(input, seed).Should().Be(EnigmaSuccessMessageMasker.Mask(input, seed));
    }

    [Fact]
    public void Mask_DiffersForDifferentSeeds()
    {
        string input = "SecretText";
        string a = EnigmaSuccessMessageMasker.Mask(input, 1);
        string b = EnigmaSuccessMessageMasker.Mask(input, 2);
        a.Should().NotBe(b);
    }

    [Fact]
    public void ComputeSeed_IsStableForKeyOrder()
    {
        Guid x = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        Guid y = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        int s1 = EnigmaSuccessMessageMasker.ComputeSeed(new Dictionary<Guid, int> { [x] = 1, [y] = 2 });
        int s2 = EnigmaSuccessMessageMasker.ComputeSeed(new Dictionary<Guid, int> { [y] = 2, [x] = 1 });
        s1.Should().Be(s2);
    }
}

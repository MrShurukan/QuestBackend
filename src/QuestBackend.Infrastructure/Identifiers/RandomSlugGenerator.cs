using System.Security.Cryptography;
using QuestBackend.Application.Abstractions;

namespace QuestBackend.Infrastructure.Identifiers;

public sealed class RandomSlugGenerator : ISlugGenerator
{
    private const string Alphabet = "abcdefghijklmnopqrstuvwxyz0123456789";

    public string Generate(int length = 8)
    {
        if (length < 6)
        {
            length = 6;
        }

        Span<char> buffer = stackalloc char[length];
        Span<byte> bytes = stackalloc byte[length];
        RandomNumberGenerator.Fill(bytes);

        for (int i = 0; i < length; i++)
        {
            buffer[i] = Alphabet[bytes[i] % Alphabet.Length];
        }

        return new string(buffer);
    }
}

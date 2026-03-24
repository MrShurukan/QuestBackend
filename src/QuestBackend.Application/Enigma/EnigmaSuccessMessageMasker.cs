using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace QuestBackend.Application.Enigma;

/// <summary>
/// Deterministic "cipher" for wrong enigma attempts: masks SuccessMessage while preserving layout.
/// </summary>
public static class EnigmaSuccessMessageMasker
{
    private static readonly char[] RussianUpper = "АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯ".ToCharArray();
    private static readonly char[] RussianLower = "абвгдеёжзийклмнопрстуфхцчшщъыьэюя".ToCharArray();

    public static int ComputeSeed(IReadOnlyDictionary<Guid, int> normalizedPositions)
    {
        IEnumerable<string> parts = normalizedPositions
            .OrderBy(x => x.Key)
            .Select(x => $"{x.Key:N}:{x.Value}");
        string canonical = string.Join('|', parts);
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
        return BitConverter.ToInt32(hash, 0);
    }

    public static string Mask(string successMessage, int seed)
    {
        Random rng = new(seed);
        StringBuilder sb = new(successMessage.Length);
        foreach (char c in successMessage)
        {
            switch (GetLetterKind(c))
            {
                case LetterKind.RussianUpper:
                    sb.Append(RussianUpper[rng.Next(RussianUpper.Length)]);
                    break;
                case LetterKind.RussianLower:
                    sb.Append(RussianLower[rng.Next(RussianLower.Length)]);
                    break;
                case LetterKind.LatinUpper:
                    sb.Append((char)('A' + rng.Next(26)));
                    break;
                case LetterKind.LatinLower:
                    sb.Append((char)('a' + rng.Next(26)));
                    break;
                default:
                    sb.Append(c);
                    break;
            }
        }

        return sb.ToString();
    }

    private enum LetterKind
    {
        None,
        RussianUpper,
        RussianLower,
        LatinUpper,
        LatinLower,
    }

    private static LetterKind GetLetterKind(char c)
    {
        if (c is >= 'A' and <= 'Z')
        {
            return LetterKind.LatinUpper;
        }

        if (c is >= 'a' and <= 'z')
        {
            return LetterKind.LatinLower;
        }

        if (!char.IsLetter(c))
        {
            return LetterKind.None;
        }

        if (!IsCyrillicScript(c))
        {
            return LetterKind.None;
        }

        UnicodeCategory cat = CharUnicodeInfo.GetUnicodeCategory(c);
        if (cat == UnicodeCategory.UppercaseLetter)
        {
            return LetterKind.RussianUpper;
        }

        if (cat == UnicodeCategory.LowercaseLetter)
        {
            return LetterKind.RussianLower;
        }

        return LetterKind.None;
    }

    private static bool IsCyrillicScript(char c) =>
        c is >= '\u0400' and <= '\u04FF' or >= '\u0500' and <= '\u052F';
}

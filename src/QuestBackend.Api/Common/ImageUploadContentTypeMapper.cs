namespace QuestBackend.Api.Common;

public static class ImageUploadContentTypeMapper
{
    public const string AllowedFormatsMessage = "Допускаются только JPEG, PNG, WebP или HEIC/HEIF.";

    /// <summary>
    /// Resolves file extension from Content-Type and, if unknown, from the original file name
    /// (needed when browsers send HEIC as application/octet-stream or empty MIME).
    /// </summary>
    public static string? MapUploadToExtension(string? contentType, string? fileName)
    {
        string? mediaType = NormalizeMediaType(contentType);
        if (mediaType is not null)
        {
            string? fromType = MapContentTypeToExtension(mediaType);
            if (fromType is not null)
            {
                return fromType;
            }
        }

        return MapFileNameToExtension(fileName);
    }

    public static string? MapContentTypeToExtension(string contentType)
    {
        return contentType switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/webp" => ".webp",
            "image/heic" => ".heic",
            "image/heif" => ".heif",
            // Some clients / cameras use these variants:
            "image/heic-sequence" => ".heic",
            "image/heif-sequence" => ".heif",
            _ => null,
        };
    }

    private static string? NormalizeMediaType(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return null;
        }

        ReadOnlySpan<char> s = contentType.AsSpan().Trim();
        int semi = s.IndexOf(';');
        if (semi >= 0)
        {
            s = s[..semi].Trim();
        }

        return s.Length == 0 ? null : s.ToString().ToLowerInvariant();
    }

    private static string? MapFileNameToExtension(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return null;
        }

        string ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext switch
        {
            ".jpg" or ".jpeg" => ".jpg",
            ".png" => ".png",
            ".webp" => ".webp",
            ".heic" => ".heic",
            ".heif" => ".heif",
            _ => null,
        };
    }
}

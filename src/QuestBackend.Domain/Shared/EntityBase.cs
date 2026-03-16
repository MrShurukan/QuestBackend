using System.Text.RegularExpressions;

namespace QuestBackend.Domain.Shared;

public interface IAuditableEntity
{
    DateTimeOffset CreatedAt { get; set; }

    DateTimeOffset UpdatedAt { get; set; }
}

public interface IVersionedEntity
{
    int Version { get; set; }
}

public abstract class EntityBase : IAuditableEntity, IVersionedEntity
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public int Version { get; set; }
}

public readonly record struct SlugToken
{
    private static readonly Regex AllowedPattern = new("^[a-z0-9]{6,32}$", RegexOptions.Compiled);

    public SlugToken(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || !AllowedPattern.IsMatch(value))
        {
            throw new ArgumentException("Slug must contain 6-32 lowercase latin letters or digits.", nameof(value));
        }

        Value = value;
    }

    public string Value { get; }

    public override string ToString() => Value;
}

public readonly record struct HexColor
{
    private static readonly Regex AllowedPattern = new("^#[0-9A-Fa-f]{6}$", RegexOptions.Compiled);

    public HexColor(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || !AllowedPattern.IsMatch(value))
        {
            throw new ArgumentException("Color must be a hex string in the form #RRGGBB.", nameof(value));
        }

        Value = value.ToUpperInvariant();
    }

    public string Value { get; }

    public override string ToString() => Value;
}

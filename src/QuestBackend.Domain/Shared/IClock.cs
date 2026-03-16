namespace QuestBackend.Domain.Shared;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}

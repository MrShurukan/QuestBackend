using QuestBackend.Domain.Shared;

namespace QuestBackend.Infrastructure.Time;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}

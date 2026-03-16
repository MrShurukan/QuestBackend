using QuestBackend.Domain.Shared;

namespace QuestBackend.Domain.QuestDay;

public enum QuestDayStatus
{
    NotStarted = 1,
    Running = 2,
    DayClosed = 3,
}

public sealed class QuestDayState : EntityBase
{
    public string DayCode { get; set; } = "default-day";

    public QuestDayStatus Status { get; set; } = QuestDayStatus.NotStarted;

    public DateTimeOffset? StartedAt { get; set; }

    public Guid? StartedByAdminUserId { get; set; }

    public DateTimeOffset? EndedAt { get; set; }

    public Guid? EndedByAdminUserId { get; set; }

    public string PreStartMessage { get; set; } = "Игра еще не началась.";

    public string DayClosedMessage { get; set; } = "Игровой день завершен.";
}

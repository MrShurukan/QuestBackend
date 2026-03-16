namespace QuestBackend.Contracts;

public sealed record QuestDayStateResponse(
    Guid Id,
    string DayCode,
    string Status,
    string Message,
    DateTimeOffset ServerTime,
    DateTimeOffset? StartedAt,
    DateTimeOffset? EndedAt);

public sealed record UpdateQuestDayMessagesRequest(string PreStartMessage, string DayClosedMessage);

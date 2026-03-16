using Microsoft.EntityFrameworkCore;
using QuestBackend.Application.Abstractions;
using QuestBackend.Application.Shared;
using QuestBackend.Contracts;
using QuestBackend.Domain.Config;
using QuestBackend.Domain.QuestDay;
using QuestBackend.Domain.Shared;

namespace QuestBackend.Application.QuestDay;

public sealed class QuestDayService : IQuestDayLifecycleGate
{
    private readonly IAuditWriter _auditWriter;
    private readonly IClock _clock;
    private readonly IConfigSnapshotService _configSnapshotService;
    private readonly ICurrentPrincipal _currentPrincipal;
    private readonly IQuestDbContext _dbContext;

    public QuestDayService(
        IQuestDbContext dbContext,
        ICurrentPrincipal currentPrincipal,
        IClock clock,
        IAuditWriter auditWriter,
        IConfigSnapshotService configSnapshotService)
    {
        _dbContext = dbContext;
        _currentPrincipal = currentPrincipal;
        _clock = clock;
        _auditWriter = auditWriter;
        _configSnapshotService = configSnapshotService;
    }

    public async Task<QuestDayStateResponse> GetPublicStateAsync(CancellationToken cancellationToken = default)
    {
        QuestDayState state = await GetCurrentStateEntityAsync(cancellationToken);
        return ToResponse(state, _clock.UtcNow);
    }

    public async Task<QuestDayStateResponse> GetAdminStateAsync(CancellationToken cancellationToken = default)
    {
        QuestDayState state = await GetCurrentStateEntityAsync(cancellationToken);
        return ToResponse(state, _clock.UtcNow);
    }

    public async Task<QuestDayStateResponse> StartAsync(CancellationToken cancellationToken = default)
    {
        QuestDayState state = await GetCurrentStateEntityAsync(cancellationToken);
        await _configSnapshotService.CreateSnapshotAsync("QuestDayStart", "Quest day started", cancellationToken);

        state.Status = QuestDayStatus.Running;
        state.StartedAt = _clock.UtcNow;
        state.StartedByAdminUserId = _currentPrincipal.AdminUserId;
        state.UpdatedAt = _clock.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditWriter.WriteAsync("StartQuestDay", nameof(QuestDayState), state.Id.ToString(), AppJson.Serialize(state), null, cancellationToken);

        return ToResponse(state, _clock.UtcNow);
    }

    public async Task<QuestDayStateResponse> FinishAsync(CancellationToken cancellationToken = default)
    {
        QuestDayState state = await GetCurrentStateEntityAsync(cancellationToken);
        await _configSnapshotService.CreateSnapshotAsync("QuestDayFinish", "Quest day finished", cancellationToken);

        state.Status = QuestDayStatus.DayClosed;
        state.EndedAt = _clock.UtcNow;
        state.EndedByAdminUserId = _currentPrincipal.AdminUserId;
        state.UpdatedAt = _clock.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditWriter.WriteAsync("FinishQuestDay", nameof(QuestDayState), state.Id.ToString(), AppJson.Serialize(state), null, cancellationToken);

        return ToResponse(state, _clock.UtcNow);
    }

    public async Task<QuestDayStateResponse> UpdateMessagesAsync(UpdateQuestDayMessagesRequest request, CancellationToken cancellationToken = default)
    {
        QuestDayState state = await GetCurrentStateEntityAsync(cancellationToken);
        state.PreStartMessage = request.PreStartMessage;
        state.DayClosedMessage = request.DayClosedMessage;
        state.UpdatedAt = _clock.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _auditWriter.WriteAsync("UpdateQuestDayMessages", nameof(QuestDayState), state.Id.ToString(), AppJson.Serialize(request), null, cancellationToken);

        return ToResponse(state, _clock.UtcNow);
    }

    public async Task<QuestLifecycleDecision> GetDecisionAsync(CancellationToken cancellationToken = default)
    {
        QuestDayState state = await GetCurrentStateEntityAsync(cancellationToken);

        return state.Status switch
        {
            QuestDayStatus.Running => new QuestLifecycleDecision(state.Status, "Quest day is running.", state, true, true),
            QuestDayStatus.DayClosed => new QuestLifecycleDecision(state.Status, state.DayClosedMessage, state, false, false),
            _ => new QuestLifecycleDecision(state.Status, state.PreStartMessage, state, false, false),
        };
    }

    public async Task<QuestDayState> GetCurrentStateEntityAsync(CancellationToken cancellationToken = default)
    {
        GlobalSettings? settings = await _dbContext.GlobalSettings.FirstOrDefaultAsync(cancellationToken);
        QuestDayState? state = null;

        if (settings?.CurrentQuestDayStateId is Guid currentStateId)
        {
            state = await _dbContext.QuestDayStates.SingleOrDefaultAsync(x => x.Id == currentStateId, cancellationToken);
        }

        state ??= await _dbContext.QuestDayStates
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (state is not null)
        {
            return state;
        }

        state = new QuestDayState
        {
            DayCode = "default-day",
            Status = QuestDayStatus.NotStarted,
            PreStartMessage = "Игра еще не началась.",
            DayClosedMessage = "Игровой день завершен.",
            CreatedAt = _clock.UtcNow,
            UpdatedAt = _clock.UtcNow,
        };

        await _dbContext.QuestDayStates.AddAsync(state, cancellationToken);

        if (settings is null)
        {
            settings = new GlobalSettings
            {
                CurrentQuestDayStateId = state.Id,
                CreatedAt = _clock.UtcNow,
                UpdatedAt = _clock.UtcNow,
            };

            await _dbContext.GlobalSettings.AddAsync(settings, cancellationToken);
        }
        else
        {
            settings.CurrentQuestDayStateId = state.Id;
            settings.UpdatedAt = _clock.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return state;
    }

    private static QuestDayStateResponse ToResponse(QuestDayState state, DateTimeOffset serverTime)
    {
        string message = state.Status switch
        {
            QuestDayStatus.DayClosed => state.DayClosedMessage,
            QuestDayStatus.Running => "Игровой день идет.",
            _ => state.PreStartMessage,
        };

        return new QuestDayStateResponse(
            state.Id,
            state.DayCode,
            state.Status.ToString(),
            message,
            serverTime,
            state.StartedAt,
            state.EndedAt);
    }
}

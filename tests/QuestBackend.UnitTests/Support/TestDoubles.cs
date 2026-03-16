using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using QuestBackend.Application.Abstractions;
using QuestBackend.Domain.Audit;
using QuestBackend.Domain.Progress;
using QuestBackend.Domain.QuestDay;
using QuestBackend.Domain.Questions;
using QuestBackend.Domain.Shared;
using QuestBackend.Infrastructure.Persistence;

namespace QuestBackend.UnitTests.Support;

internal sealed class FakeClock : IClock
{
    public DateTimeOffset UtcNow { get; set; } = new(2026, 3, 16, 12, 0, 0, TimeSpan.Zero);
}

internal sealed class FakeCurrentPrincipal : ICurrentPrincipal
{
    public Guid? AdminUserId { get; set; }

    public Guid? ParticipantUserId { get; set; }

    public string? ParticipantDisplayName { get; set; }

    public bool IsAdminAuthenticated { get; set; }

    public bool IsParticipantAuthenticated { get; set; }

    public string? CorrelationId { get; set; } = Guid.NewGuid().ToString("N");
}

internal sealed class FakeAuditWriter : IAuditWriter
{
    public List<string> Actions { get; } = [];

    public Task WriteAsync(string actionType, string entityType, string entityId, string diffJson, string? reason, CancellationToken cancellationToken = default)
    {
        Actions.Add($"{actionType}:{entityType}:{entityId}");
        return Task.CompletedTask;
    }
}

internal sealed class FakeSnapshotService : IConfigSnapshotService
{
    public int SnapshotCount { get; private set; }

    public Task<ConfigSnapshot> CreateSnapshotAsync(string snapshotType, string? comment, CancellationToken cancellationToken = default)
    {
        SnapshotCount++;
        return Task.FromResult(
            new ConfigSnapshot
            {
                SnapshotType = snapshotType,
                Comment = comment,
            });
    }
}

internal sealed class FakePasswordHasher : IPasswordHasher
{
    public string Hash(string password) => $"HASH::{password}";

    public bool Verify(string password, string passwordHash) => passwordHash == Hash(password);
}

internal sealed class AlwaysOpenLifecycleGate : IQuestDayLifecycleGate
{
    public Task<QuestLifecycleDecision> GetDecisionAsync(CancellationToken cancellationToken = default)
    {
        QuestDayState state = new()
        {
            DayCode = "test",
            Status = QuestDayStatus.Running,
        };

        return Task.FromResult(new QuestLifecycleDecision(QuestDayStatus.Running, "running", state, true, true));
    }
}

internal sealed class StaticQuestionRoutingResolver : IQuestionRoutingResolver
{
    private readonly QuestionRoutingResolution _resolution;

    public StaticQuestionRoutingResolver(QuestionRoutingResolution resolution)
    {
        _resolution = resolution;
    }

    public Task<QuestionRoutingResolution> ResolveAsync(Guid qrCodeId, CancellationToken cancellationToken = default)
        => Task.FromResult(_resolution);
}

internal static class TestDbContextFactory
{
    public static QuestDbContext Create()
    {
        SqliteConnection connection = new("Data Source=:memory:");
        connection.Open();

        DbContextOptions<QuestDbContext> options = new DbContextOptionsBuilder<QuestDbContext>()
            .UseSqlite(connection)
            .Options;

        QuestDbContext context = new(options);
        context.Database.EnsureCreated();
        return context;
    }
}

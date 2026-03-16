using FluentAssertions;
using QuestBackend.Application.QuestDay;
using QuestBackend.Domain.Config;
using QuestBackend.Domain.QuestDay;
using QuestBackend.UnitTests.Support;

namespace QuestBackend.UnitTests.QuestDay;

public sealed class QuestDayServiceTests
{
    [Fact]
    public async Task StartAsync_ShouldMoveQuestDayToRunning()
    {
        await using var dbContext = TestDbContextFactory.Create();
        FakeClock clock = new();
        FakeCurrentPrincipal principal = new() { AdminUserId = Guid.NewGuid(), IsAdminAuthenticated = true };
        FakeAuditWriter auditWriter = new();
        FakeSnapshotService snapshotService = new();

        QuestDayState questDay = new()
        {
            DayCode = "day-1",
            Status = QuestDayStatus.NotStarted,
        };
        GlobalSettings settings = new() { CurrentQuestDayStateId = questDay.Id };

        await dbContext.AddRangeAsync(questDay, settings);
        await dbContext.SaveChangesAsync();

        QuestDayService service = new(dbContext, principal, clock, auditWriter, snapshotService);

        var response = await service.StartAsync();
        var decision = await service.GetDecisionAsync();

        response.Status.Should().Be(nameof(QuestDayStatus.Running));
        decision.AllowsUnlock.Should().BeTrue();
        snapshotService.SnapshotCount.Should().Be(1);
        auditWriter.Actions.Should().ContainSingle();
    }

    [Fact]
    public async Task FinishAsync_ShouldBlockSubmissions()
    {
        await using var dbContext = TestDbContextFactory.Create();
        FakeClock clock = new();
        FakeCurrentPrincipal principal = new() { AdminUserId = Guid.NewGuid(), IsAdminAuthenticated = true };
        FakeAuditWriter auditWriter = new();
        FakeSnapshotService snapshotService = new();

        QuestDayState questDay = new()
        {
            DayCode = "day-1",
            Status = QuestDayStatus.Running,
        };
        GlobalSettings settings = new() { CurrentQuestDayStateId = questDay.Id };

        await dbContext.AddRangeAsync(questDay, settings);
        await dbContext.SaveChangesAsync();

        QuestDayService service = new(dbContext, principal, clock, auditWriter, snapshotService);

        await service.FinishAsync();
        var decision = await service.GetDecisionAsync();

        decision.Status.Should().Be(QuestDayStatus.DayClosed);
        decision.AllowsUnlock.Should().BeFalse();
        decision.AllowsSubmissions.Should().BeFalse();
    }
}

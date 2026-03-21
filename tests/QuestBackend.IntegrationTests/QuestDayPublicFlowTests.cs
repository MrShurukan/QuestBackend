using System.Net.Http.Json;
using FluentAssertions;
using QuestBackend.Contracts;
using QuestBackend.IntegrationTests.Infrastructure;

namespace QuestBackend.IntegrationTests;

public sealed class QuestDayPublicFlowTests
{
    [Fact]
    public async Task QuestDayPublic_ShouldReturnState_WithoutAuthentication()
    {
        await using QuestBackendApiFactory factory = new();
        await factory.InitializeAsync();
        await factory.ResetDatabaseAsync();

        HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/api/quest-day/public");
        response.EnsureSuccessStatusCode();
        QuestDayStateResponse state = (await response.Content.ReadFromJsonAsync<QuestDayStateResponse>())!;
        state.Status.Should().NotBeNullOrWhiteSpace();
        state.Message.Should().NotBeNull();
    }
}

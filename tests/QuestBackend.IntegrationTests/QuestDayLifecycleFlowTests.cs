using System.Net.Http.Json;
using FluentAssertions;
using QuestBackend.Contracts;
using QuestBackend.IntegrationTests.Infrastructure;

namespace QuestBackend.IntegrationTests;

public sealed class QuestDayLifecycleFlowTests
{
    [Fact]
    public async Task FinishDay_ShouldBlockScansAnswersAndEnigmaAttempts()
    {
        await using QuestBackendApiFactory factory = new();
        await factory.InitializeAsync();
        await factory.ResetDatabaseAsync();

        SeededGameConfig config = await factory.SeedBasicConfigurationAsync();

        HttpClient adminClient = factory.CreateCookieClient();
        HttpClient participantClient = factory.CreateCookieClient();

        await factory.LoginAdminAsync(adminClient);
        await factory.RegisterParticipantAsync(participantClient, "player-1", "Player");
        await participantClient.PostAsJsonAsync("/api/teams", new CreateTeamRequest("TeamDay", "secret"));

        await adminClient.PostAsync("/api/admin/quest-day/start", null);
        await participantClient.GetAsync($"/api/public/qr/{config.RedSlug}");
        await adminClient.PostAsync("/api/admin/quest-day/finish", null);

        QrResolutionResponse scanAfterFinish = (await (await participantClient.GetAsync($"/api/public/qr/{config.BlueSlug}")).Content.ReadFromJsonAsync<QrResolutionResponse>())!;
        scanAfterFinish.State.Should().Be("day_closed");

        SubmitAnswerResponse answerAfterFinish = (await (await participantClient.PostAsJsonAsync($"/api/questions/{config.RedQuestionId}/answers", new SubmitAnswerRequest("ENIGMA"))).Content.ReadFromJsonAsync<SubmitAnswerResponse>())!;
        answerAfterFinish.Result.Should().Be("day_closed");

        SubmitEnigmaAttemptResponse enigmaAfterFinish = (await (await participantClient.PostAsJsonAsync("/api/enigma/attempts", new SubmitEnigmaAttemptRequest(new Dictionary<Guid, int>()))).Content.ReadFromJsonAsync<SubmitEnigmaAttemptResponse>())!;
        enigmaAfterFinish.Result.Should().Be("day_closed");
    }
}

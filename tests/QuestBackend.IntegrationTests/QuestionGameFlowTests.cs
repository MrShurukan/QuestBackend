using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using QuestBackend.Contracts;
using QuestBackend.IntegrationTests.Infrastructure;

namespace QuestBackend.IntegrationTests;

public sealed class QuestionGameFlowTests
{
    [Fact]
    public async Task QrScanAndAnswers_ShouldUnlockQuestion_ApplyCooldown_AndGrantReward()
    {
        await using QuestBackendApiFactory factory = new();
        await factory.InitializeAsync();
        await factory.ResetDatabaseAsync();

        SeededGameConfig config = await factory.SeedBasicConfigurationAsync();
        await factory.StartQuestDayAsync();

        HttpClient client = factory.CreateCookieClient();
        await factory.LoginParticipantAsync(client, "player-1", "Player");
        await client.PostAsJsonAsync("/api/teams", new CreateTeamRequest("TeamOne", "secret"));

        HttpResponseMessage scanResponse = await client.GetAsync($"/api/public/qr/{config.BlueSlug}");
        scanResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        QrResolutionResponse scan = (await scanResponse.Content.ReadFromJsonAsync<QrResolutionResponse>())!;
        scan.State.Should().Be("resolved");
        scan.Question.Should().NotBeNull();

        HttpResponseMessage wrongResponseMessage = await client.PostAsJsonAsync($"/api/questions/{config.BlueQuestionId}/answers", new SubmitAnswerRequest("5"));
        SubmitAnswerResponse wrongResponse = (await wrongResponseMessage.Content.ReadFromJsonAsync<SubmitAnswerResponse>())!;
        wrongResponse.Result.Should().Be("wrong");
        wrongResponse.NextAllowedAnswerAt.Should().NotBeNull();

        HttpResponseMessage blockedResponseMessage = await client.PostAsJsonAsync($"/api/questions/{config.BlueQuestionId}/answers", new SubmitAnswerRequest("4"));
        SubmitAnswerResponse blockedResponse = (await blockedResponseMessage.Content.ReadFromJsonAsync<SubmitAnswerResponse>())!;
        blockedResponse.Result.Should().Be("cooldown");

        var team = await factory.GetTeamByNameAsync("TeamOne");
        await factory.SetNextAllowedAnswerAtAsync(team.Id, config.BlueQuestionId, DateTimeOffset.UtcNow.AddSeconds(-1));

        HttpResponseMessage correctResponseMessage = await client.PostAsJsonAsync($"/api/questions/{config.BlueQuestionId}/answers", new SubmitAnswerRequest("4"));
        SubmitAnswerResponse correctResponse = (await correctResponseMessage.Content.ReadFromJsonAsync<SubmitAnswerResponse>())!;
        correctResponse.Result.Should().Be("correct");
        correctResponse.RewardGranted.Should().BeTrue();

        HttpResponseMessage enigmaStateResponse = await client.GetAsync("/api/enigma/state");
        enigmaStateResponse.EnsureSuccessStatusCode();
        EnigmaStateResponse enigmaState = (await enigmaStateResponse.Content.ReadFromJsonAsync<EnigmaStateResponse>())!;
        enigmaState.Rotors.Should().Contain(x => x.TagId == config.BlueTagId && x.RewardCount == 1);
    }
}

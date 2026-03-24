using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using QuestBackend.Contracts;
using QuestBackend.IntegrationTests.Infrastructure;

namespace QuestBackend.IntegrationTests;

public sealed class QuestionGameFlowTests
{
    [Fact]
    public async Task QrScanAndAnswers_ShouldUnlockQuestion_ApplyCooldown_AndUnlockEnigmaRotor()
    {
        await using QuestBackendApiFactory factory = new();
        await factory.InitializeAsync();
        await factory.ResetDatabaseAsync();

        SeededGameConfig config = await factory.SeedBasicConfigurationAsync();
        await factory.StartQuestDayAsync();

        HttpClient client = factory.CreateCookieClient();
        await factory.RegisterParticipantAsync(client, "player-1", "Player");
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
        correctResponse.RewardGranted.Should().BeFalse();

        HttpResponseMessage enigmaStateResponse = await client.GetAsync("/api/enigma/state");
        enigmaStateResponse.EnsureSuccessStatusCode();
        EnigmaStateResponse enigmaState = (await enigmaStateResponse.Content.ReadFromJsonAsync<EnigmaStateResponse>())!;
        enigmaState.Rotors.Should().Contain(x => x.TagId == config.BlueTagId && x.IsUnlocked);
    }

    [Fact]
    public async Task KnownQuestions_EnigmaSuccess_ShouldExposeUnlockedQuestions_AndAcceptCorrectCombination()
    {
        await using QuestBackendApiFactory factory = new();
        await factory.InitializeAsync();
        await factory.ResetDatabaseAsync();

        SeededGameConfig config = await factory.SeedBasicConfigurationAsync();
        await factory.StartQuestDayAsync();

        HttpClient client = factory.CreateCookieClient();
        await factory.RegisterParticipantAsync(client, "player-enigma", "Player");
        await client.PostAsJsonAsync("/api/teams", new CreateTeamRequest("TeamEnigma", "secret"));

        await client.GetAsync($"/api/public/qr/{config.BlueSlug}");
        HttpResponseMessage wrongBlue = await client.PostAsJsonAsync(
            $"/api/questions/{config.BlueQuestionId}/answers",
            new SubmitAnswerRequest("5"));
        (await wrongBlue.Content.ReadFromJsonAsync<SubmitAnswerResponse>())!.Result.Should().Be("wrong");

        var team = await factory.GetTeamByNameAsync("TeamEnigma");
        await factory.SetNextAllowedAnswerAtAsync(team.Id, config.BlueQuestionId, DateTimeOffset.UtcNow.AddSeconds(-1));
        HttpResponseMessage correctBlue = await client.PostAsJsonAsync(
            $"/api/questions/{config.BlueQuestionId}/answers",
            new SubmitAnswerRequest("4"));
        (await correctBlue.Content.ReadFromJsonAsync<SubmitAnswerResponse>())!.Result.Should().Be("correct");

        HttpResponseMessage knownResponse = await client.GetAsync("/api/questions/known");
        knownResponse.EnsureSuccessStatusCode();
        List<QuestionSummaryResponse> known = (await knownResponse.Content.ReadFromJsonAsync<List<QuestionSummaryResponse>>())!;
        known.Should().Contain(x => x.Id == config.BlueQuestionId);

        HttpResponseMessage detailsResponse = await client.GetAsync($"/api/questions/{config.BlueQuestionId}");
        detailsResponse.EnsureSuccessStatusCode();
        QuestionDetailsResponse details = (await detailsResponse.Content.ReadFromJsonAsync<QuestionDetailsResponse>())!;
        details.Id.Should().Be(config.BlueQuestionId);

        await client.GetAsync($"/api/public/qr/{config.RedSlug}");
        HttpResponseMessage correctRed = await client.PostAsJsonAsync(
            $"/api/questions/{config.RedQuestionId}/answers",
            new SubmitAnswerRequest("ENIGMA"));
        (await correctRed.Content.ReadFromJsonAsync<SubmitAnswerResponse>())!.Result.Should().Be("correct");

        Dictionary<Guid, int> combination = new()
        {
            [config.BlueTagId] = 4,
            [config.RedTagId] = 7,
        };

        HttpResponseMessage enigmaAttempt = await client.PostAsJsonAsync(
            "/api/enigma/attempts",
            new SubmitEnigmaAttemptRequest(combination));
        enigmaAttempt.EnsureSuccessStatusCode();
        SubmitEnigmaAttemptResponse enigmaResult = (await enigmaAttempt.Content.ReadFromJsonAsync<SubmitEnigmaAttemptResponse>())!;
        enigmaResult.Result.Should().Be("success");
    }
}

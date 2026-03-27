using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using QuestBackend.Contracts;
using QuestBackend.Domain.Teams;
using QuestBackend.IntegrationTests.Infrastructure;

namespace QuestBackend.IntegrationTests;

public sealed class FinalTaskPhotoFlowTests
{
    private static readonly byte[] TinyPng = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");

    private static MultipartFormDataContent CreateFinalPhotoForm()
    {
        MultipartFormDataContent form = new();
        ByteArrayContent file = new(TinyPng);
        file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        form.Add(file, "photo", "final.png");
        return form;
    }

    private static MultipartFormDataContent CreateFinalPhotoFormHeic()
    {
        MultipartFormDataContent form = new();
        ByteArrayContent file = new(TinyPng);
        file.Headers.ContentType = new MediaTypeHeaderValue("image/heic");
        form.Add(file, "photo", "final.heic");
        return form;
    }

    /// <summary>
    /// Many mobile browsers send HEIC as application/octet-stream; extension must disambiguate.
    /// </summary>
    private static MultipartFormDataContent CreateFinalPhotoFormHeicOctetStream()
    {
        MultipartFormDataContent form = new();
        ByteArrayContent file = new(TinyPng);
        file.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        form.Add(file, "photo", "IMG_1234.heic");
        return form;
    }

    [Fact]
    public async Task FinalTaskPhoto_CaptainAfterEnigmaSolved_ShouldPersist_AndRejectSecondUpload()
    {
        await using QuestBackendApiFactory factory = new();
        await factory.InitializeAsync();
        await factory.ResetDatabaseAsync();

        SeededGameConfig config = await factory.SeedBasicConfigurationAsync();
        await factory.StartQuestDayAsync();

        HttpClient captain = factory.CreateCookieClient();
        await factory.RegisterParticipantAsync(captain, "cap-final", "Captain");
        await captain.PostAsJsonAsync("/api/teams", new CreateTeamRequest("TeamFinal", "secret"));

        await captain.GetAsync($"/api/public/qr/{config.BlueSlug}");
        await captain.PostAsJsonAsync($"/api/questions/{config.BlueQuestionId}/answers", new SubmitAnswerRequest("5"));
        Team teamEntity = await factory.GetTeamByNameAsync("TeamFinal");
        await factory.SetNextAllowedAnswerAtAsync(teamEntity.Id, config.BlueQuestionId, DateTimeOffset.UtcNow.AddSeconds(-1));
        await captain.PostAsJsonAsync($"/api/questions/{config.BlueQuestionId}/answers", new SubmitAnswerRequest("4"));

        await captain.GetAsync($"/api/public/qr/{config.RedSlug}");
        await captain.PostAsJsonAsync($"/api/questions/{config.RedQuestionId}/answers", new SubmitAnswerRequest("ENIGMA"));

        await captain.PostAsJsonAsync(
            "/api/enigma/attempts",
            new SubmitEnigmaAttemptRequest(
                new Dictionary<Guid, int> { [config.BlueTagId] = 4, [config.RedTagId] = 7 }));

        using (MultipartFormDataContent form = CreateFinalPhotoForm())
        {
            HttpResponseMessage first = await captain.PostAsync("/api/teams/me/final-task-photo", form);
            first.StatusCode.Should().Be(HttpStatusCode.OK);
            TeamSummaryResponse updated = (await first.Content.ReadFromJsonAsync<TeamSummaryResponse>())!;
            updated.FinalTaskPhotoUrl.Should().NotBeNullOrEmpty();
            updated.FinalTaskPhotoUploadedAt.Should().NotBeNull();
        }

        HttpResponseMessage myTeam = await captain.GetAsync("/api/teams/me");
        myTeam.EnsureSuccessStatusCode();
        TeamSummaryResponse me = (await myTeam.Content.ReadFromJsonAsync<TeamSummaryResponse>())!;
        me.FinalTaskPhotoUrl.Should().StartWith("/uploads/team-final/");

        using (MultipartFormDataContent form2 = CreateFinalPhotoForm())
        {
            HttpResponseMessage second = await captain.PostAsync("/api/teams/me/final-task-photo", form2);
            second.StatusCode.Should().Be(HttpStatusCode.Conflict);
        }
    }

    [Fact]
    public async Task FinalTaskPhoto_NonCaptain_ShouldReturnForbidden()
    {
        await using QuestBackendApiFactory factory = new();
        await factory.InitializeAsync();
        await factory.ResetDatabaseAsync();

        SeededGameConfig config = await factory.SeedBasicConfigurationAsync();
        await factory.StartQuestDayAsync();

        HttpClient captain = factory.CreateCookieClient();
        HttpClient member = factory.CreateCookieClient();
        await factory.RegisterParticipantAsync(captain, "cap-2", "Cap");
        await factory.RegisterParticipantAsync(member, "mem-2", "Mem");

        HttpResponseMessage create = await captain.PostAsJsonAsync("/api/teams", new CreateTeamRequest("TeamCap", "sec"));
        create.EnsureSuccessStatusCode();
        TeamSummaryResponse created = (await create.Content.ReadFromJsonAsync<TeamSummaryResponse>())!;
        await member.PostAsJsonAsync("/api/teams/join", new JoinTeamRequest(created.Id, "sec"));

        await captain.GetAsync($"/api/public/qr/{config.BlueSlug}");
        await captain.PostAsJsonAsync($"/api/questions/{config.BlueQuestionId}/answers", new SubmitAnswerRequest("5"));
        Team teamEntity = await factory.GetTeamByNameAsync("TeamCap");
        await factory.SetNextAllowedAnswerAtAsync(teamEntity.Id, config.BlueQuestionId, DateTimeOffset.UtcNow.AddSeconds(-1));
        await captain.PostAsJsonAsync($"/api/questions/{config.BlueQuestionId}/answers", new SubmitAnswerRequest("4"));

        await captain.GetAsync($"/api/public/qr/{config.RedSlug}");
        await captain.PostAsJsonAsync($"/api/questions/{config.RedQuestionId}/answers", new SubmitAnswerRequest("ENIGMA"));
        await captain.PostAsJsonAsync(
            "/api/enigma/attempts",
            new SubmitEnigmaAttemptRequest(
                new Dictionary<Guid, int> { [config.BlueTagId] = 4, [config.RedTagId] = 7 }));

        using (MultipartFormDataContent form = CreateFinalPhotoForm())
        {
            HttpResponseMessage denied = await member.PostAsync("/api/teams/me/final-task-photo", form);
            denied.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }
    }

    [Fact]
    public async Task FinalTaskPhoto_HeicContentType_ShouldPersist()
    {
        await using QuestBackendApiFactory factory = new();
        await factory.InitializeAsync();
        await factory.ResetDatabaseAsync();

        SeededGameConfig config = await factory.SeedBasicConfigurationAsync();
        await factory.StartQuestDayAsync();

        HttpClient captain = factory.CreateCookieClient();
        await factory.RegisterParticipantAsync(captain, "cap-heic", "Captain Heic");
        await captain.PostAsJsonAsync("/api/teams", new CreateTeamRequest("TeamHeic", "secret"));

        await captain.GetAsync($"/api/public/qr/{config.BlueSlug}");
        await captain.PostAsJsonAsync($"/api/questions/{config.BlueQuestionId}/answers", new SubmitAnswerRequest("5"));
        Team teamEntity = await factory.GetTeamByNameAsync("TeamHeic");
        await factory.SetNextAllowedAnswerAtAsync(teamEntity.Id, config.BlueQuestionId, DateTimeOffset.UtcNow.AddSeconds(-1));
        await captain.PostAsJsonAsync($"/api/questions/{config.BlueQuestionId}/answers", new SubmitAnswerRequest("4"));

        await captain.GetAsync($"/api/public/qr/{config.RedSlug}");
        await captain.PostAsJsonAsync($"/api/questions/{config.RedQuestionId}/answers", new SubmitAnswerRequest("ENIGMA"));

        await captain.PostAsJsonAsync(
            "/api/enigma/attempts",
            new SubmitEnigmaAttemptRequest(
                new Dictionary<Guid, int> { [config.BlueTagId] = 4, [config.RedTagId] = 7 }));

        using (MultipartFormDataContent form = CreateFinalPhotoFormHeic())
        {
            HttpResponseMessage response = await captain.PostAsync("/api/teams/me/final-task-photo", form);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            TeamSummaryResponse updated = (await response.Content.ReadFromJsonAsync<TeamSummaryResponse>())!;
            updated.FinalTaskPhotoUrl.Should().Contain(".heic");
        }
    }

    [Fact]
    public async Task FinalTaskPhoto_HeicWithOctetStreamMime_ShouldUseFileExtension()
    {
        await using QuestBackendApiFactory factory = new();
        await factory.InitializeAsync();
        await factory.ResetDatabaseAsync();

        SeededGameConfig config = await factory.SeedBasicConfigurationAsync();
        await factory.StartQuestDayAsync();

        HttpClient captain = factory.CreateCookieClient();
        await factory.RegisterParticipantAsync(captain, "cap-heic-octet", "Captain Heic Octet");
        await captain.PostAsJsonAsync("/api/teams", new CreateTeamRequest("TeamHeicOctet", "secret"));

        await captain.GetAsync($"/api/public/qr/{config.BlueSlug}");
        await captain.PostAsJsonAsync($"/api/questions/{config.BlueQuestionId}/answers", new SubmitAnswerRequest("5"));
        Team teamEntity = await factory.GetTeamByNameAsync("TeamHeicOctet");
        await factory.SetNextAllowedAnswerAtAsync(teamEntity.Id, config.BlueQuestionId, DateTimeOffset.UtcNow.AddSeconds(-1));
        await captain.PostAsJsonAsync($"/api/questions/{config.BlueQuestionId}/answers", new SubmitAnswerRequest("4"));

        await captain.GetAsync($"/api/public/qr/{config.RedSlug}");
        await captain.PostAsJsonAsync($"/api/questions/{config.RedQuestionId}/answers", new SubmitAnswerRequest("ENIGMA"));

        await captain.PostAsJsonAsync(
            "/api/enigma/attempts",
            new SubmitEnigmaAttemptRequest(
                new Dictionary<Guid, int> { [config.BlueTagId] = 4, [config.RedTagId] = 7 }));

        using (MultipartFormDataContent form = CreateFinalPhotoFormHeicOctetStream())
        {
            HttpResponseMessage response = await captain.PostAsync("/api/teams/me/final-task-photo", form);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            TeamSummaryResponse updated = (await response.Content.ReadFromJsonAsync<TeamSummaryResponse>())!;
            updated.FinalTaskPhotoUrl.Should().Contain(".heic");
        }
    }
}

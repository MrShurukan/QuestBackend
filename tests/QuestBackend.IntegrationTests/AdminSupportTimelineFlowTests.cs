using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using QuestBackend.Contracts;
using QuestBackend.IntegrationTests.Infrastructure;

namespace QuestBackend.IntegrationTests;

public sealed class AdminSupportTimelineFlowTests
{
    [Fact]
    public async Task AdminSupport_ShouldExposeQuestionMatrixAndRichTimeline()
    {
        await using QuestBackendApiFactory factory = new();
        await factory.InitializeAsync();
        await factory.ResetDatabaseAsync();

        SeededGameConfig config = await factory.SeedBasicConfigurationAsync(includeSecondBlueQuestion: true);
        await factory.StartQuestDayAsync();

        HttpClient adminClient = factory.CreateCookieClient();
        HttpClient captainClient = factory.CreateCookieClient();
        HttpClient memberClient = factory.CreateCookieClient();

        await factory.LoginAdminAsync(adminClient);
        await factory.RegisterParticipantAsync(captainClient, "support-captain", "Captain");
        await factory.RegisterParticipantAsync(memberClient, "support-member", "Member");

        HttpResponseMessage createTeamResponseMessage = await captainClient.PostAsJsonAsync("/api/teams", new CreateTeamRequest("Team Support", "secret"));
        createTeamResponseMessage.EnsureSuccessStatusCode();
        TeamSummaryResponse createdTeam = (await createTeamResponseMessage.Content.ReadFromJsonAsync<TeamSummaryResponse>())!;

        HttpResponseMessage joinTeamResponse = await memberClient.PostAsJsonAsync("/api/teams/join", new JoinTeamRequest(createdTeam.Id, "secret"));
        joinTeamResponse.EnsureSuccessStatusCode();

        ParticipantProfileResponse memberProfile = (await memberClient.GetFromJsonAsync<ParticipantProfileResponse>("/api/participant/auth/me"))!;
        Guid memberMembershipId = await factory.WithDbContextAsync(
            async db => await db.TeamMemberships
                .AsNoTracking()
                .Where(x => x.TeamId == createdTeam.Id && x.ParticipantUserId == memberProfile.Id)
                .Select(x => x.Id)
                .SingleAsync());

        await captainClient.GetAsync($"/api/public/qr/{config.BlueSlug}");
        HttpResponseMessage wrongBlueResponseMessage = await captainClient.PostAsJsonAsync(
            $"/api/questions/{config.BlueQuestionId}/answers",
            new SubmitAnswerRequest("5"));
        SubmitAnswerResponse wrongBlueResponse = (await wrongBlueResponseMessage.Content.ReadFromJsonAsync<SubmitAnswerResponse>())!;
        wrongBlueResponse.Result.Should().Be("wrong");

        await factory.SetNextAllowedAnswerAtAsync(createdTeam.Id, config.BlueQuestionId, DateTimeOffset.UtcNow.AddSeconds(-1));
        HttpResponseMessage correctBlueResponseMessage = await captainClient.PostAsJsonAsync(
            $"/api/questions/{config.BlueQuestionId}/answers",
            new SubmitAnswerRequest("4"));
        SubmitAnswerResponse correctBlueResponse = (await correctBlueResponseMessage.Content.ReadFromJsonAsync<SubmitAnswerResponse>())!;
        correctBlueResponse.Result.Should().Be("correct");

        await captainClient.GetAsync($"/api/public/qr/{config.RedSlug}");
        HttpResponseMessage correctRedResponseMessage = await captainClient.PostAsJsonAsync(
            $"/api/questions/{config.RedQuestionId}/answers",
            new SubmitAnswerRequest("ENIGMA"));
        SubmitAnswerResponse correctRedResponse = (await correctRedResponseMessage.Content.ReadFromJsonAsync<SubmitAnswerResponse>())!;
        correctRedResponse.Result.Should().Be("correct");

        HttpResponseMessage wrongEnigmaResponseMessage = await captainClient.PostAsJsonAsync(
            "/api/enigma/attempts",
            new SubmitEnigmaAttemptRequest(
                new Dictionary<Guid, int>
                {
                    [config.BlueTagId] = 1,
                    [config.RedTagId] = 1,
                }));
        wrongEnigmaResponseMessage.EnsureSuccessStatusCode();
        SubmitEnigmaAttemptResponse wrongEnigmaResponse = (await wrongEnigmaResponseMessage.Content.ReadFromJsonAsync<SubmitEnigmaAttemptResponse>())!;
        wrongEnigmaResponse.Result.Should().Be("failure");

        await factory.SetLatestEnigmaCooldownAsync(createdTeam.Id, config.EnigmaProfileId, DateTimeOffset.UtcNow.AddSeconds(-1));
        HttpResponseMessage correctEnigmaResponseMessage = await captainClient.PostAsJsonAsync(
            "/api/enigma/attempts",
            new SubmitEnigmaAttemptRequest(
                new Dictionary<Guid, int>
                {
                    [config.BlueTagId] = 4,
                    [config.RedTagId] = 7,
                }));
        correctEnigmaResponseMessage.EnsureSuccessStatusCode();
        SubmitEnigmaAttemptResponse correctEnigmaResponse = (await correctEnigmaResponseMessage.Content.ReadFromJsonAsync<SubmitEnigmaAttemptResponse>())!;
        correctEnigmaResponse.Result.Should().Be("success");

        HttpResponseMessage finalPhotoResponseMessage = await factory.PostImageAsync(
            captainClient,
            "/api/teams/me/final-task-photo",
            "photo",
            "final.png");
        finalPhotoResponseMessage.EnsureSuccessStatusCode();
        TeamSummaryResponse withFinalPhoto = (await finalPhotoResponseMessage.Content.ReadFromJsonAsync<TeamSummaryResponse>())!;
        withFinalPhoto.FinalTaskPhotoUrl.Should().StartWith("/uploads/team-final/");
        withFinalPhoto.FinalTaskPhotoUploadedAt.Should().NotBeNull();

        async Task<TeamSupportDetailsResponse> GetDetailsAsync()
        {
            HttpResponseMessage detailsResponse = await adminClient.GetAsync($"/api/admin/support/teams/{createdTeam.Id}");
            detailsResponse.EnsureSuccessStatusCode();
            return (await detailsResponse.Content.ReadFromJsonAsync<TeamSupportDetailsResponse>())!;
        }

        config.SecondBlueQuestionId.HasValue.Should().BeTrue();
        Guid secondBlueQuestionId = config.SecondBlueQuestionId!.Value;

        HttpResponseMessage unlockResponse = await adminClient.PostAsJsonAsync(
            $"/api/admin/support/teams/{createdTeam.Id}/questions/{secondBlueQuestionId}/unlock",
            new TeamQuestionAdjustmentRequest("manual unlock"));
        unlockResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        TeamSupportDetailsResponse detailsAfterUnlock = await GetDetailsAsync();
        detailsAfterUnlock.Questions.Should().Contain(x => x.Id == secondBlueQuestionId && x.State == "open" && !x.IsSolved);

        HttpResponseMessage solveResponse = await adminClient.PostAsJsonAsync(
            $"/api/admin/support/teams/{createdTeam.Id}/questions/{secondBlueQuestionId}/solve",
            new TeamQuestionAdjustmentRequest("manual solve"));
        solveResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        TeamSupportDetailsResponse detailsAfterSolve = await GetDetailsAsync();
        detailsAfterSolve.Questions.Should().Contain(x => x.Id == secondBlueQuestionId && x.State == "solved" && x.IsSolved);

        HttpResponseMessage unsolveResponse = await adminClient.PostAsJsonAsync(
            $"/api/admin/support/teams/{createdTeam.Id}/questions/{secondBlueQuestionId}/unsolve",
            new TeamQuestionAdjustmentRequest("manual unsolve"));
        unsolveResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        TeamSupportDetailsResponse detailsAfterUnsolve = await GetDetailsAsync();
        detailsAfterUnsolve.Questions.Should().Contain(x => x.Id == secondBlueQuestionId && x.State == "open" && !x.IsSolved);

        HttpResponseMessage closeResponse = await adminClient.PostAsJsonAsync(
            $"/api/admin/support/teams/{createdTeam.Id}/questions/{secondBlueQuestionId}/close",
            new TeamQuestionAdjustmentRequest("manual close"));
        closeResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        TeamSupportDetailsResponse detailsAfterClose = await GetDetailsAsync();
        detailsAfterClose.Questions.Should().NotContain(x => x.Id == secondBlueQuestionId);

        HttpResponseMessage removeMemberResponse = await adminClient.PostAsJsonAsync(
            $"/api/admin/support/teams/{createdTeam.Id}/members/{memberMembershipId}/remove",
            new TeamMemberRemovalRequest("cleanup"));
        removeMemberResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        TeamSupportDetailsResponse finalDetails = await GetDetailsAsync();
        finalDetails.Questions.Should().Contain(x => x.Id == config.BlueQuestionId && x.State == "solved" && x.IsSolved);
        finalDetails.Questions.Should().Contain(x => x.Id == config.RedQuestionId && x.State == "solved" && x.IsSolved);
        finalDetails.Questions.Should().NotContain(x => x.Id == secondBlueQuestionId);

        finalDetails.Timeline.Should().Contain(x => x.Kind == "team-created" && x.Title == "Команда создана");
        finalDetails.Timeline.Count(x => x.Kind == "member-joined").Should().BeGreaterThanOrEqualTo(2);
        finalDetails.Timeline.Should().Contain(x => x.Kind == "question-opened" && x.Description.Contains("Blue question"));
        finalDetails.Timeline.Should().Contain(x => x.Kind == "question-opened" && x.Description.Contains("Red question"));
        finalDetails.Timeline.Should().Contain(x => x.Kind == "question-attempt" && x.Title == "Неверный ответ");
        finalDetails.Timeline.Should().Contain(x => x.Kind == "question-solved" && x.Description.Contains("Blue question"));
        finalDetails.Timeline.Should().Contain(x => x.Kind == "question-solved" && x.Description.Contains("Red question"));
        finalDetails.Timeline.Should().Contain(x => x.Kind == "enigma-attempt" && x.Title == "Неуспешная попытка Enigma");
        finalDetails.Timeline.Should().Contain(x => x.Kind == "enigma-solved" && x.Title == "Успешная попытка Enigma");
        finalDetails.Timeline.Should().Contain(x => x.Kind == "final-photo-uploaded" && x.Title == "Загружено финальное фото");
        finalDetails.Timeline.Should().Contain(x => x.Kind == "member-removed" && x.Reason == "cleanup");
        finalDetails.Timeline.Should().Contain(x => x.Kind == "support-action" && x.Title == "Администратор открыл вопрос" && x.Reason == "manual unlock");
        finalDetails.Timeline.Should().Contain(x => x.Kind == "support-action" && x.Title == "Администратор засчитал решение" && x.Reason == "manual solve");
        finalDetails.Timeline.Should().Contain(x => x.Kind == "support-action" && x.Title == "Администратор отозвал решение" && x.Reason == "manual unsolve");
        finalDetails.Timeline.Should().Contain(x => x.Kind == "support-action" && x.Title == "Администратор закрыл вопрос" && x.Reason == "manual close");
        finalDetails.Timeline.Should().Contain(x => x.Kind == "support-action" && x.Title == "Администратор исключил участника" && x.Reason == "cleanup");
    }
}

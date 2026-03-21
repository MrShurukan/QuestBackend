using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using QuestBackend.Contracts;
using QuestBackend.IntegrationTests.Infrastructure;
namespace QuestBackend.IntegrationTests;

public sealed class AdminRoutingAndSupportFlowTests
{
    [Fact]
    public async Task AdminEndpoints_ShouldRotateOverrideAndSupportCorrectTeamState()
    {
        await using QuestBackendApiFactory factory = new();
        await factory.InitializeAsync();
        await factory.ResetDatabaseAsync();

        SeededGameConfig config = await factory.SeedBasicConfigurationAsync(includeSecondBlueQuestion: true);
        await factory.StartQuestDayAsync();

        HttpClient adminClient = factory.CreateCookieClient();
        HttpClient participantClient = factory.CreateCookieClient();

        await factory.LoginAdminAsync(adminClient);
        await factory.RegisterParticipantAsync(participantClient, "player-1", "Player");
        await participantClient.PostAsJsonAsync("/api/teams", new CreateTeamRequest("TeamOps", "secret"));

        HttpResponseMessage previewBeforeResponse = await adminClient.GetAsync("/api/admin/routing/preview");
        List<RoutingPreviewRowResponse> previewBefore = (await previewBeforeResponse.Content.ReadFromJsonAsync<List<RoutingPreviewRowResponse>>())!;
        previewBefore.Should().Contain(x => x.QrSlug == config.BlueSlug && x.QuestionId == config.BlueQuestionId);

        HttpResponseMessage rotateResponse = await adminClient.PostAsync($"/api/admin/routing/tags/{config.BlueTagId}/rotate?step=1", null);
        rotateResponse.EnsureSuccessStatusCode();

        HttpResponseMessage previewAfterRotateResponse = await adminClient.GetAsync("/api/admin/routing/preview");
        List<RoutingPreviewRowResponse> previewAfterRotate = (await previewAfterRotateResponse.Content.ReadFromJsonAsync<List<RoutingPreviewRowResponse>>())!;
        previewAfterRotate.Should().Contain(x => x.QrSlug == config.BlueSlug && x.QuestionId == config.SecondBlueQuestionId);

        HttpResponseMessage overrideResponse = await adminClient.PostAsJsonAsync(
            "/api/admin/routing/overrides",
            new QrBindingOverrideRequest(config.BlueQrId, config.BlueQuestionId, config.RoutingProfileId, true, "force old question"));
        overrideResponse.EnsureSuccessStatusCode();

        HttpResponseMessage previewAfterOverrideResponse = await adminClient.GetAsync("/api/admin/routing/preview");
        List<RoutingPreviewRowResponse> previewAfterOverride = (await previewAfterOverrideResponse.Content.ReadFromJsonAsync<List<RoutingPreviewRowResponse>>())!;
        previewAfterOverride.Should().Contain(x => x.QrSlug == config.BlueSlug && x.QuestionId == config.BlueQuestionId && x.ResolutionMode.Contains("Override", StringComparison.OrdinalIgnoreCase));

        await participantClient.GetAsync($"/api/public/qr/{config.BlueSlug}");
        var team = await factory.GetTeamByNameAsync("TeamOps");

        HttpResponseMessage solveResponse = await adminClient.PostAsJsonAsync(
            $"/api/admin/support/teams/{team.Id}/questions/{config.BlueQuestionId}/solve",
            new TeamQuestionAdjustmentRequest("manual correction"));
        solveResponse.EnsureSuccessStatusCode();

        HttpResponseMessage detailsResponse = await adminClient.GetAsync($"/api/admin/support/teams/{team.Id}");
        TeamSupportDetailsResponse details = (await detailsResponse.Content.ReadFromJsonAsync<TeamSupportDetailsResponse>())!;
        details.Questions.Should().Contain(x => x.Id == config.BlueQuestionId && x.IsSolved);
    }

    [Fact]
    public async Task Admin_ShouldResetLocalParticipantPassword()
    {
        await using QuestBackendApiFactory factory = new();
        await factory.InitializeAsync();
        await factory.ResetDatabaseAsync();

        HttpClient adminClient = factory.CreateCookieClient();
        HttpClient participantClient = factory.CreateCookieClient();

        await factory.LoginAdminAsync(adminClient);
        await factory.RegisterParticipantAsync(participantClient, "reset-me", "Reset User", "Oldpass12");

        ParticipantProfileResponse? me = await participantClient.GetFromJsonAsync<ParticipantProfileResponse>("/api/participant/auth/me");
        me.Should().NotBeNull();

        HttpResponseMessage resetResponse = await adminClient.PostAsJsonAsync(
            $"/api/admin/support/participants/{me!.Id}/password",
            new ParticipantPasswordResetRequest("Newpass12", "support reset"));
        resetResponse.EnsureSuccessStatusCode();

        await participantClient.PostAsync("/api/participant/auth/logout", null);

        HttpResponseMessage oldLogin = await participantClient.PostAsJsonAsync(
            "/api/participant/auth/login",
            new ParticipantLoginRequest("reset-me", "Oldpass12"));
        oldLogin.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        HttpResponseMessage newLogin = await participantClient.PostAsJsonAsync(
            "/api/participant/auth/login",
            new ParticipantLoginRequest("reset-me", "Newpass12"));
        newLogin.EnsureSuccessStatusCode();
    }
}

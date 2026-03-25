using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using QuestBackend.Contracts;
using QuestBackend.IntegrationTests.Infrastructure;

namespace QuestBackend.IntegrationTests;

public sealed class AuthAndTeamsFlowTests
{
    [Fact]
    public async Task ParticipantClients_ShouldCreateAndJoinTeams()
    {
        await using QuestBackendApiFactory factory = new();
        await factory.InitializeAsync();
        await factory.ResetDatabaseAsync();

        HttpClient captainClient = factory.CreateCookieClient();
        HttpClient memberClient = factory.CreateCookieClient();

        await factory.RegisterParticipantAsync(captainClient, "captain-1", "Captain");
        await factory.RegisterParticipantAsync(memberClient, "member-1", "Member");

        HttpResponseMessage createResponse = await captainClient.PostAsJsonAsync("/api/teams", new CreateTeamRequest("Alpha", "secret"));
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        TeamSummaryResponse createdTeam = (await createResponse.Content.ReadFromJsonAsync<TeamSummaryResponse>())!;
        createdTeam.Name.Should().Be("Alpha");
        createdTeam.Members.Should().HaveCount(1);

        HttpResponseMessage availableTeamsResponse = await memberClient.GetAsync("/api/teams/available");
        availableTeamsResponse.EnsureSuccessStatusCode();
        IReadOnlyList<TeamSummaryResponse> teams = (await availableTeamsResponse.Content.ReadFromJsonAsync<List<TeamSummaryResponse>>())!;
        teams.Should().ContainSingle(x => x.Id == createdTeam.Id);

        HttpResponseMessage joinResponse = await memberClient.PostAsJsonAsync("/api/teams/join", new JoinTeamRequest(createdTeam.Id, "secret"));
        joinResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        HttpResponseMessage myTeamResponse = await memberClient.GetAsync("/api/teams/me");
        myTeamResponse.EnsureSuccessStatusCode();
        TeamSummaryResponse myTeam = (await myTeamResponse.Content.ReadFromJsonAsync<TeamSummaryResponse>())!;
        myTeam.Members.Should().HaveCount(2);
    }

    [Fact]
    public async Task JoinTeam_ShouldRejectWhenAtMaxMembersFromGlobalSettings()
    {
        await using QuestBackendApiFactory factory = new();
        await factory.InitializeAsync();
        await factory.ResetDatabaseAsync();

        HttpClient adminClient = factory.CreateCookieClient();
        await factory.LoginAdminAsync(adminClient);

        GlobalSettingsResponse current =
            (await adminClient.GetFromJsonAsync<GlobalSettingsResponse>("/api/admin/settings/global"))!;

        GlobalSettingsUpdateRequest capped = new(
            current.AnswerCooldownMinutes,
            current.EnigmaCooldownMinutes,
            2,
            current.DefaultAnswerNormalization,
            current.CurrentQuestDayStateId,
            current.CurrentRoutingProfileId,
            current.CurrentEnigmaProfileId,
            current.FlagsJson,
            current.Timezone);

        HttpResponseMessage settingsPut = await adminClient.PutAsJsonAsync("/api/admin/settings/global", capped);
        settingsPut.StatusCode.Should().Be(HttpStatusCode.OK);

        HttpClient captainClient = factory.CreateCookieClient();
        HttpClient memberClient = factory.CreateCookieClient();
        HttpClient extraClient = factory.CreateCookieClient();

        await factory.RegisterParticipantAsync(captainClient, "cap-full", "Captain");
        await factory.RegisterParticipantAsync(memberClient, "mem-full", "Member");
        await factory.RegisterParticipantAsync(extraClient, "extra-full", "Extra");

        HttpResponseMessage createResponse = await captainClient.PostAsJsonAsync("/api/teams", new CreateTeamRequest("FullTeam", "secret"));
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        TeamSummaryResponse team = (await createResponse.Content.ReadFromJsonAsync<TeamSummaryResponse>())!;

        HttpResponseMessage firstJoin = await memberClient.PostAsJsonAsync("/api/teams/join", new JoinTeamRequest(team.Id, "secret"));
        firstJoin.StatusCode.Should().Be(HttpStatusCode.OK);

        HttpResponseMessage secondJoin = await extraClient.PostAsJsonAsync("/api/teams/join", new JoinTeamRequest(team.Id, "secret"));
        secondJoin.StatusCode.Should().Be(HttpStatusCode.Conflict);

        HttpResponseMessage availableWhenFull = await extraClient.GetAsync("/api/teams/available");
        availableWhenFull.EnsureSuccessStatusCode();
        List<TeamSummaryResponse> availableTeams = (await availableWhenFull.Content.ReadFromJsonAsync<List<TeamSummaryResponse>>())!;
        availableTeams.Should().NotContain(x => x.Id == team.Id);
    }

    [Fact]
    public async Task Captain_ShouldSeeJoinSecret_OnMyTeam_AndMemberShouldNot_AndCaptainMayUpdateSecret()
    {
        await using QuestBackendApiFactory factory = new();
        await factory.InitializeAsync();
        await factory.ResetDatabaseAsync();

        HttpClient captainClient = factory.CreateCookieClient();
        HttpClient memberClient = factory.CreateCookieClient();
        await factory.RegisterParticipantAsync(captainClient, "cap-sec", "Cap");
        await factory.RegisterParticipantAsync(memberClient, "mem-sec", "Mem");

        HttpResponseMessage createResponse =
            await captainClient.PostAsJsonAsync("/api/teams", new CreateTeamRequest("SecTeam", "alpha"));
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        TeamSummaryResponse created = (await createResponse.Content.ReadFromJsonAsync<TeamSummaryResponse>())!;
        created.JoinSecretForCaptain.Should().Be("alpha");

        HttpResponseMessage captainMe = await captainClient.GetAsync("/api/teams/me");
        captainMe.EnsureSuccessStatusCode();
        TeamSummaryResponse capTeam = (await captainMe.Content.ReadFromJsonAsync<TeamSummaryResponse>())!;
        capTeam.JoinSecretForCaptain.Should().Be("alpha");

        HttpResponseMessage joinOk = await memberClient.PostAsJsonAsync("/api/teams/join", new JoinTeamRequest(created.Id, "alpha"));
        joinOk.StatusCode.Should().Be(HttpStatusCode.OK);

        HttpResponseMessage memberMe = await memberClient.GetAsync("/api/teams/me");
        memberMe.EnsureSuccessStatusCode();
        TeamSummaryResponse memTeam = (await memberMe.Content.ReadFromJsonAsync<TeamSummaryResponse>())!;
        memTeam.JoinSecretForCaptain.Should().BeNull();

        HttpResponseMessage put = await captainClient.PutAsJsonAsync(
            "/api/teams/me/join-secret",
            new UpdateTeamJoinSecretRequest("beta"));
        put.StatusCode.Should().Be(HttpStatusCode.OK);
        TeamSummaryResponse updated = (await put.Content.ReadFromJsonAsync<TeamSummaryResponse>())!;
        updated.JoinSecretForCaptain.Should().Be("beta");

        HttpClient thirdClient = factory.CreateCookieClient();
        await factory.RegisterParticipantAsync(thirdClient, "third-sec", "Third");
        HttpResponseMessage failJoin = await thirdClient.PostAsJsonAsync("/api/teams/join", new JoinTeamRequest(created.Id, "alpha"));
        failJoin.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        HttpResponseMessage okJoin = await thirdClient.PostAsJsonAsync("/api/teams/join", new JoinTeamRequest(created.Id, "beta"));
        okJoin.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

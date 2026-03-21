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
    }
}

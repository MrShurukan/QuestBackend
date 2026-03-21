using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using QuestBackend.Contracts;
using QuestBackend.IntegrationTests.Infrastructure;

namespace QuestBackend.IntegrationTests;

public sealed class ParticipantAuthSessionFlowTests
{
    [Fact]
    public async Task Participant_Logout_ShouldDropSession_Login_ShouldRestore()
    {
        await using QuestBackendApiFactory factory = new();
        await factory.InitializeAsync();
        await factory.ResetDatabaseAsync();

        const string login = "session-user";
        const string password = "Testpass12";

        HttpClient client = factory.CreateCookieClient();
        await factory.RegisterParticipantAsync(client, login, "Session User", password);

        HttpResponseMessage meAuthed = await client.GetAsync("/api/participant/auth/me");
        meAuthed.StatusCode.Should().Be(HttpStatusCode.OK);

        HttpResponseMessage logout = await client.PostAsync("/api/participant/auth/logout", null);
        logout.StatusCode.Should().Be(HttpStatusCode.NoContent);

        HttpResponseMessage meAfterLogout = await client.GetAsync("/api/participant/auth/me");
        meAfterLogout.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        HttpResponseMessage loginResponse = await client.PostAsJsonAsync(
            "/api/participant/auth/login",
            new ParticipantLoginRequest(login, password));
        loginResponse.EnsureSuccessStatusCode();

        HttpResponseMessage meAgain = await client.GetAsync("/api/participant/auth/me");
        meAgain.StatusCode.Should().Be(HttpStatusCode.OK);
        ParticipantProfileResponse profile = (await meAgain.Content.ReadFromJsonAsync<ParticipantProfileResponse>())!;
        profile.DisplayName.Should().Be("Session User");
    }
}

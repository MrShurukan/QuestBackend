using System.Net.Http.Json;
using FluentAssertions;
using QuestBackend.Contracts;
using QuestBackend.IntegrationTests.Infrastructure;

namespace QuestBackend.IntegrationTests;

public sealed class AdminReadEndpointsFlowTests
{
    [Fact]
    public async Task Admin_ShouldReadAuditSupportTeamsAndTags()
    {
        await using QuestBackendApiFactory factory = new();
        await factory.InitializeAsync();
        await factory.ResetDatabaseAsync();

        HttpClient adminClient = factory.CreateCookieClient();
        await factory.LoginAdminAsync(adminClient);

        HttpResponseMessage auditResponse = await adminClient.GetAsync("/api/admin/audit?take=50");
        auditResponse.EnsureSuccessStatusCode();
        List<AuditEntryResponse> audit = (await auditResponse.Content.ReadFromJsonAsync<List<AuditEntryResponse>>())!;
        audit.Should().NotBeNull();

        HttpResponseMessage teamsResponse = await adminClient.GetAsync("/api/admin/support/teams");
        teamsResponse.EnsureSuccessStatusCode();
        List<TeamSummaryResponse> teams = (await teamsResponse.Content.ReadFromJsonAsync<List<TeamSummaryResponse>>())!;
        teams.Should().NotBeNull();

        HttpResponseMessage tagsResponse = await adminClient.GetAsync("/api/admin/tags");
        tagsResponse.EnsureSuccessStatusCode();
        List<TagResponse> tags = (await tagsResponse.Content.ReadFromJsonAsync<List<TagResponse>>())!;
        tags.Should().NotBeNull();
    }
}

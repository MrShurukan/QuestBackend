using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using QuestBackend.Contracts;
using QuestBackend.IntegrationTests.Infrastructure;

namespace QuestBackend.IntegrationTests;

public sealed class AdminAccountFlowTests
{
    [Fact]
    public async Task Admin_ShouldChangeOwnPassword_AndLoginWithNewOne()
    {
        await using QuestBackendApiFactory factory = new();
        await factory.InitializeAsync();
        await factory.ResetDatabaseAsync();

        HttpClient client = factory.CreateCookieClient();
        await factory.LoginAdminAsync(client);

        HttpResponseMessage update = await client.PutAsJsonAsync(
            "/api/admin/auth/profile",
            new AdminSelfProfileUpdateRequest("admin123", null, "NewAdminPw12"));
        update.EnsureSuccessStatusCode();

        await client.PostAsync("/api/admin/auth/logout", null);

        HttpResponseMessage oldLogin = await client.PostAsJsonAsync(
            "/api/admin/auth/login",
            new AdminLoginRequest(factory.AdminLogin, "admin123"));
        oldLogin.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        HttpResponseMessage newLogin = await client.PostAsJsonAsync(
            "/api/admin/auth/login",
            new AdminLoginRequest(factory.AdminLogin, "NewAdminPw12"));
        newLogin.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task SuperAdmin_ShouldCreateAnotherAdmin_AndTheyCanLogin()
    {
        await using QuestBackendApiFactory factory = new();
        await factory.InitializeAsync();
        await factory.ResetDatabaseAsync();

        HttpClient superClient = factory.CreateCookieClient();
        await factory.LoginAdminAsync(superClient);

        HttpResponseMessage create = await superClient.PostAsJsonAsync(
            "/api/admin/users",
            new AdminUserCreateRequest("sub-admin", "SubAdminPw12", "Editor"));
        create.EnsureSuccessStatusCode();

        HttpClient editorClient = factory.CreateCookieClient();
        HttpResponseMessage login = await editorClient.PostAsJsonAsync(
            "/api/admin/auth/login",
            new AdminLoginRequest("sub-admin", "SubAdminPw12"));
        login.EnsureSuccessStatusCode();
        AuthenticatedAdminResponse me = (await login.Content.ReadFromJsonAsync<AuthenticatedAdminResponse>())!;
        me.Role.Should().Be("Editor");
    }
}

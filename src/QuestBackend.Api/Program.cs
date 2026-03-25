using System.Net;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using QuestBackend.Api.Common;
using QuestBackend.Api.Modules.Admin;
using QuestBackend.Domain.Admin;
using QuestBackend.Api.Modules.Enigma;
using QuestBackend.Api.Modules.Participants;
using QuestBackend.Api.Modules.Public;
using QuestBackend.Api.Modules.QuestDay;
using QuestBackend.Api.Modules.Questions;
using QuestBackend.Api.Modules.Teams;
using QuestBackend.Application.Shared;
using QuestBackend.Infrastructure;
using QuestBackend.Infrastructure.Initialization;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // Nginx на том же хосте подключается к Kestrel с loopback — доверяем только ему.
    options.KnownProxies.Add(IPAddress.Loopback);
    options.KnownProxies.Add(IPAddress.IPv6Loopback);
});

builder.Services.AddOpenApi();
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database");
builder.Services.AddQuestRateLimiting();

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 25 * 1024 * 1024;
});

builder.Services.AddQuestBackendInfrastructure(builder.Configuration);

builder.Services
    .AddAuthentication(
        options =>
        {
            options.DefaultScheme = "DynamicCookie";
            options.DefaultAuthenticateScheme = "DynamicCookie";
            options.DefaultChallengeScheme = QuestAuthConstants.ParticipantScheme;
        })
    .AddPolicyScheme(
        "DynamicCookie",
        "Dynamic cookie authentication",
        options =>
        {
            options.ForwardDefaultSelector = context =>
            {
                if (context.Request.Cookies.ContainsKey("questbackend.admin"))
                {
                    return QuestAuthConstants.AdminScheme;
                }

                if (context.Request.Cookies.ContainsKey("questbackend.participant"))
                {
                    return QuestAuthConstants.ParticipantScheme;
                }

                return QuestAuthConstants.ParticipantScheme;
            };
        })
    .AddCookie(
        QuestAuthConstants.AdminScheme,
        options =>
        {
            options.Cookie.Name = "questbackend.admin";
            ConfigureApiCookie(options);
        })
    .AddCookie(
        QuestAuthConstants.ParticipantScheme,
        options =>
        {
            options.Cookie.Name = "questbackend.participant";
            ConfigureApiCookie(options);
        });

builder.Services.AddAuthorization(
    options =>
    {
        options.AddPolicy(
            ApiPolicies.AdminOnly,
            policy =>
            {
                policy.AddAuthenticationSchemes(QuestAuthConstants.AdminScheme);
                policy.RequireAuthenticatedUser();
            });

        options.AddPolicy(
            ApiPolicies.AdminSuperOnly,
            policy =>
            {
                policy.AddAuthenticationSchemes(QuestAuthConstants.AdminScheme);
                policy.RequireAuthenticatedUser();
                policy.RequireRole(nameof(AdminRole.SuperAdmin));
            });

        options.AddPolicy(
            ApiPolicies.ParticipantOnly,
            policy =>
            {
                policy.AddAuthenticationSchemes(QuestAuthConstants.ParticipantScheme);
                policy.RequireAuthenticatedUser();
            });
    });

WebApplication app = builder.Build();

app.UseForwardedHeaders();

string webRoot = app.Environment.WebRootPath ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot");
Directory.CreateDirectory(Path.Combine(webRoot, "uploads", "avatars"));

using (IServiceScope scope = app.Services.CreateScope())
{
    AppInitializer initializer = scope.ServiceProvider.GetRequiredService<AppInitializer>();
    await initializer.InitializeAsync();
}

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseRateLimiter();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapHealthChecks("/health/live", new HealthCheckOptions());
app.MapHealthChecks("/health/ready", new HealthCheckOptions());
app.MapGet("/", () => Results.Ok(new { status = "ok", service = "QuestBackend.Api" }));

app.MapPublicQrEndpoints();
app.MapParticipantAuthEndpoints();
app.MapTeamEndpoints();
app.MapQuestDayPublicEndpoints();
app.MapQuestionEndpoints();
app.MapEnigmaEndpoints();

app.MapAdminAuthEndpoints();
app.MapAdminUsersEndpoints();
app.MapAdminTagEndpoints();
app.MapAdminQuestionEndpoints();
app.MapAdminPoolEndpoints();
app.MapAdminQrEndpoints();
app.MapAdminRoutingEndpoints();
app.MapAdminEnigmaEndpoints();
app.MapAdminQuestDayLifecycleEndpoints();
app.MapAdminSettingsEndpoints();
app.MapAdminSupportEndpoints();
app.MapAdminAuditEndpoints();

app.Run();

static void ConfigureApiCookie(CookieAuthenticationOptions options)
{
    options.SlidingExpiration = true;
    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    };

    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    };
}

public partial class Program;

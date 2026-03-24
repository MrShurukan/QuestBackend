using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using QuestBackend.Application.Abstractions;
using QuestBackend.Contracts;
using QuestBackend.Domain.Config;
using QuestBackend.Domain.Enigma;
using QuestBackend.Domain.Progress;
using QuestBackend.Domain.Questions;
using QuestBackend.Domain.Routing;
using QuestBackend.Domain.Tags;
using QuestBackend.Domain.Teams;
using QuestBackend.Infrastructure.Initialization;
using QuestBackend.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace QuestBackend.IntegrationTests.Infrastructure;

/// <summary>
/// Integration host against a dedicated Postgres instance (Testcontainers).
/// Uses environment <c>Testing</c> and replaces <see cref="QuestDbContext"/> registration so the app never uses
/// the developer connection string from appsettings or <c>ConnectionStrings__QuestDatabase</c> from the shell.
/// </summary>
public sealed class QuestBackendApiFactory : WebApplicationFactory<Program>
{
    private readonly string _adminLogin = $"admin-{Guid.NewGuid():N}";
    private readonly string _adminPassword = "admin123";

    public string AdminLogin => _adminLogin;
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17")
        .WithImage("postgres:17")
        .WithDatabase($"quest_backend_tests_{Guid.NewGuid():N}")
        .WithUsername("quest_backend")
        .WithPassword("quest_backend")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Avoid Development: user secrets / env vars can override connection string after in-memory config.
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration(
            (_, config) =>
            {
                Dictionary<string, string?> settings = new()
                {
                    ["ConnectionStrings:QuestDatabase"] = _postgres.GetConnectionString(),
                    ["Bootstrap:Admin:Login"] = _adminLogin,
                    ["Bootstrap:Admin:Password"] = _adminPassword,
                    ["Bootstrap:SeedSampleData"] = "false",
                };

                config.AddInMemoryCollection(settings);
            });

        // RegisterOptions from Program runs with IConfiguration; AddDbContext captures connection string once.
        // Environment variables (e.g. ConnectionStrings__QuestDatabase) can still win over in-memory providers.
        // Re-bind EF to the container connection string after all host configuration runs.
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<QuestDbContext>>();
            services.RemoveAll<QuestDbContext>();
            services.RemoveAll<IQuestDbContext>();

            string connectionString = _postgres.GetConnectionString();
            services.AddDbContext<QuestDbContext>(options => options.UseNpgsql(connectionString));
            services.AddScoped<IQuestDbContext>(sp => sp.GetRequiredService<QuestDbContext>());
        });
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    public HttpClient CreateCookieClient()
    {
        return CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                HandleCookies = true,
            });
    }

    public async Task ResetDatabaseAsync()
    {
        using IServiceScope scope = Services.CreateScope();
        QuestDbContext dbContext = scope.ServiceProvider.GetRequiredService<QuestDbContext>();

        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.MigrateAsync();

        AppInitializer initializer = scope.ServiceProvider.GetRequiredService<AppInitializer>();
        await initializer.InitializeAsync();
    }

    public async Task<SeededGameConfig> SeedBasicConfigurationAsync(bool includeSecondBlueQuestion = false)
    {
        using IServiceScope scope = Services.CreateScope();
        QuestDbContext dbContext = scope.ServiceProvider.GetRequiredService<QuestDbContext>();

        QuestionTag blueTag = new() { Code = "blue", Name = "Blue", Color = "#2563EB", IsActive = true, SortOrder = 1 };
        QuestionTag redTag = new() { Code = "red", Name = "Red", Color = "#DC2626", IsActive = true, SortOrder = 2 };

        Question blueQuestion = new()
        {
            Tag = blueTag,
            Title = "Blue question",
            BodyRichText = "What is 2 + 2?",
            FooterHint = "Blue hint 4",
            Status = QuestionStatus.Active,
            IsActive = true,
            AnswerSchema = new AnswerSchema
            {
                Kind = AnswerValidationKind.Numeric,
                ExpectedNumericValue = 4,
                NumericTolerance = 0,
            },
        };

        Question? secondBlueQuestion = includeSecondBlueQuestion
            ? new Question
            {
                Tag = blueTag,
                Title = "Blue question 2",
                BodyRichText = "What is 3 + 3?",
                FooterHint = "Blue hint 6",
                Status = QuestionStatus.Active,
                IsActive = true,
                AnswerSchema = new AnswerSchema
                {
                    Kind = AnswerValidationKind.Numeric,
                    ExpectedNumericValue = 6,
                    NumericTolerance = 0,
                },
            }
            : null;

        Question redQuestion = new()
        {
            Tag = redTag,
            Title = "Red question",
            BodyRichText = "Type ENIGMA",
            FooterHint = "Red hint 7",
            Status = QuestionStatus.Active,
            IsActive = true,
            AnswerSchema = new AnswerSchema
            {
                Kind = AnswerValidationKind.NormalizedText,
                AcceptedAnswers = ["ENIGMA"],
            },
        };

        QuestionPool bluePool = new() { Tag = blueTag, Name = "Blue pool", IsActive = true, SortOrder = 1 };
        bluePool.Entries.Add(new QuestionPoolEntry { Question = blueQuestion, Position = 0, IsEnabled = true });
        if (secondBlueQuestion is not null)
        {
            bluePool.Entries.Add(new QuestionPoolEntry { Question = secondBlueQuestion, Position = 1, IsEnabled = true });
        }

        QuestionPool redPool = new() { Tag = redTag, Name = "Red pool", IsActive = true, SortOrder = 1 };
        redPool.Entries.Add(new QuestionPoolEntry { Question = redQuestion, Position = 0, IsEnabled = true });

        QrCode blueQr = new() { Tag = blueTag, Slug = "blue0001", Label = "Blue QR", SlotIndex = 0, IsActive = true };
        QrCode redQr = new() { Tag = redTag, Slug = "red00001", Label = "Red QR", SlotIndex = 0, IsActive = true };

        RoutingProfile routingProfile = new() { Name = "Default routing", IsActive = true };
        routingProfile.TagStates.Add(new RoutingProfileTagState { Tag = blueTag, ActivePool = bluePool, RotationOffset = 0, IsEnabled = true });
        routingProfile.TagStates.Add(new RoutingProfileTagState { Tag = redTag, ActivePool = redPool, RotationOffset = 0, IsEnabled = true });

        EnigmaProfile enigmaProfile = new()
        {
            Name = "Default enigma",
            Mode = EnigmaMode.SimpleCombination,
            IsActive = true,
            AttemptCooldownMinutes = 5,
            SuccessMessage = "success",
            FailureMessage = "failure",
            SecretCombinationJson = includeSecondBlueQuestion
                ? "{\"" + blueTag.Id + "\":4,\"" + redTag.Id + "\":7}"
                : "{\"" + blueTag.Id + "\":4,\"" + redTag.Id + "\":7}",
        };
        enigmaProfile.RotorDefinitions.Add(new EnigmaRotorDefinition { Tag = blueTag, Label = "Blue rotor", DisplayOrder = 1, PositionMin = 1, PositionMax = 9, IsActive = true });
        enigmaProfile.RotorDefinitions.Add(new EnigmaRotorDefinition { Tag = redTag, Label = "Red rotor", DisplayOrder = 2, PositionMin = 1, PositionMax = 9, IsActive = true });

        GlobalSettings settings = await dbContext.GlobalSettings.FirstAsync();
        settings.CurrentRoutingProfileId = routingProfile.Id;
        settings.CurrentEnigmaProfileId = enigmaProfile.Id;
        settings.AnswerCooldownMinutes = 5;
        settings.EnigmaCooldownMinutes = 5;

        await dbContext.AddRangeAsync(blueTag, redTag, blueQuestion, redQuestion, bluePool, redPool, blueQr, redQr, routingProfile, enigmaProfile);
        if (secondBlueQuestion is not null)
        {
            await dbContext.AddAsync(secondBlueQuestion);
        }

        await dbContext.SaveChangesAsync();

        return new SeededGameConfig(
            blueTag.Id,
            redTag.Id,
            blueQuestion.Id,
            redQuestion.Id,
            secondBlueQuestion?.Id,
            blueQr.Id,
            redQr.Id,
            "blue0001",
            "red00001",
            routingProfile.Id,
            enigmaProfile.Id);
    }

    public async Task StartQuestDayAsync()
    {
        using IServiceScope scope = Services.CreateScope();
        QuestDbContext dbContext = scope.ServiceProvider.GetRequiredService<QuestDbContext>();

        var state = await dbContext.QuestDayStates.FirstAsync();
        state.Status = Domain.QuestDay.QuestDayStatus.Running;
        state.StartedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync();
    }

    public async Task SetNextAllowedAnswerAtAsync(Guid teamId, Guid questionId, DateTimeOffset? value)
    {
        using IServiceScope scope = Services.CreateScope();
        QuestDbContext dbContext = scope.ServiceProvider.GetRequiredService<QuestDbContext>();
        TeamQuestionState state = await dbContext.TeamQuestionStates.SingleAsync(x => x.TeamId == teamId && x.QuestionId == questionId);
        state.NextAllowedAnswerAt = value;
        await dbContext.SaveChangesAsync();
    }

    public async Task<Team> GetTeamByNameAsync(string name)
    {
        using IServiceScope scope = Services.CreateScope();
        QuestDbContext dbContext = scope.ServiceProvider.GetRequiredService<QuestDbContext>();
        return await dbContext.Teams.SingleAsync(x => x.Name == name);
    }

    public async Task LoginAdminAsync(HttpClient client)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/admin/auth/login", new AdminLoginRequest(_adminLogin, _adminPassword));
        response.EnsureSuccessStatusCode();
    }

    public async Task RegisterParticipantAsync(HttpClient client, string login, string displayName, string password = "Testpass12")
    {
        using MultipartFormDataContent content = new();
        content.Add(new StringContent(login), "login");
        content.Add(new StringContent(displayName), "displayName");
        content.Add(new StringContent(password), "password");
        HttpResponseMessage response = await client.PostAsync("/api/participant/auth/register", content);
        response.EnsureSuccessStatusCode();
    }
}

public sealed record SeededGameConfig(
    Guid BlueTagId,
    Guid RedTagId,
    Guid BlueQuestionId,
    Guid RedQuestionId,
    Guid? SecondBlueQuestionId,
    Guid BlueQrId,
    Guid RedQrId,
    string BlueSlug,
    string RedSlug,
    Guid RoutingProfileId,
    Guid EnigmaProfileId);

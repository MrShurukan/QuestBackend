using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QuestBackend.Application.Abstractions;
using QuestBackend.Application.Admin;
using QuestBackend.Application.Audit;
using QuestBackend.Application.Enigma;
using QuestBackend.Application.Participants;
using QuestBackend.Application.QuestDay;
using QuestBackend.Application.Questions;
using QuestBackend.Application.Routing;
using QuestBackend.Application.Support;
using QuestBackend.Application.Teams;
using QuestBackend.Domain.Shared;
using QuestBackend.Infrastructure.Audit;
using QuestBackend.Infrastructure.Auth;
using QuestBackend.Infrastructure.Http;
using QuestBackend.Infrastructure.Identifiers;
using QuestBackend.Infrastructure.Initialization;
using QuestBackend.Infrastructure.Persistence;
using QuestBackend.Infrastructure.Security;
using QuestBackend.Infrastructure.Time;

namespace QuestBackend.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddQuestBackendInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        string connectionString =
            configuration.GetConnectionString("QuestDatabase")
            ?? "Host=localhost;Port=5432;Database=quest_backend;Username=quest_backend;Password=quest_backend";

        services.AddDbContext<QuestDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IQuestDbContext>(sp => sp.GetRequiredService<QuestDbContext>());

        services.AddHttpContextAccessor();
        services.AddScoped<IClock, SystemClock>();
        services.AddScoped<ICurrentPrincipal, HttpContextCurrentPrincipal>();
        services.AddScoped<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<ISlugGenerator, RandomSlugGenerator>();
        services.AddScoped<IAuditWriter, AuditWriter>();
        services.AddScoped<IConfigSnapshotService, ConfigSnapshotService>();
        services.AddScoped<IExternalParticipantAuthProvider, DisabledExternalParticipantAuthProvider>();

        services.AddScoped<AdminAuthService>();
        services.AddScoped<AdminUsersService>();
        services.AddScoped<AdminConfigurationService>();
        services.AddScoped<ParticipantAuthService>();
        services.AddScoped<TeamService>();
        services.AddScoped<IQuestionRoutingResolver, QuestionRoutingResolver>();
        services.AddScoped<IAnswerEvaluator, AnswerEvaluator>();
        services.AddScoped<IEnigmaEvaluator, EnigmaEvaluator>();
        services.AddScoped<QuestDayService>();
        services.AddScoped<IQuestDayLifecycleGate>(sp => sp.GetRequiredService<QuestDayService>());
        services.AddScoped<QuestionGameService>();
        services.AddScoped<EnigmaService>();
        services.AddScoped<SupportService>();
        services.AddScoped<AuditService>();
        services.AddScoped<AppInitializer>();
        services.AddScoped<DevSampleDataSeeder>();

        return services;
    }
}

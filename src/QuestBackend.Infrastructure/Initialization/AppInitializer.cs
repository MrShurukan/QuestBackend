using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using QuestBackend.Application.Abstractions;
using QuestBackend.Domain.Admin;
using QuestBackend.Domain.Config;
using QuestBackend.Domain.QuestDay;
using QuestBackend.Domain.Shared;
using QuestBackend.Infrastructure.Persistence;

namespace QuestBackend.Infrastructure.Initialization;

public sealed class AppInitializer
{
    private readonly IClock _clock;
    private readonly IConfiguration _configuration;
    private readonly DevSampleDataSeeder _devSampleDataSeeder;
    private readonly IPasswordHasher _passwordHasher;
    private readonly QuestDbContext _dbContext;

    public AppInitializer(
        QuestDbContext dbContext,
        IConfiguration configuration,
        IPasswordHasher passwordHasher,
        IClock clock,
        DevSampleDataSeeder devSampleDataSeeder)
    {
        _dbContext = dbContext;
        _configuration = configuration;
        _passwordHasher = passwordHasher;
        _clock = clock;
        _devSampleDataSeeder = devSampleDataSeeder;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.Database.MigrateAsync(cancellationToken);

        QuestDayState? questDayState = await _dbContext.QuestDayStates.FirstOrDefaultAsync(cancellationToken);
        if (questDayState is null)
        {
            questDayState = new QuestDayState
            {
                DayCode = "default-day",
                Status = QuestDayStatus.NotStarted,
                PreStartMessage = "Игра еще не началась.",
                DayClosedMessage = "Игровой день завершен.",
                CreatedAt = _clock.UtcNow,
                UpdatedAt = _clock.UtcNow,
            };

            await _dbContext.QuestDayStates.AddAsync(questDayState, cancellationToken);
        }

        GlobalSettings? settings = await _dbContext.GlobalSettings.FirstOrDefaultAsync(cancellationToken);
        if (settings is null)
        {
            settings = new GlobalSettings
            {
                CurrentQuestDayStateId = questDayState.Id,
                CreatedAt = _clock.UtcNow,
                UpdatedAt = _clock.UtcNow,
            };

            await _dbContext.GlobalSettings.AddAsync(settings, cancellationToken);
        }
        else if (settings.CurrentQuestDayStateId is null)
        {
            settings.CurrentQuestDayStateId = questDayState.Id;
        }

        bool hasAdmin = await _dbContext.AdminUsers.AnyAsync(cancellationToken);
        if (!hasAdmin)
        {
            string login = _configuration["Bootstrap:Admin:Login"] ?? "admin";
            string password = _configuration["Bootstrap:Admin:Password"] ?? "admin123";

            AdminUser admin = new()
            {
                Login = login,
                PasswordHash = _passwordHasher.Hash(password),
                Role = AdminRole.SuperAdmin,
                PermissionsJson = "{\"all\":true}",
                IsActive = true,
                CreatedAt = _clock.UtcNow,
                UpdatedAt = _clock.UtcNow,
            };

            await _dbContext.AdminUsers.AddAsync(admin, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _devSampleDataSeeder.SeedAsync(cancellationToken);
    }
}

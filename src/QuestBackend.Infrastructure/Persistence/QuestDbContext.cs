using Microsoft.EntityFrameworkCore;
using QuestBackend.Application.Abstractions;
using QuestBackend.Domain.Admin;
using QuestBackend.Domain.Audit;
using QuestBackend.Domain.Config;
using QuestBackend.Domain.Enigma;
using QuestBackend.Domain.Participants;
using QuestBackend.Domain.Progress;
using QuestBackend.Domain.QuestDay;
using QuestBackend.Domain.Questions;
using QuestBackend.Domain.Routing;
using QuestBackend.Domain.Shared;
using QuestBackend.Domain.Tags;
using QuestBackend.Domain.Teams;

namespace QuestBackend.Infrastructure.Persistence;

public sealed class QuestDbContext : DbContext, IQuestDbContext
{
    public QuestDbContext(DbContextOptions<QuestDbContext> options)
        : base(options)
    {
    }

    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();

    public DbSet<ParticipantUser> ParticipantUsers => Set<ParticipantUser>();

    public DbSet<Team> Teams => Set<Team>();

    public DbSet<TeamMembership> TeamMemberships => Set<TeamMembership>();

    public DbSet<QuestionTag> QuestionTags => Set<QuestionTag>();

    public DbSet<Question> Questions => Set<Question>();

    public DbSet<QuestionPool> QuestionPools => Set<QuestionPool>();

    public DbSet<QuestionPoolEntry> QuestionPoolEntries => Set<QuestionPoolEntry>();

    public DbSet<QrCode> QrCodes => Set<QrCode>();

    public DbSet<RoutingProfile> RoutingProfiles => Set<RoutingProfile>();

    public DbSet<RoutingProfileTagState> RoutingProfileTagStates => Set<RoutingProfileTagState>();

    public DbSet<QrBindingOverride> QrBindingOverrides => Set<QrBindingOverride>();

    public DbSet<QuestDayState> QuestDayStates => Set<QuestDayState>();

    public DbSet<TeamQuestionState> TeamQuestionStates => Set<TeamQuestionState>();

    public DbSet<TeamAnswerAttempt> TeamAnswerAttempts => Set<TeamAnswerAttempt>();

    public DbSet<QrScanEvent> QrScanEvents => Set<QrScanEvent>();

    public DbSet<EnigmaProfile> EnigmaProfiles => Set<EnigmaProfile>();

    public DbSet<EnigmaRotorDefinition> EnigmaRotorDefinitions => Set<EnigmaRotorDefinition>();

    public DbSet<TeamRotorReward> TeamRotorRewards => Set<TeamRotorReward>();

    public DbSet<EnigmaAttempt> EnigmaAttempts => Set<EnigmaAttempt>();

    public DbSet<GlobalSettings> GlobalSettings => Set<GlobalSettings>();

    public DbSet<AdminAuditLog> AdminAuditLogs => Set<AdminAuditLog>();

    public DbSet<ConfigSnapshot> ConfigSnapshots => Set<ConfigSnapshot>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(QuestDbContext).Assembly);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;

        foreach (var entry in ChangeTracker.Entries<IAuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.CreatedAt == default)
                {
                    entry.Entity.CreatedAt = now;
                }

                entry.Entity.UpdatedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }

        foreach (var entry in ChangeTracker.Entries<IVersionedEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.Version = 1;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.Version += 1;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}

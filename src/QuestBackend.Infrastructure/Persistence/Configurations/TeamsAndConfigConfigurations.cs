using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuestBackend.Domain.Config;
using QuestBackend.Domain.QuestDay;
using QuestBackend.Domain.Teams;

namespace QuestBackend.Infrastructure.Persistence.Configurations;

internal sealed class TeamConfiguration : IEntityTypeConfiguration<Team>
{
    public void Configure(EntityTypeBuilder<Team> builder)
    {
        builder.ToTable("teams");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.JoinSecretHash).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Notes).HasColumnType("text");
        builder.Property(x => x.FinalTaskPhotoUrl).HasMaxLength(1000);
        builder.Property(x => x.FinalTaskPhotoUploadedAt);
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.HasIndex(x => x.Name).IsUnique();
    }
}

internal sealed class TeamMembershipConfiguration : IEntityTypeConfiguration<TeamMembership>
{
    public void Configure(EntityTypeBuilder<TeamMembership> builder)
    {
        builder.ToTable("team_memberships");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RemovalReason).HasColumnType("text");
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.HasIndex(x => new { x.TeamId, x.ParticipantUserId, x.Status });

        builder
            .HasOne(x => x.Team)
            .WithMany(x => x.Memberships)
            .HasForeignKey(x => x.TeamId);

        builder
            .HasOne(x => x.ParticipantUser)
            .WithMany(x => x.Memberships)
            .HasForeignKey(x => x.ParticipantUserId);
    }
}

internal sealed class QuestDayStateConfiguration : IEntityTypeConfiguration<QuestDayState>
{
    public void Configure(EntityTypeBuilder<QuestDayState> builder)
    {
        builder.ToTable("quest_day_states");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.DayCode).HasMaxLength(100).IsRequired();
        builder.Property(x => x.PreStartMessage).HasColumnType("text");
        builder.Property(x => x.DayClosedMessage).HasColumnType("text");
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.HasIndex(x => x.DayCode).IsUnique();
    }
}

internal sealed class GlobalSettingsConfiguration : IEntityTypeConfiguration<GlobalSettings>
{
    public void Configure(EntityTypeBuilder<GlobalSettings> builder)
    {
        builder.ToTable("global_settings");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.DefaultAnswerNormalization).HasColumnType("text");
        builder.Property(x => x.FlagsJson).HasColumnType("text");
        builder.Property(x => x.Timezone).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Version).IsConcurrencyToken();
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuestBackend.Domain.Audit;
using QuestBackend.Domain.Enigma;
using QuestBackend.Domain.Progress;

namespace QuestBackend.Infrastructure.Persistence.Configurations;

internal sealed class TeamQuestionStateConfiguration : IEntityTypeConfiguration<TeamQuestionState>
{
    public void Configure(EntityTypeBuilder<TeamQuestionState> builder)
    {
        builder.ToTable("team_question_states");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.HasIndex(x => new { x.TeamId, x.QuestionId }).IsUnique();
    }
}

internal sealed class TeamAnswerAttemptConfiguration : IEntityTypeConfiguration<TeamAnswerAttempt>
{
    public void Configure(EntityTypeBuilder<TeamAnswerAttempt> builder)
    {
        builder.ToTable("team_answer_attempts");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RawAnswer).HasColumnType("text");
        builder.Property(x => x.NormalizedAnswer).HasColumnType("text");
        builder.Property(x => x.EvaluationSnapshotJson).HasColumnType("text");
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.HasIndex(x => x.AttemptedAt);
    }
}

internal sealed class QrScanEventConfiguration : IEntityTypeConfiguration<QrScanEvent>
{
    public void Configure(EntityTypeBuilder<QrScanEvent> builder)
    {
        builder.ToTable("qr_scan_events");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ResolutionMetaJson).HasColumnType("text");
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.HasIndex(x => x.OccurredAt);
    }
}

internal sealed class EnigmaProfileConfiguration : IEntityTypeConfiguration<EnigmaProfile>
{
    public void Configure(EntityTypeBuilder<EnigmaProfile> builder)
    {
        builder.ToTable("enigma_profiles");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.SuccessMessage).HasColumnType("text");
        builder.Property(x => x.FailureMessage).HasColumnType("text");
        builder.Property(x => x.SecretCombinationJson).HasColumnType("text");
        builder.Property(x => x.ConfigJson).HasColumnType("text");
        builder.Property(x => x.Version).IsConcurrencyToken();
    }
}

internal sealed class EnigmaRotorDefinitionConfiguration : IEntityTypeConfiguration<EnigmaRotorDefinition>
{
    public void Configure(EntityTypeBuilder<EnigmaRotorDefinition> builder)
    {
        builder.ToTable("enigma_rotor_definitions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Label).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ColorOverride).HasMaxLength(7);
        builder.Property(x => x.MetaJson).HasColumnType("text");
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.HasIndex(x => new { x.EnigmaProfileId, x.DisplayOrder });
    }
}

internal sealed class TeamRotorRewardConfiguration : IEntityTypeConfiguration<TeamRotorReward>
{
    public void Configure(EntityTypeBuilder<TeamRotorReward> builder)
    {
        builder.ToTable("team_rotor_rewards");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RewardType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.PayloadJson).HasColumnType("text");
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.HasIndex(x => new { x.TeamId, x.SourceQuestionId }).IsUnique();
        builder.HasIndex(x => new { x.TeamId, x.TagId });
    }
}

internal sealed class EnigmaAttemptConfiguration : IEntityTypeConfiguration<EnigmaAttempt>
{
    public void Configure(EntityTypeBuilder<EnigmaAttempt> builder)
    {
        builder.ToTable("enigma_attempts");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.InputJson).HasColumnType("text");
        builder.Property(x => x.EvaluationSnapshotJson).HasColumnType("text");
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.HasIndex(x => x.AttemptedAt);
    }
}

internal sealed class TeamEnigmaDraftConfiguration : IEntityTypeConfiguration<TeamEnigmaDraft>
{
    public void Configure(EntityTypeBuilder<TeamEnigmaDraft> builder)
    {
        builder.ToTable("team_enigma_drafts");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.PositionsJson).HasColumnType("text").IsRequired();
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.HasIndex(x => new { x.TeamId, x.EnigmaProfileId }).IsUnique();
        builder.HasOne(x => x.Team)
            .WithMany(x => x.EnigmaDrafts)
            .HasForeignKey(x => x.TeamId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.EnigmaProfile)
            .WithMany(x => x.TeamDrafts)
            .HasForeignKey(x => x.EnigmaProfileId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class AdminAuditLogConfiguration : IEntityTypeConfiguration<AdminAuditLog>
{
    public void Configure(EntityTypeBuilder<AdminAuditLog> builder)
    {
        builder.ToTable("admin_audit_logs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ActionType).HasMaxLength(200).IsRequired();
        builder.Property(x => x.EntityType).HasMaxLength(200).IsRequired();
        builder.Property(x => x.EntityId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.DiffJson).HasColumnType("text");
        builder.Property(x => x.Reason).HasColumnType("text");
        builder.Property(x => x.CorrelationId).HasMaxLength(100);
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.HasIndex(x => x.OccurredAt);
    }
}

internal sealed class ConfigSnapshotConfiguration : IEntityTypeConfiguration<ConfigSnapshot>
{
    public void Configure(EntityTypeBuilder<ConfigSnapshot> builder)
    {
        builder.ToTable("config_snapshots");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.SnapshotType).HasMaxLength(200).IsRequired();
        builder.Property(x => x.PayloadJson).HasColumnType("text");
        builder.Property(x => x.Comment).HasColumnType("text");
        builder.Property(x => x.Version).IsConcurrencyToken();
    }
}

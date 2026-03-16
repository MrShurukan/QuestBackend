using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuestBackend.Domain.Questions;
using QuestBackend.Domain.Routing;
using QuestBackend.Domain.Tags;

namespace QuestBackend.Infrastructure.Persistence.Configurations;

internal sealed class QuestionTagConfiguration : IEntityTypeConfiguration<QuestionTag>
{
    public void Configure(EntityTypeBuilder<QuestionTag> builder)
    {
        builder.ToTable("question_tags");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Color).HasMaxLength(7).IsRequired();
        builder.Property(x => x.Description).HasColumnType("text");
        builder.Property(x => x.UiMetaJson).HasColumnType("text");
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.HasIndex(x => x.Code).IsUnique();
    }
}

internal sealed class QuestionConfiguration : IEntityTypeConfiguration<Question>
{
    public void Configure(EntityTypeBuilder<Question> builder)
    {
        builder.ToTable("questions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).HasMaxLength(300).IsRequired();
        builder.Property(x => x.BodyRichText).HasColumnType("text");
        builder.Property(x => x.FooterHint).HasColumnType("text");
        builder.Property(x => x.ImageUrl).HasMaxLength(1000);
        builder.Property(x => x.UiMetaJson).HasColumnType("text");
        builder.Property(x => x.SupportNotes).HasColumnType("text");
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.HasIndex(x => new { x.TagId, x.Status });

        builder
            .HasOne(x => x.Tag)
            .WithMany(x => x.Questions)
            .HasForeignKey(x => x.TagId);

        builder.ComplexProperty(
            x => x.AnswerSchema,
            schemaBuilder =>
            {
                schemaBuilder.ToJson();
                schemaBuilder.ComplexProperty(x => x.Normalization);
            });
    }
}

internal sealed class QuestionPoolConfiguration : IEntityTypeConfiguration<QuestionPool>
{
    public void Configure(EntityTypeBuilder<QuestionPool> builder)
    {
        builder.ToTable("question_pools");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasColumnType("text");
        builder.Property(x => x.Version).IsConcurrencyToken();

        builder
            .HasOne(x => x.Tag)
            .WithMany(x => x.Pools)
            .HasForeignKey(x => x.TagId);
    }
}

internal sealed class QuestionPoolEntryConfiguration : IEntityTypeConfiguration<QuestionPoolEntry>
{
    public void Configure(EntityTypeBuilder<QuestionPoolEntry> builder)
    {
        builder.ToTable("question_pool_entries");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Notes).HasColumnType("text");
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.HasIndex(x => new { x.PoolId, x.Position }).IsUnique();
        builder.HasIndex(x => new { x.PoolId, x.QuestionId });

        builder
            .HasOne(x => x.Pool)
            .WithMany(x => x.Entries)
            .HasForeignKey(x => x.PoolId);

        builder
            .HasOne(x => x.Question)
            .WithMany(x => x.PoolEntries)
            .HasForeignKey(x => x.QuestionId);
    }
}

internal sealed class QrCodeConfiguration : IEntityTypeConfiguration<QrCode>
{
    public void Configure(EntityTypeBuilder<QrCode> builder)
    {
        builder.ToTable("qr_codes");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Slug).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Label).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Notes).HasColumnType("text");
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.HasIndex(x => x.Slug).IsUnique();

        builder
            .HasOne(x => x.Tag)
            .WithMany(x => x.QrCodes)
            .HasForeignKey(x => x.TagId);
    }
}

internal sealed class RoutingProfileConfiguration : IEntityTypeConfiguration<RoutingProfile>
{
    public void Configure(EntityTypeBuilder<RoutingProfile> builder)
    {
        builder.ToTable("routing_profiles");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasColumnType("text");
        builder.Property(x => x.Version).IsConcurrencyToken();
    }
}

internal sealed class RoutingProfileTagStateConfiguration : IEntityTypeConfiguration<RoutingProfileTagState>
{
    public void Configure(EntityTypeBuilder<RoutingProfileTagState> builder)
    {
        builder.ToTable("routing_profile_tag_states");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.HasIndex(x => new { x.RoutingProfileId, x.TagId }).IsUnique();

        builder
            .HasOne(x => x.RoutingProfile)
            .WithMany(x => x.TagStates)
            .HasForeignKey(x => x.RoutingProfileId);

        builder
            .HasOne(x => x.Tag)
            .WithMany()
            .HasForeignKey(x => x.TagId);

        builder
            .HasOne(x => x.ActivePool)
            .WithMany()
            .HasForeignKey(x => x.ActivePoolId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

internal sealed class QrBindingOverrideConfiguration : IEntityTypeConfiguration<QrBindingOverride>
{
    public void Configure(EntityTypeBuilder<QrBindingOverride> builder)
    {
        builder.ToTable("qr_binding_overrides");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Reason).HasColumnType("text");
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.HasIndex(x => new { x.QrCodeId, x.IsActive });

        builder
            .HasOne(x => x.QrCode)
            .WithMany(x => x.Overrides)
            .HasForeignKey(x => x.QrCodeId);

        builder
            .HasOne(x => x.Question)
            .WithMany()
            .HasForeignKey(x => x.QuestionId);

        builder
            .HasOne(x => x.ScopeProfile)
            .WithMany()
            .HasForeignKey(x => x.ScopeProfileId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

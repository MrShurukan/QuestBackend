using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuestBackend.Domain.Admin;
using QuestBackend.Domain.Participants;

namespace QuestBackend.Infrastructure.Persistence.Configurations;

internal sealed class AdminUserConfiguration : IEntityTypeConfiguration<AdminUser>
{
    public void Configure(EntityTypeBuilder<AdminUser> builder)
    {
        builder.ToTable("admin_users");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Login).HasMaxLength(100).IsRequired();
        builder.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired();
        builder.Property(x => x.PermissionsJson).HasColumnType("text");
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.HasIndex(x => x.Login).IsUnique();
    }
}

internal sealed class ParticipantUserConfiguration : IEntityTypeConfiguration<ParticipantUser>
{
    public void Configure(EntityTypeBuilder<ParticipantUser> builder)
    {
        builder.ToTable("participant_users");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Provider).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ProviderSubject).HasMaxLength(200).IsRequired();
        builder.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.PasswordHash).HasMaxLength(500);
        builder.Property(x => x.AvatarUrl).HasMaxLength(1000);
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.HasIndex(x => new { x.Provider, x.ProviderSubject }).IsUnique();
    }
}

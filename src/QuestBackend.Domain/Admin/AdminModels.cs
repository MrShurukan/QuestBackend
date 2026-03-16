using QuestBackend.Domain.Shared;

namespace QuestBackend.Domain.Admin;

public enum AdminRole
{
    SuperAdmin = 1,
    Editor = 2,
    Support = 3,
}

public sealed class AdminUser : EntityBase
{
    public string Login { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public AdminRole Role { get; set; } = AdminRole.SuperAdmin;

    public string PermissionsJson { get; set; } = "{}";

    public bool IsActive { get; set; } = true;

    public DateTimeOffset? LastLoginAt { get; set; }
}

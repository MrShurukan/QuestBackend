namespace QuestBackend.Contracts;

public sealed record AdminLoginRequest(string Login, string Password);

public sealed record AuthenticatedAdminResponse(Guid Id, string Login, string Role, bool IsActive);

public sealed record AdminSelfProfileUpdateRequest(string CurrentPassword, string? NewLogin, string? NewPassword);

public sealed record AdminUserCreateRequest(string Login, string Password, string Role);

public sealed record AdminUserListItemResponse(
    Guid Id,
    string Login,
    string Role,
    bool IsActive,
    DateTimeOffset? LastLoginAt);

public sealed record ParticipantLoginRequest(string Login, string Password);

public sealed record ParticipantProfileResponse(
    Guid Id,
    string Provider,
    string ProviderSubject,
    string DisplayName,
    string? AvatarUrl,
    bool IsBlocked,
    string? Login);

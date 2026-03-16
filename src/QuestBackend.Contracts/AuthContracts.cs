namespace QuestBackend.Contracts;

public sealed record AdminLoginRequest(string Login, string Password);

public sealed record AuthenticatedAdminResponse(Guid Id, string Login, string Role, bool IsActive);

public sealed record DevParticipantLoginRequest(string ProviderSubject, string DisplayName, string? AvatarUrl);

public sealed record ParticipantProfileResponse(Guid Id, string Provider, string ProviderSubject, string DisplayName, string? AvatarUrl, bool IsBlocked);

using Microsoft.EntityFrameworkCore;
using QuestBackend.Application.Abstractions;
using QuestBackend.Application.Shared;
using QuestBackend.Contracts;
using QuestBackend.Domain.Participants;
using QuestBackend.Domain.Shared;

namespace QuestBackend.Application.Participants;

public sealed class ParticipantAuthService
{
    private const int MinLoginLength = 3;
    private const int MaxLoginLength = 100;
    private const int MinDisplayNameLength = 2;
    private const int MaxDisplayNameLength = 200;
    private const int MinPasswordLength = 8;

    private readonly IClock _clock;
    private readonly ICurrentPrincipal _currentPrincipal;
    private readonly IQuestDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;

    public ParticipantAuthService(
        IQuestDbContext dbContext,
        ICurrentPrincipal currentPrincipal,
        IClock clock,
        IPasswordHasher passwordHasher)
    {
        _dbContext = dbContext;
        _currentPrincipal = currentPrincipal;
        _clock = clock;
        _passwordHasher = passwordHasher;
    }

    public static string NormalizeLogin(string login) => login.Trim().ToLowerInvariant();

    public async Task<ParticipantUser> RegisterLocalAsync(
        string login,
        string displayName,
        string password,
        string? avatarUrl,
        CancellationToken cancellationToken = default)
    {
        string normalizedLogin = NormalizeLogin(login);
        ValidateLogin(normalizedLogin);
        string trimmedDisplayName = displayName.Trim();
        if (trimmedDisplayName.Length < MinDisplayNameLength || trimmedDisplayName.Length > MaxDisplayNameLength)
        {
            throw new AppException(400, "ФИО должно быть от 2 до 200 символов.");
        }

        if (password.Length < MinPasswordLength)
        {
            throw new AppException(400, $"Пароль не короче {MinPasswordLength} символов.");
        }

        bool exists = await _dbContext.ParticipantUsers.AnyAsync(
            x => x.Provider == ParticipantAuthProviders.Local && x.ProviderSubject == normalizedLogin,
            cancellationToken);

        if (exists)
        {
            throw new AppException(409, "Этот логин уже зарегистрирован.");
        }

        ParticipantUser participant = new()
        {
            Provider = ParticipantAuthProviders.Local,
            ProviderSubject = normalizedLogin,
            DisplayName = trimmedDisplayName,
            PasswordHash = _passwordHasher.Hash(password),
            AvatarUrl = avatarUrl,
            LastSeenAt = _clock.UtcNow,
            PersonalDataConsentAcceptedAt = _clock.UtcNow,
        };

        await _dbContext.ParticipantUsers.AddAsync(participant, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return participant;
    }

    public async Task<ParticipantUser> LoginLocalAsync(ParticipantLoginRequest request, CancellationToken cancellationToken = default)
    {
        string normalizedLogin = NormalizeLogin(request.Login);
        if (normalizedLogin.Length < MinLoginLength)
        {
            throw new AppException(401, "Неверный логин или пароль.");
        }

        ParticipantUser? participant = await _dbContext.ParticipantUsers
            .SingleOrDefaultAsync(
                x => x.Provider == ParticipantAuthProviders.Local && x.ProviderSubject == normalizedLogin,
                cancellationToken);

        if (participant is null
            || string.IsNullOrEmpty(participant.PasswordHash)
            || participant.IsBlocked
            || !_passwordHasher.Verify(request.Password, participant.PasswordHash))
        {
            throw new AppException(401, "Неверный логин или пароль.");
        }

        participant.LastSeenAt = _clock.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return participant;
    }

    public async Task<ParticipantProfileResponse?> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        if (!_currentPrincipal.IsParticipantAuthenticated || _currentPrincipal.ParticipantUserId is null)
        {
            return null;
        }

        ParticipantUser? participant = await _dbContext.ParticipantUsers
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == _currentPrincipal.ParticipantUserId.Value, cancellationToken);

        return participant is null ? null : ToResponse(participant);
    }

    public static ParticipantProfileResponse ToResponse(ParticipantUser participant) =>
        new(
            participant.Id,
            participant.Provider,
            participant.ProviderSubject,
            participant.DisplayName,
            participant.AvatarUrl,
            participant.IsBlocked,
            participant.Provider == ParticipantAuthProviders.Local ? participant.ProviderSubject : null);

    private static void ValidateLogin(string normalizedLogin)
    {
        if (normalizedLogin.Length < MinLoginLength || normalizedLogin.Length > MaxLoginLength)
        {
            throw new AppException(400, $"Логин должен быть от {MinLoginLength} до {MaxLoginLength} символов.");
        }
    }
}

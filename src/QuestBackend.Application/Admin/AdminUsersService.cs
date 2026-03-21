using Microsoft.EntityFrameworkCore;
using QuestBackend.Application.Abstractions;
using QuestBackend.Application.Shared;
using QuestBackend.Contracts;
using QuestBackend.Domain.Admin;
using QuestBackend.Domain.Shared;

namespace QuestBackend.Application.Admin;

public sealed class AdminUsersService
{
    private readonly IAuditWriter _auditWriter;
    private readonly IClock _clock;
    private readonly ICurrentPrincipal _currentPrincipal;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IQuestDbContext _dbContext;

    public AdminUsersService(
        IQuestDbContext dbContext,
        IPasswordHasher passwordHasher,
        ICurrentPrincipal currentPrincipal,
        IClock clock,
        IAuditWriter auditWriter)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _currentPrincipal = currentPrincipal;
        _clock = clock;
        _auditWriter = auditWriter;
    }

    public async Task<AdminUser> UpdateMyProfileAsync(AdminSelfProfileUpdateRequest request, CancellationToken cancellationToken = default)
    {
        if (!_currentPrincipal.IsAdminAuthenticated || _currentPrincipal.AdminUserId is null)
        {
            throw new AppException(401, "Требуется вход администратора.");
        }

        if (string.IsNullOrWhiteSpace(request.CurrentPassword))
        {
            throw new AppException(400, "Укажите текущий пароль.");
        }

        string? newLogin = string.IsNullOrWhiteSpace(request.NewLogin) ? null : request.NewLogin.Trim();
        string? newPassword = string.IsNullOrWhiteSpace(request.NewPassword) ? null : request.NewPassword;

        if (newLogin is null && newPassword is null)
        {
            throw new AppException(400, "Укажите новый логин и/или новый пароль.");
        }

        if (newLogin is not null && newLogin.Length > 100)
        {
            throw new AppException(400, "Логин не длиннее 100 символов.");
        }

        if (newPassword is not null && newPassword.Length < 8)
        {
            throw new AppException(400, "Новый пароль не короче 8 символов.");
        }

        AdminUser admin = await _dbContext.AdminUsers.SingleAsync(x => x.Id == _currentPrincipal.AdminUserId.Value, cancellationToken);

        if (!_passwordHasher.Verify(request.CurrentPassword, admin.PasswordHash))
        {
            throw new AppException(401, "Неверный текущий пароль.");
        }

        if (newLogin is not null)
        {
            bool taken = await _dbContext.AdminUsers.AnyAsync(x => x.Login == newLogin && x.Id != admin.Id, cancellationToken);
            if (taken)
            {
                throw new AppException(409, "Логин уже занят.");
            }

            admin.Login = newLogin;
        }

        if (newPassword is not null)
        {
            admin.PasswordHash = _passwordHasher.Hash(newPassword);
        }

        admin.UpdatedAt = _clock.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditWriter.WriteAsync(
            "AdminSelfProfileUpdate",
            nameof(AdminUser),
            admin.Id.ToString(),
            AppJson.Serialize(new { loginChanged = newLogin is not null, passwordChanged = newPassword is not null }),
            null,
            cancellationToken);

        return admin;
    }

    public async Task<IReadOnlyList<AdminUserListItemResponse>> ListAdminsAsync(CancellationToken cancellationToken = default)
    {
        List<AdminUser> users = await _dbContext.AdminUsers.AsNoTracking().OrderBy(x => x.Login).ToListAsync(cancellationToken);
        return users.Select(ToListItem).ToList();
    }

    public async Task<AdminUserListItemResponse> CreateAdminAsync(AdminUserCreateRequest request, CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<AdminRole>(request.Role, true, out AdminRole role))
        {
            throw new AppException(400, "Недопустимая роль. Допустимо: SuperAdmin, Editor, Support.");
        }

        string login = request.Login.Trim();
        if (login.Length is < 1 or > 100)
        {
            throw new AppException(400, "Логин от 1 до 100 символов.");
        }

        if (request.Password.Length < 8)
        {
            throw new AppException(400, "Пароль не короче 8 символов.");
        }

        if (await _dbContext.AdminUsers.AnyAsync(x => x.Login == login, cancellationToken))
        {
            throw new AppException(409, "Логин уже занят.");
        }

        AdminUser user = new()
        {
            Login = login,
            PasswordHash = _passwordHasher.Hash(request.Password),
            Role = role,
            IsActive = true,
            PermissionsJson = "{}",
            CreatedAt = _clock.UtcNow,
            UpdatedAt = _clock.UtcNow,
        };

        await _dbContext.AdminUsers.AddAsync(user, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditWriter.WriteAsync(
            "AdminUserCreated",
            nameof(AdminUser),
            user.Id.ToString(),
            AppJson.Serialize(new { login, role = role.ToString() }),
            null,
            cancellationToken);

        return ToListItem(user);
    }

    private static AdminUserListItemResponse ToListItem(AdminUser user) =>
        new(user.Id, user.Login, user.Role.ToString(), user.IsActive, user.LastLoginAt);
}

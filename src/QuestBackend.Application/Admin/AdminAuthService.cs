using Microsoft.EntityFrameworkCore;
using QuestBackend.Application.Abstractions;
using QuestBackend.Application.Shared;
using QuestBackend.Contracts;
using QuestBackend.Domain.Admin;
using QuestBackend.Domain.Shared;

namespace QuestBackend.Application.Admin;

public sealed class AdminAuthService
{
    private readonly IClock _clock;
    private readonly ICurrentPrincipal _currentPrincipal;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IQuestDbContext _dbContext;

    public AdminAuthService(
        IQuestDbContext dbContext,
        IPasswordHasher passwordHasher,
        ICurrentPrincipal currentPrincipal,
        IClock clock)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _currentPrincipal = currentPrincipal;
        _clock = clock;
    }

    public async Task<AdminUser> LoginAsync(AdminLoginRequest request, CancellationToken cancellationToken = default)
    {
        AdminUser? admin = await _dbContext.AdminUsers
            .SingleOrDefaultAsync(x => x.Login == request.Login, cancellationToken);

        if (admin is null || !admin.IsActive || !_passwordHasher.Verify(request.Password, admin.PasswordHash))
        {
            throw new AppException(401, "Invalid admin credentials.");
        }

        admin.LastLoginAt = _clock.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return admin;
    }

    public async Task<AuthenticatedAdminResponse?> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        if (!_currentPrincipal.IsAdminAuthenticated || _currentPrincipal.AdminUserId is null)
        {
            return null;
        }

        AdminUser? admin = await _dbContext.AdminUsers
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == _currentPrincipal.AdminUserId.Value, cancellationToken);

        return admin is null ? null : ToResponse(admin);
    }

    public static AuthenticatedAdminResponse ToResponse(AdminUser admin) =>
        new(admin.Id, admin.Login, admin.Role.ToString(), admin.IsActive);
}

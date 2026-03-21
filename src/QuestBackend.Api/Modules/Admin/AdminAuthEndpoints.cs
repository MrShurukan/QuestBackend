using Microsoft.AspNetCore.Authentication;
using QuestBackend.Api.Common;
using QuestBackend.Application.Admin;
using QuestBackend.Application.Shared;
using QuestBackend.Contracts;
using QuestBackend.Domain.Admin;

namespace QuestBackend.Api.Modules.Admin;

public static class AdminAuthEndpoints
{
    public static IEndpointRouteBuilder MapAdminAuthEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/admin/auth")
            .RequireRateLimiting(Common.RateLimiting.AuthPolicy);

        group.MapPost(
            "/login",
            async (AdminLoginRequest request, AdminAuthService service, HttpContext httpContext, CancellationToken cancellationToken) =>
            {
                var admin = await service.LoginAsync(request, cancellationToken);
                await httpContext.SignInAsync(
                    QuestAuthConstants.AdminScheme,
                    AuthPrincipalFactory.CreateAdminPrincipal(admin));

                return Results.Ok(AdminAuthService.ToResponse(admin));
            });

        group.MapGet(
                "/me",
                async (AdminAuthService service, CancellationToken cancellationToken) =>
                {
                    AuthenticatedAdminResponse? admin = await service.GetCurrentAsync(cancellationToken);
                    return admin is null ? Results.Unauthorized() : Results.Ok(admin);
                })
            .RequireAuthorization(ApiPolicies.AdminOnly);

        group.MapPut(
                "/profile",
                async (AdminSelfProfileUpdateRequest request, AdminUsersService usersService, HttpContext httpContext, CancellationToken cancellationToken) =>
                {
                    AdminUser admin = await usersService.UpdateMyProfileAsync(request, cancellationToken);
                    await httpContext.SignInAsync(
                        QuestAuthConstants.AdminScheme,
                        AuthPrincipalFactory.CreateAdminPrincipal(admin));

                    return Results.Ok(AdminAuthService.ToResponse(admin));
                })
            .RequireAuthorization(ApiPolicies.AdminOnly);

        group.MapPost(
                "/logout",
                async (HttpContext httpContext) =>
                {
                    await httpContext.SignOutAsync(QuestAuthConstants.AdminScheme);
                    return Results.NoContent();
                })
            .RequireAuthorization(ApiPolicies.AdminOnly);

        return app;
    }
}

using QuestBackend.Api.Common;
using QuestBackend.Application.Admin;
using QuestBackend.Contracts;

namespace QuestBackend.Api.Modules.Admin;

public static class AdminUsersEndpoints
{
    public static IEndpointRouteBuilder MapAdminUsersEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/admin/users")
            .RequireAuthorization(ApiPolicies.AdminSuperOnly);

        group.MapGet(
            "/",
            async (AdminUsersService service, CancellationToken cancellationToken) =>
                Results.Ok(await service.ListAdminsAsync(cancellationToken)));

        group.MapPost(
            "/",
            async (AdminUserCreateRequest request, AdminUsersService service, CancellationToken cancellationToken) =>
                Results.Ok(await service.CreateAdminAsync(request, cancellationToken)));

        return app;
    }
}

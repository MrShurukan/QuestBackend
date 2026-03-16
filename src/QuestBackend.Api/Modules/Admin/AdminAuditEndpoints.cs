using QuestBackend.Api.Common;
using QuestBackend.Application.Audit;

namespace QuestBackend.Api.Modules.Admin;

public static class AdminAuditEndpoints
{
    public static IEndpointRouteBuilder MapAdminAuditEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/admin/audit")
            .RequireAuthorization(ApiPolicies.AdminOnly);

        group.MapGet(
            "/",
            async (int? take, AuditService service, CancellationToken cancellationToken) =>
            {
                return Results.Ok(await service.GetEntriesAsync(take ?? 200, cancellationToken));
            });

        return app;
    }
}

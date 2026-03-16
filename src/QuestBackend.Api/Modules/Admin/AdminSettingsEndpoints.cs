using QuestBackend.Api.Common;
using QuestBackend.Application.Admin;
using QuestBackend.Contracts;

namespace QuestBackend.Api.Modules.Admin;

public static class AdminSettingsEndpoints
{
    public static IEndpointRouteBuilder MapAdminSettingsEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/admin/settings")
            .RequireAuthorization(ApiPolicies.AdminOnly);

        group.MapGet(
            "/global",
            async (AdminConfigurationService service, CancellationToken cancellationToken) =>
            {
                return Results.Ok(await service.GetGlobalSettingsAsync(cancellationToken));
            });

        group.MapPut(
            "/global",
            async (GlobalSettingsUpdateRequest request, AdminConfigurationService service, CancellationToken cancellationToken) =>
            {
                return Results.Ok(await service.UpdateGlobalSettingsAsync(request, cancellationToken));
            });

        return app;
    }
}

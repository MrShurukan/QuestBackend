using QuestBackend.Api.Common;
using QuestBackend.Application.Admin;
using QuestBackend.Contracts;

namespace QuestBackend.Api.Modules.Admin;

public static class AdminRoutingEndpoints
{
    public static IEndpointRouteBuilder MapAdminRoutingEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/admin/routing")
            .RequireAuthorization(ApiPolicies.AdminOnly);

        group.MapGet(
            "/profiles",
            async (AdminConfigurationService service, CancellationToken cancellationToken) =>
            {
                return Results.Ok(await service.GetRoutingProfilesAsync(cancellationToken));
            });

        group.MapPost(
            "/profiles",
            async (RoutingProfileUpsertRequest request, AdminConfigurationService service, CancellationToken cancellationToken) =>
            {
                return Results.Ok(await service.CreateRoutingProfileAsync(request, cancellationToken));
            });

        group.MapPut(
            "/profiles/{id:guid}",
            async (Guid id, RoutingProfileUpsertRequest request, AdminConfigurationService service, CancellationToken cancellationToken) =>
            {
                return Results.Ok(await service.UpdateRoutingProfileAsync(id, request, cancellationToken));
            });

        group.MapPost(
            "/profiles/{id:guid}/activate",
            async (Guid id, AdminConfigurationService service, CancellationToken cancellationToken) =>
            {
                await service.ActivateRoutingProfileAsync(id, cancellationToken);
                return Results.NoContent();
            });

        group.MapPost(
            "/tags/{tagId:guid}/rotate",
            async (Guid tagId, int step, AdminConfigurationService service, CancellationToken cancellationToken) =>
            {
                await service.RotateTagPoolOffsetAsync(tagId, step, cancellationToken);
                return Results.NoContent();
            });

        group.MapGet(
            "/preview",
            async (AdminConfigurationService service, CancellationToken cancellationToken) =>
            {
                return Results.Ok(await service.PreviewRoutingMatrixAsync(cancellationToken));
            });

        group.MapPost(
            "/overrides",
            async (QrBindingOverrideRequest request, AdminConfigurationService service, CancellationToken cancellationToken) =>
            {
                return Results.Ok(await service.SetQrBindingOverrideAsync(request, cancellationToken));
            });

        group.MapDelete(
            "/overrides/{id:guid}",
            async (Guid id, AdminConfigurationService service, CancellationToken cancellationToken) =>
            {
                await service.ClearQrBindingOverrideAsync(id, cancellationToken);
                return Results.NoContent();
            });

        return app;
    }
}

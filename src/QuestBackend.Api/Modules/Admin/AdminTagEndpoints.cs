using QuestBackend.Api.Common;
using QuestBackend.Application.Admin;
using QuestBackend.Contracts;

namespace QuestBackend.Api.Modules.Admin;

public static class AdminTagEndpoints
{
    public static IEndpointRouteBuilder MapAdminTagEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/admin/tags")
            .RequireAuthorization(ApiPolicies.AdminOnly);

        group.MapGet(
            "/",
            async (AdminConfigurationService service, CancellationToken cancellationToken) =>
            {
                return Results.Ok(await service.GetTagsAsync(cancellationToken));
            });

        group.MapPost(
            "/",
            async (TagUpsertRequest request, AdminConfigurationService service, CancellationToken cancellationToken) =>
            {
                return Results.Ok(await service.CreateTagAsync(request, cancellationToken));
            });

        group.MapPut(
            "/{id:guid}",
            async (Guid id, TagUpsertRequest request, AdminConfigurationService service, CancellationToken cancellationToken) =>
            {
                return Results.Ok(await service.UpdateTagAsync(id, request, cancellationToken));
            });

        return app;
    }
}

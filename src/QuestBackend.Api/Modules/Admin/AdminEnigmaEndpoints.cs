using QuestBackend.Api.Common;
using QuestBackend.Application.Admin;
using QuestBackend.Contracts;

namespace QuestBackend.Api.Modules.Admin;

public static class AdminEnigmaEndpoints
{
    public static IEndpointRouteBuilder MapAdminEnigmaEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/admin/enigma")
            .RequireAuthorization(ApiPolicies.AdminOnly);

        group.MapGet(
            "/profiles",
            async (AdminConfigurationService service, CancellationToken cancellationToken) =>
            {
                return Results.Ok(await service.GetEnigmaProfilesAsync(cancellationToken));
            });

        group.MapPost(
            "/profiles",
            async (EnigmaProfileUpsertRequest request, AdminConfigurationService service, CancellationToken cancellationToken) =>
            {
                return Results.Ok(await service.CreateEnigmaProfileAsync(request, cancellationToken));
            });

        group.MapPut(
            "/profiles/{id:guid}",
            async (Guid id, EnigmaProfileUpsertRequest request, AdminConfigurationService service, CancellationToken cancellationToken) =>
            {
                return Results.Ok(await service.UpdateEnigmaProfileAsync(id, request, cancellationToken));
            });

        group.MapPost(
            "/profiles/{id:guid}/activate",
            async (Guid id, AdminConfigurationService service, CancellationToken cancellationToken) =>
            {
                await service.ActivateEnigmaProfileAsync(id, cancellationToken);
                return Results.NoContent();
            });

        return app;
    }
}

using QuestBackend.Api.Common;
using QuestBackend.Application.Admin;
using QuestBackend.Contracts;

namespace QuestBackend.Api.Modules.Admin;

public static class AdminPoolEndpoints
{
    public static IEndpointRouteBuilder MapAdminPoolEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/admin/pools")
            .RequireAuthorization(ApiPolicies.AdminOnly);

        group.MapGet(
            "/",
            async (AdminConfigurationService service, CancellationToken cancellationToken) =>
            {
                return Results.Ok(await service.GetPoolsAsync(cancellationToken));
            });

        group.MapPost(
            "/",
            async (QuestionPoolUpsertRequest request, AdminConfigurationService service, CancellationToken cancellationToken) =>
            {
                return Results.Ok(await service.CreatePoolAsync(request, cancellationToken));
            });

        group.MapPut(
            "/{id:guid}",
            async (Guid id, QuestionPoolUpsertRequest request, AdminConfigurationService service, CancellationToken cancellationToken) =>
            {
                return Results.Ok(await service.UpdatePoolAsync(id, request, cancellationToken));
            });

        return app;
    }
}

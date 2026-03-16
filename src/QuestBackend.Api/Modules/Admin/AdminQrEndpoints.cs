using QuestBackend.Api.Common;
using QuestBackend.Application.Admin;
using QuestBackend.Contracts;

namespace QuestBackend.Api.Modules.Admin;

public static class AdminQrEndpoints
{
    public static IEndpointRouteBuilder MapAdminQrEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/admin/qr")
            .RequireAuthorization(ApiPolicies.AdminOnly);

        group.MapGet(
            "/",
            async (AdminConfigurationService service, CancellationToken cancellationToken) =>
            {
                return Results.Ok(await service.GetQrCodesAsync(cancellationToken));
            });

        group.MapPost(
            "/",
            async (QrCodeUpsertRequest request, AdminConfigurationService service, CancellationToken cancellationToken) =>
            {
                return Results.Ok(await service.CreateQrCodeAsync(request, cancellationToken));
            });

        group.MapPut(
            "/{id:guid}",
            async (Guid id, QrCodeUpsertRequest request, AdminConfigurationService service, CancellationToken cancellationToken) =>
            {
                return Results.Ok(await service.UpdateQrCodeAsync(id, request, cancellationToken));
            });

        return app;
    }
}

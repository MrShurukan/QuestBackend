using QuestBackend.Api.Common;
using QuestBackend.Application.QuestDay;
using QuestBackend.Contracts;

namespace QuestBackend.Api.Modules.Admin;

public static class AdminQuestDayLifecycleEndpoints
{
    public static IEndpointRouteBuilder MapAdminQuestDayLifecycleEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/admin/quest-day")
            .RequireAuthorization(ApiPolicies.AdminOnly);

        group.MapGet(
            "/",
            async (QuestDayService service, CancellationToken cancellationToken) =>
            {
                return Results.Ok(await service.GetAdminStateAsync(cancellationToken));
            });

        group.MapPut(
            "/messages",
            async (UpdateQuestDayMessagesRequest request, QuestDayService service, CancellationToken cancellationToken) =>
            {
                return Results.Ok(await service.UpdateMessagesAsync(request, cancellationToken));
            });

        group.MapPost(
            "/start",
            async (QuestDayService service, CancellationToken cancellationToken) =>
            {
                return Results.Ok(await service.StartAsync(cancellationToken));
            });

        group.MapPost(
            "/finish",
            async (QuestDayService service, CancellationToken cancellationToken) =>
            {
                return Results.Ok(await service.FinishAsync(cancellationToken));
            });

        return app;
    }
}

using QuestBackend.Application.QuestDay;

namespace QuestBackend.Api.Modules.QuestDay;

public static class QuestDayPublicEndpoints
{
    public static IEndpointRouteBuilder MapQuestDayPublicEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet(
            "/api/quest-day/public",
            async (QuestDayService service, CancellationToken cancellationToken) =>
            {
                return Results.Ok(await service.GetPublicStateAsync(cancellationToken));
            });

        return app;
    }
}

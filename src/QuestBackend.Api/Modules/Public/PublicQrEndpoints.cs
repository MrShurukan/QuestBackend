using QuestBackend.Application.Questions;

namespace QuestBackend.Api.Modules.Public;

public static class PublicQrEndpoints
{
    public static IEndpointRouteBuilder MapPublicQrEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/public");

        group.MapGet(
                "/qr/{slug}",
                async (string slug, QuestionGameService service, CancellationToken cancellationToken) =>
                {
                    return Results.Ok(await service.ResolveQrScanAsync(slug, cancellationToken));
                })
            .RequireRateLimiting(Common.RateLimiting.PublicQrPolicy);

        return app;
    }
}

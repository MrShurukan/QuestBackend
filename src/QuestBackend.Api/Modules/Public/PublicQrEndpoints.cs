using QuestBackend.Application.Questions;

namespace QuestBackend.Api.Modules.Public;

public static class PublicQrEndpoints
{
    public static IEndpointRouteBuilder MapPublicQrEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet(
            "/q/{slug}",
            async (string slug, QuestionGameService service, CancellationToken cancellationToken) =>
            {
                return Results.Ok(await service.ResolveQrScanAsync(slug, cancellationToken));
            })
            .RequireRateLimiting(Common.RateLimiting.PublicQrPolicy);

        return app;
    }
}

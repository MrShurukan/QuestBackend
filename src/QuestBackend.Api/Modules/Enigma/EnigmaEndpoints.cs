using QuestBackend.Api.Common;
using QuestBackend.Application.Enigma;
using QuestBackend.Contracts;

namespace QuestBackend.Api.Modules.Enigma;

public static class EnigmaEndpoints
{
    public static IEndpointRouteBuilder MapEnigmaEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/enigma")
            .RequireAuthorization(ApiPolicies.ParticipantOnly);

        group.MapGet(
            "/state",
            async (EnigmaService service, CancellationToken cancellationToken) =>
            {
                return Results.Ok(await service.GetStateAsync(cancellationToken));
            });

        group.MapPost(
            "/attempts",
            async (SubmitEnigmaAttemptRequest request, EnigmaService service, CancellationToken cancellationToken) =>
            {
                return Results.Ok(await service.SubmitAttemptAsync(request, cancellationToken));
            })
            .RequireRateLimiting(Common.RateLimiting.SubmissionPolicy);

        group.MapPut(
            "/draft-positions",
            async (UpdateEnigmaDraftPositionsRequest request, EnigmaService service, CancellationToken cancellationToken) =>
            {
                await service.SaveDraftPositionsAsync(request, cancellationToken);
                return Results.NoContent();
            })
            .RequireRateLimiting(Common.RateLimiting.EnigmaDraftPolicy);

        return app;
    }
}

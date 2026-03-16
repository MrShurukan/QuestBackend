using Microsoft.AspNetCore.Authentication;
using QuestBackend.Api.Common;
using QuestBackend.Application.Participants;
using QuestBackend.Application.Shared;
using QuestBackend.Contracts;

namespace QuestBackend.Api.Modules.Participants;

public static class ParticipantAuthEndpoints
{
    public static IEndpointRouteBuilder MapParticipantAuthEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/participant/auth")
            .RequireRateLimiting(Common.RateLimiting.AuthPolicy);

        group.MapPost(
            "/dev-login",
            async (DevParticipantLoginRequest request, ParticipantAuthService service, HttpContext httpContext, CancellationToken cancellationToken) =>
            {
                var participant = await service.DevLoginAsync(request, cancellationToken);
                await httpContext.SignInAsync(
                    QuestAuthConstants.ParticipantScheme,
                    AuthPrincipalFactory.CreateParticipantPrincipal(participant));

                return Results.Ok(ParticipantAuthService.ToResponse(participant));
            });

        group.MapGet(
            "/me",
            async (ParticipantAuthService service, CancellationToken cancellationToken) =>
            {
                ParticipantProfileResponse? participant = await service.GetCurrentAsync(cancellationToken);
                return participant is null ? Results.Unauthorized() : Results.Ok(participant);
            });

        group.MapPost(
            "/logout",
            async (HttpContext httpContext) =>
            {
                await httpContext.SignOutAsync(QuestAuthConstants.ParticipantScheme);
                return Results.NoContent();
            });

        return app;
    }
}

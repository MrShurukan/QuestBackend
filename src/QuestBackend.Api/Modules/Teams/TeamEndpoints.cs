using QuestBackend.Api.Common;
using QuestBackend.Application.Teams;
using QuestBackend.Contracts;

namespace QuestBackend.Api.Modules.Teams;

public static class TeamEndpoints
{
    public static IEndpointRouteBuilder MapTeamEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/teams");

        group.MapGet(
                "/available",
                async (TeamService service, CancellationToken cancellationToken) =>
                    Results.Ok(await service.GetAvailableTeamsAsync(cancellationToken)))
            .RequireAuthorization(ApiPolicies.ParticipantOnly);

        group.MapGet(
                "/me",
                async (TeamService service, CancellationToken cancellationToken) =>
                {
                    TeamSummaryResponse? team = await service.GetMyTeamAsync(cancellationToken);
                    return team is null ? Results.NotFound() : Results.Ok(team);
                })
            .RequireAuthorization(ApiPolicies.ParticipantOnly);

        group.MapPost(
                "/",
                async (CreateTeamRequest request, TeamService service, CancellationToken cancellationToken) =>
                    Results.Ok(await service.CreateTeamAsync(request, cancellationToken)))
            .RequireAuthorization(ApiPolicies.ParticipantOnly);

        group.MapPost(
                "/join",
                async (JoinTeamRequest request, TeamService service, CancellationToken cancellationToken) =>
                    Results.Ok(await service.JoinTeamAsync(request, cancellationToken)))
            .RequireAuthorization(ApiPolicies.ParticipantOnly);

        return app;
    }
}

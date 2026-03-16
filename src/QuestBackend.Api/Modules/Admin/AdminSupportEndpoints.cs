using QuestBackend.Api.Common;
using QuestBackend.Application.Support;
using QuestBackend.Contracts;

namespace QuestBackend.Api.Modules.Admin;

public static class AdminSupportEndpoints
{
    public static IEndpointRouteBuilder MapAdminSupportEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/admin/support")
            .RequireAuthorization(ApiPolicies.AdminOnly);

        group.MapGet(
            "/teams",
            async (SupportService service, CancellationToken cancellationToken) =>
            {
                return Results.Ok(await service.GetTeamsAsync(cancellationToken));
            });

        group.MapGet(
            "/teams/{teamId:guid}",
            async (Guid teamId, SupportService service, CancellationToken cancellationToken) =>
            {
                return Results.Ok(await service.GetTeamDetailsAsync(teamId, cancellationToken));
            });

        group.MapPost(
            "/teams/{teamId:guid}/questions/{questionId:guid}/unlock",
            async (Guid teamId, Guid questionId, TeamQuestionAdjustmentRequest request, SupportService service, CancellationToken cancellationToken) =>
            {
                await service.UnlockQuestionAsync(teamId, questionId, request, cancellationToken);
                return Results.NoContent();
            });

        group.MapPost(
            "/teams/{teamId:guid}/questions/{questionId:guid}/solve",
            async (Guid teamId, Guid questionId, TeamQuestionAdjustmentRequest request, SupportService service, CancellationToken cancellationToken) =>
            {
                await service.MarkQuestionSolvedAsync(teamId, questionId, request, cancellationToken);
                return Results.NoContent();
            });

        group.MapPost(
            "/teams/{teamId:guid}/questions/{questionId:guid}/revoke-reward",
            async (Guid teamId, Guid questionId, TeamQuestionAdjustmentRequest request, SupportService service, CancellationToken cancellationToken) =>
            {
                await service.RevokeQuestionRewardAsync(teamId, questionId, request, cancellationToken);
                return Results.NoContent();
            });

        group.MapPost(
            "/teams/{teamId:guid}/rewards/adjust",
            async (Guid teamId, TeamRewardAdjustmentRequest request, SupportService service, CancellationToken cancellationToken) =>
            {
                await service.AdjustRewardAsync(teamId, request, cancellationToken);
                return Results.NoContent();
            });

        group.MapPost(
            "/teams/{teamId:guid}/members/{membershipId:guid}/remove",
            async (Guid teamId, Guid membershipId, TeamMemberRemovalRequest request, SupportService service, CancellationToken cancellationToken) =>
            {
                await service.RemoveMemberAsync(teamId, membershipId, request, cancellationToken);
                return Results.NoContent();
            });

        return app;
    }
}

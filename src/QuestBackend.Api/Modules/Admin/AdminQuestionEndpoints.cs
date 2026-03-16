using QuestBackend.Api.Common;
using QuestBackend.Application.Admin;
using QuestBackend.Contracts;

namespace QuestBackend.Api.Modules.Admin;

public static class AdminQuestionEndpoints
{
    public static IEndpointRouteBuilder MapAdminQuestionEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/admin/questions")
            .RequireAuthorization(ApiPolicies.AdminOnly);

        group.MapGet(
            "/",
            async (AdminConfigurationService service, CancellationToken cancellationToken) =>
            {
                return Results.Ok(await service.GetQuestionsAsync(cancellationToken));
            });

        group.MapPost(
            "/",
            async (QuestionUpsertRequest request, AdminConfigurationService service, CancellationToken cancellationToken) =>
            {
                return Results.Ok(await service.CreateQuestionAsync(request, cancellationToken));
            });

        group.MapPut(
            "/{id:guid}",
            async (Guid id, QuestionUpsertRequest request, AdminConfigurationService service, CancellationToken cancellationToken) =>
            {
                return Results.Ok(await service.UpdateQuestionAsync(id, request, cancellationToken));
            });

        group.MapPost(
            "/{id:guid}/duplicate",
            async (Guid id, AdminConfigurationService service, CancellationToken cancellationToken) =>
            {
                return Results.Ok(await service.DuplicateQuestionAsync(id, cancellationToken));
            });

        return app;
    }
}

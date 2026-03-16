using QuestBackend.Api.Common;
using QuestBackend.Application.Questions;
using QuestBackend.Contracts;

namespace QuestBackend.Api.Modules.Questions;

public static class QuestionEndpoints
{
    public static IEndpointRouteBuilder MapQuestionEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/questions")
            .RequireAuthorization(ApiPolicies.ParticipantOnly);

        group.MapGet(
            "/known",
            async (QuestionGameService service, CancellationToken cancellationToken) =>
            {
                return Results.Ok(await service.GetKnownQuestionsAsync(cancellationToken));
            });

        group.MapGet(
            "/{questionId:guid}",
            async (Guid questionId, QuestionGameService service, CancellationToken cancellationToken) =>
            {
                return Results.Ok(await service.GetQuestionDetailsAsync(questionId, cancellationToken));
            });

        group.MapPost(
            "/{questionId:guid}/answers",
            async (Guid questionId, SubmitAnswerRequest request, QuestionGameService service, CancellationToken cancellationToken) =>
            {
                return Results.Ok(await service.SubmitAnswerAsync(questionId, request, cancellationToken));
            })
            .RequireRateLimiting(Common.RateLimiting.SubmissionPolicy);

        return app;
    }
}

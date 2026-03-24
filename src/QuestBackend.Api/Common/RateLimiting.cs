using System.Security.Claims;
using System.Threading.RateLimiting;

namespace QuestBackend.Api.Common;

public static class RateLimiting
{
    public const string PublicQrPolicy = "public-qr";
    public const string AuthPolicy = "auth";
    public const string SubmissionPolicy = "submission";

    public const string EnigmaDraftPolicy = "enigma-draft";

    public static IServiceCollection AddQuestRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(
            options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                options.AddPolicy(
                    PublicQrPolicy,
                    httpContext =>
                        RateLimitPartition.GetFixedWindowLimiter(
                            partitionKey: GetRemoteKey(httpContext),
                            factory: _ => new FixedWindowRateLimiterOptions
                            {
                                PermitLimit = 60,
                                Window = TimeSpan.FromMinutes(1),
                                QueueLimit = 0,
                            }));

                options.AddPolicy(
                    AuthPolicy,
                    httpContext =>
                        RateLimitPartition.GetFixedWindowLimiter(
                            partitionKey: GetRemoteKey(httpContext),
                            factory: _ => new FixedWindowRateLimiterOptions
                            {
                                PermitLimit = 20,
                                Window = TimeSpan.FromMinutes(1),
                                QueueLimit = 0,
                            }));

                options.AddPolicy(
                    SubmissionPolicy,
                    httpContext =>
                        RateLimitPartition.GetFixedWindowLimiter(
                            partitionKey: GetParticipantOrRemoteKey(httpContext),
                            factory: _ => new FixedWindowRateLimiterOptions
                            {
                                PermitLimit = 30,
                                Window = TimeSpan.FromMinutes(1),
                                QueueLimit = 0,
                            }));

                options.AddPolicy(
                    EnigmaDraftPolicy,
                    httpContext =>
                        RateLimitPartition.GetFixedWindowLimiter(
                            partitionKey: GetParticipantOrRemoteKey(httpContext),
                            factory: _ => new FixedWindowRateLimiterOptions
                            {
                                PermitLimit = 120,
                                Window = TimeSpan.FromMinutes(1),
                                QueueLimit = 0,
                            }));
            });

        return services;
    }

    private static string GetRemoteKey(HttpContext httpContext) =>
        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    private static string GetParticipantOrRemoteKey(HttpContext httpContext)
    {
        string? participantId = httpContext.User.FindFirstValue(QuestBackend.Application.Shared.QuestAuthConstants.ParticipantIdClaim);
        return !string.IsNullOrWhiteSpace(participantId) ? participantId : GetRemoteKey(httpContext);
    }
}

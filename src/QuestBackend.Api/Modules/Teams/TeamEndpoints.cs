using QuestBackend.Api.Common;
using QuestBackend.Application.Shared;
using QuestBackend.Application.Teams;
using QuestBackend.Contracts;

namespace QuestBackend.Api.Modules.Teams;

public static class TeamEndpoints
{
    private const long MaxFinalPhotoBytes = 25 * 1024 * 1024;

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

        group.MapPut(
                "/me/join-secret",
                async (UpdateTeamJoinSecretRequest request, TeamService service, CancellationToken cancellationToken) =>
                    Results.Ok(await service.UpdateMyTeamJoinSecretAsync(request, cancellationToken)))
            .RequireAuthorization(ApiPolicies.ParticipantOnly);

        group.MapPost(
                "/me/final-task-photo",
                async (
                    HttpContext httpContext,
                    TeamService service,
                    IWebHostEnvironment environment,
                    CancellationToken cancellationToken) =>
                {
                    if (!httpContext.Request.HasFormContentType)
                    {
                        throw new AppException(400, "Ожидается запрос multipart/form-data.");
                    }

                    IFormCollection form = await httpContext.Request.ReadFormAsync(cancellationToken);
                    await service.EnsureFinalTaskPhotoUploadAllowedAsync(cancellationToken);
                    IFormFile? file = form.Files.GetFile("photo");
                    string relativeUrl = await SaveFinalTaskPhotoAsync(file, environment.WebRootPath, cancellationToken);
                    TeamSummaryResponse team = await service.RecordFinalTaskPhotoAsync(relativeUrl, cancellationToken);
                    return Results.Ok(team);
                })
            .RequireAuthorization(ApiPolicies.ParticipantOnly)
            .DisableAntiforgery();

        return app;
    }

    private static async Task<string> SaveFinalTaskPhotoAsync(
        IFormFile? file,
        string? webRootPath,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            throw new AppException(400, "Прикрепите файл изображения.");
        }

        if (file.Length > MaxFinalPhotoBytes)
        {
            throw new AppException(400, "Размер файла не больше 25 МБ.");
        }

        string? extension = MapImageContentTypeToExtension(file.ContentType);
        if (extension is null)
        {
            throw new AppException(400, "Допускаются только JPEG, PNG или WebP.");
        }

        string root = string.IsNullOrEmpty(webRootPath)
            ? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot")
            : webRootPath;

        string dir = Path.Combine(root, "uploads", "team-final");
        Directory.CreateDirectory(dir);

        string fileName = $"{Guid.NewGuid():N}{extension}";
        string physicalPath = Path.Combine(dir, fileName);
        await using (FileStream stream = File.Create(physicalPath))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        return $"/uploads/team-final/{fileName}";
    }

    private static string? MapImageContentTypeToExtension(string contentType)
    {
        return contentType switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/webp" => ".webp",
            _ => null,
        };
    }
}

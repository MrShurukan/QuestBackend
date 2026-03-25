using Microsoft.AspNetCore.Authentication;
using QuestBackend.Api.Common;
using QuestBackend.Application.Participants;
using QuestBackend.Application.Shared;
using QuestBackend.Contracts;
using QuestBackend.Domain.Participants;

namespace QuestBackend.Api.Modules.Participants;

public static class ParticipantAuthEndpoints
{
    private const long MaxAvatarBytes = 25 * 1024 * 1024;

    public static IEndpointRouteBuilder MapParticipantAuthEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/participant/auth")
            .RequireRateLimiting(Common.RateLimiting.AuthPolicy);

        group.MapPost(
            "/register",
            async (
                HttpContext httpContext,
                ParticipantAuthService service,
                IWebHostEnvironment environment,
                CancellationToken cancellationToken) =>
            {
                if (!httpContext.Request.HasFormContentType)
                {
                    throw new AppException(400, "Ожидается запрос multipart/form-data.");
                }

                IFormCollection form = await httpContext.Request.ReadFormAsync(cancellationToken);
                string login = form["login"].ToString();
                string displayName = form["displayName"].ToString();
                string password = form["password"].ToString();
                string consentRaw = form["acceptPersonalDataProcessing"].ToString();
                if (!string.Equals(consentRaw, "true", StringComparison.OrdinalIgnoreCase))
                {
                    throw new AppException(400, "Нужно согласие на обработку персональных данных.");
                }

                IFormFile? avatar = form.Files.GetFile("avatar");

                string? avatarUrl = await TrySaveAvatarAsync(avatar, environment.WebRootPath, cancellationToken);
                ParticipantUser participant = await service.RegisterLocalAsync(login, displayName, password, avatarUrl, cancellationToken);

                await httpContext.SignInAsync(
                    QuestAuthConstants.ParticipantScheme,
                    AuthPrincipalFactory.CreateParticipantPrincipal(participant));

                return Results.Ok(ParticipantAuthService.ToResponse(participant));
            })
            .DisableAntiforgery();

        group.MapPost(
            "/login",
            async (ParticipantLoginRequest request, ParticipantAuthService service, HttpContext httpContext, CancellationToken cancellationToken) =>
            {
                ParticipantUser participant = await service.LoginLocalAsync(request, cancellationToken);
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

    private static async Task<string?> TrySaveAvatarAsync(
        IFormFile? file,
        string? webRootPath,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return null;
        }

        if (file.Length > MaxAvatarBytes)
        {
            throw new AppException(400, "Размер аватара не больше 25 МБ.");
        }

        string? extension = MapImageContentTypeToExtension(file.ContentType);
        if (extension is null)
        {
            throw new AppException(400, "Аватар допускается только в формате JPEG, PNG или WebP.");
        }

        string root = string.IsNullOrEmpty(webRootPath)
            ? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot")
            : webRootPath;

        string dir = Path.Combine(root, "uploads", "avatars");
        Directory.CreateDirectory(dir);

        string fileName = $"{Guid.NewGuid():N}{extension}";
        string physicalPath = Path.Combine(dir, fileName);
        await using (FileStream stream = File.Create(physicalPath))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        return $"/uploads/avatars/{fileName}";
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

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

                string? avatarUrl = await TrySaveOptionalAvatarAsync(avatar, environment.WebRootPath, cancellationToken);
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
                "/avatar",
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
                    IFormFile? file = form.Files.GetFile("avatar");
                    string relativeUrl = await SaveAvatarFileAsync(file, environment.WebRootPath, cancellationToken);
                    ParticipantProfileResponse profile = await service.UpdateAvatarAsync(relativeUrl, cancellationToken);
                    return Results.Ok(profile);
                })
            .RequireAuthorization(ApiPolicies.ParticipantOnly)
            .DisableAntiforgery();

        group.MapPost(
            "/logout",
            async (HttpContext httpContext) =>
            {
                await httpContext.SignOutAsync(QuestAuthConstants.ParticipantScheme);
                return Results.NoContent();
            });

        return app;
    }

    private static async Task<string?> TrySaveOptionalAvatarAsync(
        IFormFile? file,
        string? webRootPath,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return null;
        }

        return await SaveAvatarFileAsync(file, webRootPath, cancellationToken);
    }

    private static async Task<string> SaveAvatarFileAsync(
        IFormFile? file,
        string? webRootPath,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            throw new AppException(400, "Прикрепите файл изображения для аватара.");
        }

        if (file.Length > MaxAvatarBytes)
        {
            throw new AppException(400, "Размер аватара не больше 25 МБ.");
        }

        string? extension = ImageUploadContentTypeMapper.MapUploadToExtension(file.ContentType, file.FileName);
        if (extension is null)
        {
            throw new AppException(400, ImageUploadContentTypeMapper.AllowedFormatsMessage);
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
}

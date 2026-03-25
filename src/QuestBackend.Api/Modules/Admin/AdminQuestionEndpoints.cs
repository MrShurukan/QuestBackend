using QuestBackend.Api.Common;
using QuestBackend.Application.Admin;
using QuestBackend.Application.Shared;
using QuestBackend.Contracts;

namespace QuestBackend.Api.Modules.Admin;

public static class AdminQuestionEndpoints
{
    private const long MaxQuestionImageBytes = 25 * 1024 * 1024;

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

        group.MapPost(
                "/upload-image",
                async (
                    HttpContext httpContext,
                    IWebHostEnvironment environment,
                    CancellationToken cancellationToken) =>
                {
                    if (!httpContext.Request.HasFormContentType)
                    {
                        throw new AppException(400, "Ожидается запрос multipart/form-data.");
                    }

                    IFormCollection form = await httpContext.Request.ReadFormAsync(cancellationToken);
                    IFormFile? file = form.Files.GetFile("image");
                    string imageUrl = await SaveQuestionImageAsync(file, environment.WebRootPath, cancellationToken);
                    return Results.Ok(new QuestionImageUploadResponse(imageUrl));
                })
            .DisableAntiforgery();

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

    private static async Task<string> SaveQuestionImageAsync(
        IFormFile? file,
        string? webRootPath,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            throw new AppException(400, "Прикрепите файл изображения.");
        }

        if (file.Length > MaxQuestionImageBytes)
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

        string dir = Path.Combine(root, "uploads", "questions");
        Directory.CreateDirectory(dir);

        string fileName = $"{Guid.NewGuid():N}{extension}";
        string physicalPath = Path.Combine(dir, fileName);
        await using (FileStream stream = File.Create(physicalPath))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        return $"/uploads/questions/{fileName}";
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

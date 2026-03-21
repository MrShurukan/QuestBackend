using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using QuestBackend.Application.Shared;

namespace QuestBackend.Api.Common;

public sealed class ExceptionHandlingMiddleware
{
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly RequestDelegate _next;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (AppException exception)
        {
            await WriteProblemAsync(context, exception.StatusCode, exception.Message);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unhandled exception for request {Path}", context.Request.Path);
            await WriteProblemAsync(context, StatusCodes.Status500InternalServerError, "Внутренняя ошибка сервера.");
        }
    }

    private static async Task WriteProblemAsync(HttpContext context, int statusCode, string detail)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        object problem = new
        {
            title = TitleRu(statusCode),
            status = statusCode,
            detail,
            traceId = context.TraceIdentifier,
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
    }

    private static string TitleRu(int statusCode) =>
        statusCode switch
        {
            StatusCodes.Status400BadRequest => "Некорректный запрос",
            StatusCodes.Status401Unauthorized => "Требуется авторизация",
            StatusCodes.Status403Forbidden => "Доступ запрещён",
            StatusCodes.Status404NotFound => "Не найдено",
            StatusCodes.Status409Conflict => "Конфликт",
            StatusCodes.Status422UnprocessableEntity => "Не удалось обработать",
            StatusCodes.Status429TooManyRequests => "Слишком много запросов",
            StatusCodes.Status500InternalServerError => "Внутренняя ошибка сервера",
            _ => ReasonPhrases.GetReasonPhrase(statusCode),
        };
}

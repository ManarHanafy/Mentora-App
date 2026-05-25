using Microsoft.AspNetCore.Diagnostics;

namespace api.Errors;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);

        var (statusCode, title, detail, errorCode) = exception switch
        {
            ArgumentException => (
                StatusCodes.Status400BadRequest,
                "Bad Request",
                exception.Message,
                "Request.Invalid"),
            KeyNotFoundException => (
                StatusCodes.Status404NotFound,
                "Not Found",
                exception.Message,
                "Resource.NotFound"),
            UnauthorizedAccessException => (
                StatusCodes.Status401Unauthorized,
                "Unauthorized",
                exception.Message,
                "Auth.Unauthorized"),
            InvalidOperationException => (
                StatusCodes.Status503ServiceUnavailable,
                "Service Unavailable",
                exception.Message,
                "Service.Unavailable"),
            _ => (
                StatusCodes.Status500InternalServerError,
                "Internal Server Error",
                "An unexpected error occurred.",
                "Server.Unexpected")
        };

        var problemDetails = ApiProblemDetailsFactory.Create(
            httpContext,
            statusCode,
            title,
            detail,
            validationErrors:
            [
                new
                {
                    Code = errorCode,
                    Description = detail
                }
            ]);

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}

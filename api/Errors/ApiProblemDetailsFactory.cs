using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;
using api.Abstractions;
using api.Middleware;

namespace api.Errors;

public static class ApiProblemDetailsFactory
{
    public static ProblemDetails Create(
        HttpContext? httpContext,
        int statusCode,
        string? title = null,
        string? detail = null,
        Error? error = null,
        IEnumerable<object>? validationErrors = null)
    {
        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title ?? ReasonPhrases.GetReasonPhrase(statusCode),
            Detail = detail,
            Type = $"https://httpstatuses.com/{statusCode}",
            Instance = httpContext?.Request.Path.Value
        };

        if (httpContext is not null)
        {
            problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;
            if (httpContext.Response.Headers.TryGetValue(CorrelationIdMiddleware.HeaderName, out StringValues correlationId)
                && !StringValues.IsNullOrEmpty(correlationId))
            {
                problemDetails.Extensions["correlationId"] = correlationId.ToString();
            }
        }

        var errors = new List<object>();

        if (error is not null && error != Error.None)
            errors.Add(new { error.Code, error.Description });

        if (validationErrors is not null)
            errors.AddRange(validationErrors);

        if (errors.Count > 0)
            problemDetails.Extensions["errors"] = errors;

        return problemDetails;
    }
}

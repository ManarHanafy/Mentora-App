using api.Errors;

namespace api.Middleware;

public class ApiVersioningPathMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.Headers["api-supported-versions"] = "1.0";
        context.Response.Headers["api-default-version"] = "1.0";

        var path = context.Request.Path.Value;

        if (!string.IsNullOrWhiteSpace(path) && path.StartsWith("/api/v", StringComparison.OrdinalIgnoreCase))
        {
            var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length > 1 && segments[0].Equals("api", StringComparison.OrdinalIgnoreCase))
            {
                var versionSegment = segments[1];
                if (versionSegment.Equals("v1", StringComparison.OrdinalIgnoreCase))
                {
                    var tail = string.Join('/', segments.Skip(2));
                    context.Request.Path = string.IsNullOrWhiteSpace(tail)
                        ? "/api"
                        : $"/api/{tail}";
                }
                else if (versionSegment.Length > 1
                         && versionSegment.StartsWith("v", StringComparison.OrdinalIgnoreCase)
                         && int.TryParse(versionSegment[1..], out _))
                {
                    var problem = ApiProblemDetailsFactory.Create(
                        context,
                        StatusCodes.Status400BadRequest,
                        "Unsupported API version",
                        "Requested API version is not supported.",
                        validationErrors:
                        [
                            new
                            {
                                Code = "ApiVersion.Unsupported",
                                Description = $"Supported versions: v1. Requested: {versionSegment}."
                            }
                        ]);

                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await context.Response.WriteAsJsonAsync(problem);
                    return;
                }
            }
        }

        await next(context);
    }
}

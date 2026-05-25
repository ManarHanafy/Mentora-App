using Microsoft.AspNetCore.Mvc;
using api.Errors;

namespace api.Abstractions;

public static class ResultExtensions
{
    public static ObjectResult ToProblem(this Result result)
    {
        if (result.IsSuccess)
            throw new InvalidOperationException("Cannot convert a success result to a problem.");

        var statusCode  = result.Error.StatusCode ?? StatusCodes.Status500InternalServerError;
        var problemDetails = ApiProblemDetailsFactory.Create(
            httpContext: null,
            statusCode: statusCode,
            title: GetTitleForStatus(statusCode),
            error: result.Error);

        return new ObjectResult(problemDetails) { StatusCode = statusCode };
    }

    private static string GetTitleForStatus(int statusCode) => statusCode switch
    {
        StatusCodes.Status400BadRequest  => "Bad Request",
        StatusCodes.Status401Unauthorized => "Unauthorized",
        StatusCodes.Status403Forbidden   => "Forbidden",
        StatusCodes.Status404NotFound    => "Not Found",
        StatusCodes.Status409Conflict    => "Conflict",
        _                                => "Internal Server Error"
    };
}

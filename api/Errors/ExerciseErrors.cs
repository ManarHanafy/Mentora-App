using api.Abstractions;

namespace api.Errors;

public static class ExerciseErrors
{
    public static readonly Error NotFound = new(
        "Exercise.NotFound",
        "Exercise not found.",
        StatusCodes.Status404NotFound);

    public static readonly Error InvalidParameter = new(
        "Exercise.InvalidParameter",
        "Invalid parameter code. Valid codes: anx, dep, str, slp, soc, cdt, safe, eng.",
        StatusCodes.Status400BadRequest);
}

using Microsoft.AspNetCore.Mvc;

public static class ApiProblemDetailsWriter
{
    public static Task WriteUnauthorizedAsync(HttpContext context)
    {
        return WriteAsync(
            context,
            StatusCodes.Status401Unauthorized,
            "Authentication required",
            "You must sign in before accessing this API resource.",
            "auth.required");
    }

    public static Task WriteForbiddenAsync(HttpContext context)
    {
        return WriteAsync(
            context,
            StatusCodes.Status403Forbidden,
            "Permission denied",
            "You do not have permission to perform this operation.",
            "permission.denied");
    }

    private static Task WriteAsync(
        HttpContext context,
        int statusCode,
        string title,
        string detail,
        string code)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        return context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path,
            Extensions =
            {
                ["code"] = code,
                ["correlationId"] = context.TraceIdentifier,
            },
        });
    }
}

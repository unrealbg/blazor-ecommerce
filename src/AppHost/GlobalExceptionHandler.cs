using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is ValidationException validationException)
        {
            var errors = validationException.Errors
                .GroupBy(error => error.PropertyName)
                .ToDictionary(
                    group => group.Key,
                    group => group
                        .Select(error => error.ErrorMessage)
                        .Distinct()
                        .ToArray());

            var problem = new ValidationProblemDetails(errors)
            {
                Title = "Validation failed",
                Detail = "One or more validation errors occurred.",
                Status = StatusCodes.Status400BadRequest,
                Type = "https://www.rfc-editor.org/rfc/rfc9110#name-400-bad-request",
            };

            problem.Extensions["errorType"] = "validation";
            await WriteProblemAsync(httpContext, problem, cancellationToken);
            return true;
        }

        if (exception is DbUpdateConcurrencyException)
        {
            var problem = new ProblemDetails
            {
                Title = "Concurrency conflict",
                Detail = "The resource was modified by another request. Please retry.",
                Status = StatusCodes.Status409Conflict,
                Type = "https://www.rfc-editor.org/rfc/rfc9110#name-409-conflict",
            };

            problem.Extensions["errorType"] = "concurrency";
            await WriteProblemAsync(httpContext, problem, cancellationToken);
            return true;
        }

        logger.LogError(exception, "Unhandled exception while processing request.");

        var serverErrorProblem = new ProblemDetails
        {
            Title = "Internal server error",
            Detail = "An unexpected error occurred.",
            Status = StatusCodes.Status500InternalServerError,
            Type = "https://www.rfc-editor.org/rfc/rfc9110#name-500-internal-server-error",
        };

        serverErrorProblem.Extensions["errorType"] = "server";
        serverErrorProblem.Extensions["correlationId"] = httpContext.TraceIdentifier;
        await WriteProblemAsync(httpContext, serverErrorProblem, cancellationToken);
        return true;
    }

    private static async Task WriteProblemAsync(
        HttpContext httpContext,
        ProblemDetails problemDetails,
        CancellationToken cancellationToken)
    {
        httpContext.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;
        httpContext.Response.ContentType = "application/problem+json";
        problemDetails.Instance = httpContext.Request.Path;
        problemDetails.Extensions["correlationId"] = httpContext.TraceIdentifier;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
    }
}

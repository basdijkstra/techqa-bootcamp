using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace PixelVault.Shared.Errors;

/// <summary>
/// Maps <see cref="DomainException"/>s thrown anywhere in the request pipeline to problem details responses.
/// </summary>
public sealed class DomainExceptionHandler(IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not DomainException domainException)
        {
            return false;
        }

        ProblemDetails problem = domainException is ValidationException validation
            ? new ValidationProblemDetails(validation.Errors)
            : new ProblemDetails();

        problem.Status = domainException.StatusCode;
        problem.Title = TitleFor(domainException.StatusCode);
        problem.Detail = domainException.Message;
        problem.Extensions["code"] = domainException.Code;
        foreach (var (key, value) in domainException.Details)
        {
            problem.Extensions[key] = value;
        }

        httpContext.Response.StatusCode = domainException.StatusCode;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception
        });
    }

    private static string TitleFor(int statusCode) => statusCode switch
    {
        400 => "Bad Request",
        401 => "Unauthorized",
        402 => "Payment Required",
        403 => "Forbidden",
        404 => "Not Found",
        409 => "Conflict",
        410 => "Gone",
        422 => "Unprocessable Entity",
        423 => "Locked",
        502 => "Bad Gateway",
        503 => "Service Unavailable",
        _ => "Error"
    };
}

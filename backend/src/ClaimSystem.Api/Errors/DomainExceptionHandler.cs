using ClaimSystem.Domain.Common;
using Microsoft.AspNetCore.Diagnostics;

namespace ClaimSystem.Api.Errors;

/// <summary>
/// Turns a <see cref="DomainException"/> into a ProblemDetails response with the right status code.
/// Any other exception falls through to the default handler (500).
/// </summary>
internal sealed class DomainExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not DomainException domainException)
        {
            return false;
        }

        httpContext.Response.StatusCode = domainException.Code switch
        {
            DomainErrors.Validation => StatusCodes.Status400BadRequest,
            DomainErrors.NotAllowed => StatusCodes.Status403Forbidden,
            DomainErrors.NotFound => StatusCodes.Status404NotFound,
            DomainErrors.InvalidTransition or DomainErrors.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError,
        };

        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails =
            {
                Detail = domainException.Message,
                Extensions = { ["code"] = domainException.Code },
            },
        });
    }
}

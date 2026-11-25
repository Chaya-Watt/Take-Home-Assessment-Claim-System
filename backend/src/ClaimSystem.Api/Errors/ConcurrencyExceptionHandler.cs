using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;

namespace ClaimSystem.Api.Errors;

/// <summary>
/// Two people saved the same claim at the same time (the claim's <c>Version</c> no longer matched).
/// The second one gets 409 and should reload the claim and try again.
/// </summary>
internal sealed class ConcurrencyExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not DbUpdateConcurrencyException)
        {
            return false;
        }

        httpContext.Response.StatusCode = StatusCodes.Status409Conflict;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails =
            {
                Detail = "The claim was changed by someone else at the same time. Reload it and try again.",
                Extensions = { ["code"] = "concurrency_conflict" },
            },
        });
    }
}

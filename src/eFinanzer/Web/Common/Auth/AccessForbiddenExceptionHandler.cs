using Microsoft.AspNetCore.Diagnostics;

namespace eFinanzer.Common.Auth;

/// <summary>
/// Turns an <see cref="AccessForbiddenException"/> thrown by the group checks in endpoint handlers into a 403 response
/// </summary>
public class AccessForbiddenExceptionHandler(ILogger<AccessForbiddenExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not AccessForbiddenException accessForbiddenException)
            return false;

        logger.LogWarning("Access to {HttpPath} denied for user {UserId}: {Reason}",
            httpContext.Request.Path.Value,
            httpContext.User.FindFirst("sub")?.Value,
            accessForbiddenException.Message);

        await TypedResults.Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Forbidden",
                detail: accessForbiddenException.Message)
            .ExecuteAsync(httpContext);

        return true;
    }
}

using Microsoft.AspNetCore.Diagnostics;

namespace eFinanzer.Common.Endpoints;

/// <summary>
/// Turns a <see cref="BadHttpRequestException"/> thrown by the parameter binding, e.g. for a body with a missing
/// required property or an unknown enum value, into a response with its status code instead of a 500
/// </summary>
public class BadHttpRequestExceptionHandler(ILogger<BadHttpRequestExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not BadHttpRequestException badHttpRequestException)
            return false;

        // The inner exception tells what is wrong with the body, e.g. which JSON property could not be read
        var detail = badHttpRequestException.InnerException?.Message ?? badHttpRequestException.Message;

        logger.LogWarning("Bad request to {HttpPath}: {Reason}", httpContext.Request.Path.Value, detail);

        await TypedResults.Problem(
                statusCode: badHttpRequestException.StatusCode,
                title: "Bad Request",
                detail: detail)
            .ExecuteAsync(httpContext);

        return true;
    }
}

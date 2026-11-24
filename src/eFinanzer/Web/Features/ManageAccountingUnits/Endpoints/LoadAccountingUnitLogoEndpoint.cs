using System.Diagnostics;
using System.Net.Mime;
using eFinanzer.Common.Endpoints;
using eFinanzer.Common.Results.Errors;
using eFinanzer.Features.ManageAccountingUnits.Tasks.ManageAccountingUnitLogo;
using eFinanzer.Features.ManageAccountingUnits.Tasks.ManageAccountingUnitLogo.Errors;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace eFinanzer.Features.ManageAccountingUnits.Endpoints;

/// <summary>
/// Logos are not confidential, so they can be loaded without logging in
/// </summary>
public class LoadAccountingUnitLogoEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
        => app.MapGet("/manage-accounting-units/load-accounting-unit-logo", Handle)
            .WithName("LoadAccountingUnitLogo")
            .WithTags("ManageAccountingUnits")
            .AllowAnonymous()
            .Produces(StatusCodes.Status200OK, typeof(Stream), MediaTypeNames.Image.Png, MediaTypeNames.Image.Jpeg,
                MediaTypeNames.Image.Svg);

    private static async Task<Results<
        FileStreamHttpResult,
        NotFound<NotFoundError>,
        InternalServerError<LoadAccountingUnitLogoError>>> Handle(
        [FromQuery] string key,
        IManageAccountingUnitLogoTask task,
        HttpResponse response,
        CancellationToken ct)
    {
        var result = await task.Load(key, ct);
        if (result.IsSuccess)
        {
            // SVGs can contain scripts, which would run on our origin if a logo is opened directly instead of being
            // shown in an <img>. The policy blocks them and anything else the image could load.
            response.Headers.ContentSecurityPolicy = "default-src 'none'; style-src 'unsafe-inline'; sandbox";
            response.Headers.XContentTypeOptions = "nosniff";
            return TypedResults.Stream(result.Value.Content, result.Value.ContentType);
        }

        return result.Error switch
        {
            NotFoundError error => TypedResults.NotFound(error),
            LoadAccountingUnitLogoError error => TypedResults.InternalServerError(error),
            _ => throw new UnreachableException($"Unhandled error: {result.Error}")
        };
    }
}

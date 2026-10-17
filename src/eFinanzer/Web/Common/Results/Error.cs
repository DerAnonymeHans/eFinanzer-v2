using System.Net;

namespace eFinanzer.Common.Results;

public abstract record Error(string Message, HttpStatusCode StatusCode = HttpStatusCode.InternalServerError, Error? ParentError = null)
{
    public virtual IResult GetHttpResult() 
        => TypedResults.Json(this, statusCode: (int)StatusCode);
}
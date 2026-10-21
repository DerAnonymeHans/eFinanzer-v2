using eFinanzer.Common.Results.Errors;

namespace eFinanzer.Features.ManageRoleChangeRequests;

/// <summary>
/// Input constraints of a role change request, shared by the create and resolve tasks
/// </summary>
public static class RoleChangeRequestValidation
{
    public const int CommentMaxLength = 500;

    public static Result ValidateComment(string field, string? comment)
    {
        if (comment?.Length > CommentMaxLength)
            return new InvalidInputError(field, $"{field} must not be longer than {CommentMaxLength} characters");

        return Result.Ok();
    }
}

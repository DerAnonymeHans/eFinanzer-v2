using System.Text.RegularExpressions;
using eFinanzer.Common.Results.Errors;

namespace eFinanzer.Features.ManageAccountingUnits;

/// <summary>
/// Input constraints of an accounting unit, shared by the create and update tasks
/// </summary>
public static partial class AccountingUnitValidation
{
    public const int KeyMaxLength = 10;
    public const int NameMaxLength = 50;
    public const int DescriptionMaxLength = 500;

    [GeneratedRegex("^[a-zA-Z0-9_-]+$")]
    private static partial Regex KeyRegex();

    public static Result ValidateKey(string? key)
    {
        if (string.IsNullOrEmpty(key) || key.Length > KeyMaxLength)
            return new InvalidInputError("Key", $"Key must be between 1 and {KeyMaxLength} characters long");

        if (!KeyRegex().IsMatch(key))
            return new InvalidInputError("Key", "Key may only contain the characters a-z, A-Z, 0-9, '_' and '-'");

        return Result.Ok();
    }

    public static Result ValidateName(string? name)
    {
        if (name?.Length > NameMaxLength)
            return new InvalidInputError("Name", $"Name must not be longer than {NameMaxLength} characters");

        return Result.Ok();
    }

    public static Result ValidateDescription(string? description)
    {
        if (description?.Length > DescriptionMaxLength)
            return new InvalidInputError("Description", $"Description must not be longer than {DescriptionMaxLength} characters");

        return Result.Ok();
    }
}

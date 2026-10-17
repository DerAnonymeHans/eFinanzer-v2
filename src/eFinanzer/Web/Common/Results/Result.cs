using System.Diagnostics.CodeAnalysis;

namespace eFinanzer.Common.Results;

/// <summary>
/// Result of an operation without a return value. Either ok or failed with an <see cref="Results.Error"/>.
/// </summary>
public sealed record Result
{
    public Error? Error { get; }

    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsSuccess => Error is null;

    [MemberNotNullWhen(true, nameof(Error))]
    public bool IsError => !IsSuccess;

    private Result(Error? error)
    {
        Error = error;
    }

    public static Result Ok() => new((Error?)null);

    public static Result Fail(Error error) => new(error);

    public TResult Match<TResult>(Func<TResult> onSuccess, Func<Error, TResult> onError)
        => IsSuccess ? onSuccess() : onError(Error);

    public static implicit operator Result(Error error) => Fail(error);
}

/// <summary>
/// Result of an operation returning a <typeparamref name="TValue"/>. Either ok with a value or failed with an
/// <see cref="Results.Error"/>.
/// </summary>
public sealed record Result<TValue>
{
    public TValue? Value { get; }
    public Error? Error { get; }

    [MemberNotNullWhen(true, nameof(Value))]
    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsSuccess => Error is null;

    [MemberNotNullWhen(false, nameof(Value))]
    [MemberNotNullWhen(true, nameof(Error))]
    public bool IsError => !IsSuccess;

    private Result(TValue? value, Error? error)
    {
        Value = value;
        Error = error;
    }

    public static Result<TValue> Ok(TValue value) => new(value, null);

    public static Result<TValue> Fail(Error error) => new(default, error);

    public TResult Match<TResult>(Func<TValue, TResult> onSuccess, Func<Error, TResult> onError)
        => IsSuccess ? onSuccess(Value) : onError(Error);

    public static implicit operator Result<TValue>(TValue value) => Ok(value);

    public static implicit operator Result<TValue>(Error error) => Fail(error);

    /// <summary>
    /// Drops the value, keeping only whether the operation succeeded
    /// </summary>
    public static implicit operator Result(Result<TValue> result)
        => result.IsSuccess ? Result.Ok() : Result.Fail(result.Error);
}

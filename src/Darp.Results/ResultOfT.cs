using System.Diagnostics.Contracts;

namespace Darp.Results;

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

/// <summary> A result which might be in the Success or Error state. Option to attach Metadata as well </summary>
/// <typeparam name="TValue"> The type of the value. </typeparam>
/// <typeparam name="TError"> The type of the error. </typeparam>
[DebuggerDisplay("{IsSuccess ? \"Success: \" + Value : \"Error: \" + Error,nq}")]
public abstract partial class Result<TValue, TError>
{
    internal const string InvalidCaseType = $"Result should either be {nameof(Ok)} or {nameof(Err)}";

    /// <summary> The optional metadata </summary>
    public IReadOnlyDictionary<string, object> Metadata { get; }

    /// <summary> Initializes a new instance of the <see cref="Result{TValue, TError}"/> class. </summary>
    /// <param name="metadata"> The metadata of the result. </param>
    private Result(IReadOnlyDictionary<string, object> metadata)
    {
        if (this is not (Ok or Err))
        {
            throw new InvalidOperationException(
                $"Result has to extend either {nameof(Ok)} or {nameof(Err)}. No custom inheritance is allowed"
            );
        }
        Metadata = metadata;
    }

    public bool IsOk => this is Ok;
    public bool IsErr => this is Err;

    /// <summary> Tries to get the value of the result. </summary>
    /// <param name="value"> The underlying value of the result. </param>
    /// <returns> True if the result is a success, false otherwise. </returns>
    public bool TryGetValue([NotNullWhen(true)] out TValue? value)
    {
        switch (this)
        {
            case Ok ok:
                value = ok.Value!;
                return true;
            case Err:
                value = default;
                return false;
            default:
                throw new UnreachableException(InvalidCaseType);
        }
    }

    /// <summary> Tries to get the value of the result or the error. </summary>
    /// <param name="value"> The underlying value of the result. </param>
    /// <param name="error"> The error of the result. </param>
    /// <returns> True if the result is a success, false otherwise. </returns>
    public bool TryGetValue([NotNullWhen(true)] out TValue? value, [NotNullWhen(false)] out Err? error)
    {
        switch (this)
        {
            case Ok ok:
                value = ok.Value!;
                error = null;
                return true;
            case Err err:
                value = default;
                error = err;
                return false;
            default:
                throw new UnreachableException(InvalidCaseType);
        }
    }

    /// <summary> Tries to get the value of the result or the error. A new type can be provided to allow conversion to a different type. </summary>
    /// <param name="value"> The underlying value of the result. </param>
    /// <param name="error"> The error of the result. </param>
    /// <typeparam name="TNewValue"> The type of the new value. </typeparam>
    /// <returns> True if the result is a success, false otherwise. </returns>
    public bool TryGetValue<TNewValue>(
        [NotNullWhen(true)] out TValue? value,
        [NotNullWhen(false)] out Result<TNewValue, TError>.Err? error
    )
    {
        switch (this)
        {
            case Ok ok:
                value = ok.Value!;
                error = null;
                return true;
            case Err err:
                value = default;
                error = err.As<TNewValue>();
                return false;
            default:
                throw new UnreachableException(InvalidCaseType);
        }
    }

    public bool TryGetError([NotNullWhen(true)] out TError? error)
    {
        if (this is Err err)
        {
            error = err.Error!;
            return true;
        }
        error = default;
        return false;
    }

    public bool TryGetError([NotNullWhen(true)] out TError? error, [NotNullWhen(false)] out Ok? success)
    {
        switch (this)
        {
            case Ok ok:
                error = default!;
                success = ok;
                return false;
            case Err err:
                error = err.Error!;
                success = null;
                return true;
            default:
                throw new UnreachableException(InvalidCaseType);
        }
    }

    /// <seealso href="https://doc.rust-lang.org/std/result/enum.Result.html#method.unwrap"/>
    [Pure]
    public TValue Unwrap() =>
        TryGetValue(out TValue? value)
            ? value
            : throw new InvalidOperationException("Could not unwrap value. Result is not a success");

    /// <seealso href="https://doc.rust-lang.org/std/result/enum.Result.html#method.unwrap_err"/>
    [Pure]
    public TError UnwrapError() =>
        TryGetError(out TError? error)
            ? error
            : throw new InvalidOperationException("Could not unwrap error. Result is not a error");

    /// <seealso href="https://doc.rust-lang.org/std/result/enum.Result.html#method.expect"/>
    [Pure]
    public TValue Expect(string message)
    {
        if (this is not Ok ok)
            throw new InvalidOperationException(message);
        return ok.Value;
    }

    /// <seealso href="https://doc.rust-lang.org/std/result/enum.Result.html#method.expect_err"/>
    [Pure]
    public TError ExpectError(string message)
    {
        if (this is not Err err)
            throw new InvalidOperationException(message);
        return err.Error;
    }

    public static implicit operator Result<TValue, TError>(TValue value) => Result.Ok<TValue, TError>(value);

    public static implicit operator Result<TValue, TError>(TError error) => Result.Error<TValue, TError>(error);

    /// <seealso href="https://doc.rust-lang.org/std/result/enum.Result.html#method.iter"/>
    [Pure]
    public IEnumerator<TValue> GetEnumerator() => AsEnumerable().GetEnumerator();

    /// <seealso href="https://doc.rust-lang.org/std/result/enum.Result.html#method.iter"/>
    [Pure]
    public IEnumerable<TValue> AsEnumerable()
    {
        if (this is not Ok ok)
            yield break;
        yield return ok.Value;
    }
}

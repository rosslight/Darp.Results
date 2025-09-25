using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Runtime.InteropServices.JavaScript;
using static Darp.Results.Result;

namespace Darp.Results;

partial class Result<TValue, TError>
{
    /// <summary> Tries to get the value of the result. </summary>
    /// <param name="value"> The underlying value of the result. </param>
    /// <returns> True if the result is a success, false otherwise. </returns>
    public bool TryGetValue([MaybeNullWhen(false)] out TValue value)
    {
        switch (this)
        {
            case Ok<TValue, TError> ok:
                value = ok.Value;
                return true;
            case Err<TValue, TError>:
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
    public bool TryGetValue(
        [MaybeNullWhen(false)] out TValue value,
        [MaybeNullWhen(true)] out Err<TValue, TError> error
    )
    {
        switch (this)
        {
            case Ok<TValue, TError> ok:
                value = ok.Value;
                error = null;
                return true;
            case Err<TValue, TError> err:
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
        [MaybeNullWhen(false)] out TValue value,
        [MaybeNullWhen(true)] out Err<TNewValue, TError> error
    )
    {
        switch (this)
        {
            case Ok<TValue, TError> ok:
                value = ok.Value;
                error = null;
                return true;
            case Err<TValue, TError> err:
                value = default;
                error = err.As<TNewValue>();
                return false;
            default:
                throw new UnreachableException(InvalidCaseType);
        }
    }

    /// <summary> Tries to get the error of the result. </summary>
    /// <param name="error"> The error of the result. </param>
    /// <returns> True if the result is a failure, false otherwise. </returns>
    public bool TryGetError([MaybeNullWhen(false)] out TError error)
    {
        if (this is Err<TValue, TError> err)
        {
            error = err.Error;
            return true;
        }
        error = default;
        return false;
    }

    /// <summary> Tries to get the error of the result. </summary>
    /// <param name="error"> The error of the result. </param>
    /// <param name="success"> The success of the result. </param>
    /// <returns> True if the result is a failure, false otherwise. </returns>
    public bool TryGetError(
        [MaybeNullWhen(false)] out TError error,
        [MaybeNullWhen(true)] out Ok<TValue, TError> success
    )
    {
        switch (this)
        {
            case Ok<TValue, TError> ok:
                error = default;
                success = ok;
                return false;
            case Err<TValue, TError> err:
                error = err.Error;
                success = null;
                return true;
            default:
                throw new UnreachableException(InvalidCaseType);
        }
    }

    /// <summary> Tries to get the error of the result. </summary>
    /// <param name="error"> The error of the result. </param>
    /// <param name="success"> The success of the result. </param>
    /// <typeparam name="TNewError"> The type of the new error. </typeparam>
    /// <returns> True if the result is a failure, false otherwise. </returns>
    public bool TryGetError<TNewError>(
        [MaybeNullWhen(false)] out TError error,
        [MaybeNullWhen(true)] out Ok<TValue, TNewError> success
    )
    {
        switch (this)
        {
            case Ok<TValue, TError> ok:
                error = default;
                success = ok.As<TNewError>();
                return false;
            case Err<TValue, TError> err:
                error = err.Error;
                success = null;
                return true;
            default:
                throw new UnreachableException(InvalidCaseType);
        }
    }

    /// <summary> Returns the <see cref="Result.Ok{TValue,TError}.Value"/>. Prefer non-throwing alternatives </summary>
    /// <returns> The underlying value, if in <see cref="Result.Ok{TValue,TError}"/> state </returns>
    /// <exception cref="InvalidOperationException"> An exception, if in <see cref="Result.Err{TValue,TError}"/> state </exception>
    /// <seealso href="https://doc.rust-lang.org/std/result/enum.Result.html#method.unwrap"/>
    [Pure]
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public TValue Unwrap() => Expect("Could not unwrap value. Result is not a success");

    /// <summary> Returns the <see cref="Result.Ok{TValue,TError}.Value"/> or a default value if the result is an error. </summary>
    /// <param name="defaultValue"> The default value to return if the result is an error. </param>
    /// <returns> The underlying value, if in <see cref="Result.Ok{TValue,TError}"/> state, or the default value if in <see cref="Result.Err{TValue,TError}"/> state </returns>
    /// <seealso href="https://doc.rust-lang.org/std/result/enum.Result.html#method.unwrap_or"/>
    [Pure]
    public TValue Unwrap(TValue defaultValue) => TryGetValue(out TValue? value) ? value : defaultValue;

    /// <summary> Returns the <see cref="Result.Ok{TValue,TError}.Value"/> or the default value if the result is an error. </summary>
    /// <returns> The underlying value, if in <see cref="Result.Ok{TValue,TError}"/> state, or the default value if in <see cref="Result.Err{TValue,TError}"/> state </returns>
    /// <seealso href="https://doc.rust-lang.org/std/result/enum.Result.html#method.unwrap_or_default"/>
    [Pure]
    public TValue? UnwrapOrDefault() => TryGetValue(out TValue? value) ? value : default;

    /// <summary> Returns the <see cref="Result.Ok{TValue,TError}.Value"/> or a default value created by the provider if the result is an error. </summary>
    /// <param name="valueProvider"> The default value provider to create a return value if the result is an error. </param>
    /// <returns> The underlying value, if in <see cref="Result.Ok{TValue,TError}"/> state, or the default value if in <see cref="Result.Err{TValue,TError}"/> state </returns>
    /// <seealso href="https://doc.rust-lang.org/std/result/enum.Result.html#method.unwrap_or_else"/>
    [Pure]
    public TValue Unwrap(Func<TError, TValue> valueProvider)
    {
        ArgumentNullException.ThrowIfNull(valueProvider);
        return TryGetValue(out TValue? value, out Err<TValue, TError>? err) ? value : valueProvider(err.Error);
    }

    /// <summary> Returns the <see cref="JSType.Error"/> </summary>
    /// <returns> The underlying error, if in <see cref="Result.Err{TValue,TError}"/> state </returns>
    /// <exception cref="InvalidOperationException"> An exception, if in <see cref="Result.Ok{TValue,TError}"/> state </exception>
    /// <seealso href="https://doc.rust-lang.org/std/result/enum.Result.html#method.unwrap_err"/>
    [Pure]
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public TError UnwrapError() => ExpectError("Could not unwrap error. Result is not a error");

    /// <summary> Returns the <see cref="Result.Ok{TValue,TError}.Value"/> with a custom message. Prefer non-throwing alternatives </summary>
    /// <returns> The underlying value, if in <see cref="Result.Ok{TValue,TError}"/> state </returns>
    /// <exception cref="InvalidOperationException"> An exception, if in <see cref="Result.Err{TValue,TError}"/> state </exception>
    /// <seealso href="https://doc.rust-lang.org/std/result/enum.Result.html#method.expect"/>
    [Pure]
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public TValue Expect(string message) =>
        TryGetValue(out TValue? value) ? value : throw new InvalidOperationException(message);

    /// <summary> Returns the <see cref="JSType.Error"/> with a custom message </summary>
    /// <returns> The underlying error, if in <see cref="Result.Err{TValue,TError}"/> state </returns>
    /// <exception cref="InvalidOperationException"> An exception, if in <see cref="Result.Ok{TValue,TError}"/> state </exception>
    /// <seealso href="https://doc.rust-lang.org/std/result/enum.Result.html#method.expect_err"/>
    [Pure]
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public TError ExpectError(string message) =>
        TryGetError(out TError? error) ? error : throw new InvalidOperationException(message);
}

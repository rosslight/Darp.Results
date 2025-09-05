using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;

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
            case Ok ok:
                value = ok.Value;
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
    public bool TryGetValue([MaybeNullWhen(false)] out TValue value, [MaybeNullWhen(true)] out Err error)
    {
        switch (this)
        {
            case Ok ok:
                value = ok.Value;
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
        [MaybeNullWhen(false)] out TValue value,
        [MaybeNullWhen(true)] out Result<TNewValue, TError>.Err error
    )
    {
        switch (this)
        {
            case Ok ok:
                value = ok.Value;
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

    public bool TryGetError([MaybeNullWhen(false)] out TError error)
    {
        if (this is Err err)
        {
            error = err.Error;
            return true;
        }
        error = default;
        return false;
    }

    public bool TryGetError([MaybeNullWhen(false)] out TError error, [MaybeNullWhen(true)] out Ok success)
    {
        switch (this)
        {
            case Ok ok:
                error = default;
                success = ok;
                return false;
            case Err err:
                error = err.Error;
                success = null;
                return true;
            default:
                throw new UnreachableException(InvalidCaseType);
        }
    }

    public bool TryGetError<TNewError>(
        [MaybeNullWhen(false)] out TError error,
        [MaybeNullWhen(true)] out Result<TValue, TNewError>.Ok success
    )
    {
        switch (this)
        {
            case Ok ok:
                error = default;
                success = ok.As<TNewError>();
                return false;
            case Err err:
                error = err.Error;
                success = null;
                return true;
            default:
                throw new UnreachableException(InvalidCaseType);
        }
    }

    /// <summary> Returns the <see cref="Ok.Value"/>. Prefer non-throwing alternatives </summary>
    /// <returns> The underlying value, if in <see cref="Ok"/> state </returns>
    /// <exception cref="InvalidOperationException"> An exception, if in <see cref="Err"/> state </exception>
    /// <seealso href="https://doc.rust-lang.org/std/result/enum.Result.html#method.unwrap"/>
    [Pure]
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public TValue Unwrap() => Expect("Could not unwrap value. Result is not a success");

    /// <summary> Returns the <see cref="Ok.Value"/> with a custom message. Prefer non-throwing alternatives </summary>
    /// <returns> The underlying value, if in <see cref="Ok"/> state </returns>
    /// <exception cref="InvalidOperationException"> An exception, if in <see cref="Err"/> state </exception>
    /// <seealso href="https://doc.rust-lang.org/std/result/enum.Result.html#method.expect"/>
    [Pure]
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public TValue Expect(string message)
    {
        return this is not Ok ok ? throw new InvalidOperationException(message) : ok.Value;
    }

    /// <summary> Returns the <see cref="Ok.Value"/> or a default value if the result is an error. </summary>
    /// <param name="defaultValue"> The default value to return if the result is an error. </param>
    /// <returns> The underlying value, if in <see cref="Ok"/> state, or the default value if in <see cref="Err"/> state </returns>
    /// <seealso href="https://doc.rust-lang.org/std/result/enum.Result.html#method.unwrap_or"/>
    [Pure]
    public TValue Unwrap(TValue defaultValue) => TryGetValue(out TValue? value) ? value : defaultValue;

    /// <summary> Returns the <see cref="Ok.Value"/> or a default value created by the provider if the result is an error. </summary>
    /// <param name="valueProvider"> The default value provider to create a return value if the result is an error. </param>
    /// <returns> The underlying value, if in <see cref="Ok"/> state, or the default value if in <see cref="Err"/> state </returns>
    /// <seealso href="https://doc.rust-lang.org/std/result/enum.Result.html#method.unwrap_or_else"/>
    [Pure]
    public TValue Unwrap(Func<TValue> valueProvider) => TryGetValue(out TValue? value) ? value : valueProvider();

    /// <summary> Returns the <see cref="Err.Error"/> </summary>
    /// <returns> The underlying error, if in <see cref="Err"/> state </returns>
    /// <exception cref="InvalidOperationException"> An exception, if in <see cref="Ok"/> state </exception>
    /// <seealso href="https://doc.rust-lang.org/std/result/enum.Result.html#method.unwrap_err"/>
    [Pure]
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public TError UnwrapError() => ExpectError("Could not unwrap error. Result is not a error");

    /// <summary> Returns the <see cref="Err.Error"/> with a custom message </summary>
    /// <returns> The underlying error, if in <see cref="Err"/> state </returns>
    /// <exception cref="InvalidOperationException"> An exception, if in <see cref="Ok"/> state </exception>
    /// <seealso href="https://doc.rust-lang.org/std/result/enum.Result.html#method.expect_err"/>
    [Pure]
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public TError ExpectError(string message)
    {
        return this is not Err err ? throw new InvalidOperationException(message) : err.Error;
    }
}

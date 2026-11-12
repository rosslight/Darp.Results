using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;

namespace Darp.Results;

partial struct Result<TValue, TError>
{
    /// <summary> Tries to get the value of the result. </summary>
    /// <param name="value"> The underlying value of the result. </param>
    /// <returns> True if the result is a success, false otherwise. </returns>
    public bool TryGetValue([MaybeNullWhen(false)] out TValue value)
    {
        value = _value;
        return IsOk;
    }

    /// <summary> Tries to get the value of the result or the error. </summary>
    /// <param name="value"> The underlying value of the result. </param>
    /// <param name="error"> The error case with its metadata. It converts into any result with the same error type. </param>
    /// <returns> True if the result is a success, false otherwise. </returns>
    /// <remarks> Throws <see cref="InvalidOperationException"/> for <c>default(Result)</c>. </remarks>
    public bool TryGetValue([MaybeNullWhen(false)] out TValue value, out Err<TError> error)
    {
        switch (_state)
        {
            case ResultState.Ok:
                value = _value;
                error = default;
                return true;
            case ResultState.Err:
                value = default;
                error = ErrCase;
                return false;
            default:
                throw Uninitialized();
        }
    }

    /// <summary> Tries to get the error of the result. </summary>
    /// <param name="error"> The error of the result. </param>
    /// <returns> True if the result is a failure, false otherwise. </returns>
    public bool TryGetError([MaybeNullWhen(false)] out TError error)
    {
        error = _error;
        return IsErr;
    }

    /// <summary> Tries to get the error of the result or the value. </summary>
    /// <param name="error"> The error of the result. </param>
    /// <param name="success"> The success case with its metadata. It converts into any result with the same value type. </param>
    /// <returns> True if the result is a failure, false otherwise. </returns>
    /// <remarks> Throws <see cref="InvalidOperationException"/> for <c>default(Result)</c>. </remarks>
    public bool TryGetError([MaybeNullWhen(false)] out TError error, out Ok<TValue> success)
    {
        switch (_state)
        {
            case ResultState.Ok:
                error = default;
                success = OkCase;
                return false;
            case ResultState.Err:
                error = _error;
                success = default;
                return true;
            default:
                throw Uninitialized();
        }
    }

    /// <summary> Returns the value. Prefer non-throwing alternatives </summary>
    /// <returns> The underlying value, if in <see cref="Ok{TValue}"/> state </returns>
    /// <exception cref="InvalidOperationException"> An exception, if in <see cref="Err{TError}"/> state </exception>
    /// <seealso href="https://doc.rust-lang.org/std/result/enum.Result.html#method.unwrap"/>
    [Pure]
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public TValue Unwrap() => Expect("Called Result.Unwrap() on an Err value");

    /// <summary> Returns the value or a default value if the result is an error. </summary>
    /// <param name="defaultValue"> The default value to return if the result is an error. </param>
    /// <returns> The underlying value, or the default value if in <see cref="Err{TError}"/> state </returns>
    /// <seealso href="https://doc.rust-lang.org/std/result/enum.Result.html#method.unwrap_or"/>
    [Pure]
    public TValue Unwrap(TValue defaultValue) => TryGetValue(out TValue? value) ? value : defaultValue;

    /// <summary> Returns the value or the default value if the result is an error. </summary>
    /// <returns> The underlying value, or the default value if in <see cref="Err{TError}"/> state </returns>
    /// <seealso href="https://doc.rust-lang.org/std/result/enum.Result.html#method.unwrap_or_default"/>
    [Pure]
    public TValue? UnwrapOrDefault() => TryGetValue(out TValue? value) ? value : default;

    /// <summary> Returns the value or a value created by the provider if the result is an error. </summary>
    /// <param name="valueProvider"> Creates a return value from the error. </param>
    /// <returns> The underlying value, or the provided value if in <see cref="Err{TError}"/> state </returns>
    /// <seealso href="https://doc.rust-lang.org/std/result/enum.Result.html#method.unwrap_or_else"/>
    [Pure]
    public TValue Unwrap(Func<TError, TValue> valueProvider)
    {
        ArgumentNullException.ThrowIfNull(valueProvider);
        return TryGetValue(out TValue? value, out Err<TError> err) ? value : valueProvider(err.Error);
    }

    /// <summary> Returns the error </summary>
    /// <returns> The underlying error, if in <see cref="Err{TError}"/> state </returns>
    /// <exception cref="InvalidOperationException"> An exception, if in <see cref="Ok{TValue}"/> state </exception>
    /// <seealso href="https://doc.rust-lang.org/std/result/enum.Result.html#method.unwrap_err"/>
    [Pure]
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public TError UnwrapError() => ExpectError("Called Result.UnwrapError() on an Ok value");

    /// <summary> Returns the value with a custom message. Prefer non-throwing alternatives </summary>
    /// <param name="message"> The message of the exception. </param>
    /// <returns> The underlying value, if in <see cref="Ok{TValue}"/> state </returns>
    /// <exception cref="InvalidOperationException"> An exception, if in <see cref="Err{TError}"/> state </exception>
    /// <seealso href="https://doc.rust-lang.org/std/result/enum.Result.html#method.expect"/>
    [Pure]
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public TValue Expect(string message) =>
        TryGetValue(out TValue? value, out Err<TError> err)
            ? value
            : throw CreateInvalidOperationException(message, err.Error);

    /// <summary> Returns the error with a custom message </summary>
    /// <param name="message"> The message of the exception. </param>
    /// <returns> The underlying error, if in <see cref="Err{TError}"/> state </returns>
    /// <exception cref="InvalidOperationException"> An exception, if in <see cref="Ok{TValue}"/> state </exception>
    /// <seealso href="https://doc.rust-lang.org/std/result/enum.Result.html#method.expect_err"/>
    [Pure]
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public TError ExpectError(string message) =>
        TryGetError(out TError? error, out Ok<TValue> ok)
            ? error
            : throw CreateInvalidOperationException(message, ok.Value);

    private static InvalidOperationException CreateInvalidOperationException<TUnexpected>(
        string message,
        TUnexpected unexpected
    ) => new($"{message}: {FormatUnexpectedValue(unexpected)}");

    private static string FormatUnexpectedValue<TUnexpected>(TUnexpected unexpected)
    {
        if (unexpected is null)
            return "<null>";
        try
        {
            return unexpected.ToString() ?? "<null>";
        }
        catch (Exception ex)
        {
            return $"<{unexpected.GetType().FullName}.ToString() threw {ex.GetType().FullName}>";
        }
    }
}

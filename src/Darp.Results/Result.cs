namespace Darp.Results;

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

/// <summary> A result which might be in the Success or Error state. Option to attach Metadata as well </summary>
/// <typeparam name="TValue"></typeparam>
/// <typeparam name="TError"></typeparam>
[DebuggerDisplay("{IsSuccess ? \"Success: \" + Value : \"Error: \" + Error,nq}")]
public sealed class Result<TValue, TError>
{
    private readonly TValue? _value;
    private readonly TError? _error;

    /// <summary> True, if the result is in the 'success' state. False otherwise </summary>
    public bool IsSuccess { get; }

    /// <summary> True, if the result is in the 'error' state. False otherwise </summary>
    public bool IsError => !IsSuccess;

    /// <summary> The value of the result </summary>
    /// <exception cref="InvalidOperationException"> Thrown if the result is in the 'error' state </exception>
    public TValue Value =>
        IsSuccess ? _value! : throw new InvalidOperationException("Result is not in success state");

    /// <summary> The value of the result or the default if the result is in error state </summary>
    // ReSharper disable once ConvertToAutoPropertyWhenPossible
    public TValue? ValueOrDefault => _value;

    /// <summary> The error of the result </summary>
    /// <exception cref="InvalidOperationException"> Thrown if the result is in the 'success' state </exception>
    public TError Error =>
        !IsSuccess ? _error! : throw new InvalidOperationException("Result is not in error state");

    /// <summary> The optional metadata </summary>
    public IReadOnlyDictionary<string, object> Metadata { get; }

    private Result(
        bool isSuccess,
        TValue? value,
        TError? error,
        IReadOnlyDictionary<string, object> metadata
    )
    {
        (IsSuccess, _value, _error, Metadata) = (isSuccess, value, error, metadata);
    }

    internal static Result<TValue, TError> OkUnsafe(
        TValue value,
        IReadOnlyDictionary<string, object> metadata
    ) => new(true, value, default, metadata);

    internal static Result<TValue, TError> ErrorUnsafe(
        TError error,
        IReadOnlyDictionary<string, object> metadata
    ) => new(false, default, error, metadata);

    /// <summary>
    /// Maps the result value to the <typeparamref name="TNewValue"/> by applying a function to a contained Ok value, leaving an Error value untouched.
    /// </summary>
    /// <param name="func"> The function to transform the value </param>
    /// <typeparam name="TNewValue"> The type of the new value </typeparam>
    /// <returns> The result with a new value or the existing error </returns>
    /// <seealso href="https://doc.rust-lang.org/std/result/enum.Result.html#method.map"/>
    public Result<TNewValue, TError> Map<TNewValue>(Func<TValue, TNewValue> func)
    {
        ArgumentNullException.ThrowIfNull(func);
        if (IsSuccess)
            return Result<TNewValue, TError>.OkUnsafe(func(Value), Metadata);
        return Result<TNewValue, TError>.ErrorUnsafe(Error, Metadata);
    }

    /// <summary>
    /// Returns the provided <paramref name="defaultValue"/> (if Error), or applies the <paramref name="func"/> to the contained value (if Ok).
    /// </summary>
    /// <param name="func"> The function to transform the value </param>
    /// <param name="defaultValue"> The default value if the result has an error </param>
    /// <typeparam name="TNewValue"> The type of the new value </typeparam>
    /// <returns> The mapped value or default </returns>
    public TNewValue MapOr<TNewValue>(Func<TValue, TNewValue> func, TNewValue defaultValue)
    {
        ArgumentNullException.ThrowIfNull(func);
        return TryGetValue(out var value) ? func(value) : defaultValue;
    }

    /// <summary>
    /// Maps the result error to the <typeparamref name="TNewError"/> by applying a function to a contained error, leaving a value untouched.
    /// </summary>
    /// <param name="func"> The function to transform the error </param>
    /// <typeparam name="TNewError"> The type of the error </typeparam>
    /// <returns> The result with a new error or the existing value </returns>
    public Result<TValue, TNewError> MapError<TNewError>(Func<TError, TNewError> func)
    {
        ArgumentNullException.ThrowIfNull(func);
        if (IsSuccess)
            return Result<TValue, TNewError>.OkUnsafe(Value, Metadata);
        return Result<TValue, TNewError>.ErrorUnsafe(func(Error), Metadata);
    }

    public bool TryGetValue([NotNullWhen(true)] out TValue? value)
    {
        value = _value;
        return IsSuccess;
    }

    public bool TryGetValue<TNewValue>(
        [NotNullWhen(true)] out TValue? value,
        [NotNullWhen(false)] out Result<TNewValue, TError>? failedResult
    )
    {
        if (IsSuccess)
        {
            value = _value!;
            failedResult = null;
            return true;
        }
        value = default;
        failedResult = Result<TNewValue, TError>.ErrorUnsafe(Error, Metadata);
        return false;
    }

    public bool TryGetError([NotNullWhen(true)] out TError? error)
    {
        error = _error;
        return IsError;
    }

    public TValue Expect(string message)
    {
        if (IsError)
            throw new InvalidOperationException(message);
        return Value;
    }

    public TError ExpectError(string message)
    {
        if (IsSuccess)
            throw new InvalidOperationException(message);
        return Error;
    }

    public Result<TNewValue, TError> And<TNewValue>(Result<TNewValue, TError> result)
    {
        if (IsError)
            return Result<TNewValue, TError>.ErrorUnsafe(Error, Metadata);
        return result;
    }

    public Result<TNewValue, TError> And<TNewValue>(Func<Result<TNewValue, TError>> lazyResult)
    {
        ArgumentNullException.ThrowIfNull(lazyResult);
        if (IsError)
            return Result<TNewValue, TError>.ErrorUnsafe(Error, Metadata);
        return lazyResult();
    }

    public Result<TValue, TError> Or(Result<TValue, TError> result)
    {
        if (IsSuccess)
            return this;
        return result;
    }

    public Result<TValue, TError> Or(Func<Result<TValue, TError>> lazyResult)
    {
        ArgumentNullException.ThrowIfNull(lazyResult);
        if (IsSuccess)
            return this;
        return lazyResult();
    }

    public static implicit operator Result<TValue, TError>(TValue value) =>
        Result.Ok<TValue, TError>(value);

    public static implicit operator Result<TValue, TError>(TError error) =>
        Result.Error<TValue, TError>(error);

    public IEnumerator<TValue> GetEnumerator() => AsEnumerable().GetEnumerator();

    public IEnumerable<TValue> AsEnumerable()
    {
        if (IsError)
            yield break;
        yield return Value;
    }

    public Result<TValue, TError> WithMetadata(string key, object value)
    {
        var newMetadata = new ReadOnlyDictionary<string, object>(
            new Dictionary<string, object>(Metadata) { [key] = value }
        );
        if (IsSuccess)
            return OkUnsafe(Value, newMetadata);
        return ErrorUnsafe(Error, newMetadata);
    }

    public Result<TValue, TError> WithMetadata(IDictionary<string, object> metadata)
    {
        var newMetadata = new ReadOnlyDictionary<string, object>(
            Metadata.Concat(metadata).ToDictionary()
        );
        if (IsSuccess)
            return OkUnsafe(Value, newMetadata);
        return ErrorUnsafe(Error, newMetadata);
    }

    public void Deconstruct(out bool isSuccess, out TValue? value, out TError? error)
    {
        (isSuccess, value, error) = (IsSuccess, _value, _error);
    }

    public void Deconstruct(out bool isSuccess, out TValue? value)
    {
        (isSuccess, value) = (IsSuccess, _value);
    }
}

public static class Result
{
    public static Result<TValue, TError> Ok<TValue, TError>(
        TValue value,
        IDictionary<string, object>? metadata = null
    )
    {
        return Result<TValue, TError>.OkUnsafe(
            value,
            metadata is null
                ? ReadOnlyDictionary<string, object>.Empty
                : new ReadOnlyDictionary<string, object>(metadata)
        );
    }

    public static Result<TValue, TError> Error<TValue, TError>(
        TError error,
        IDictionary<string, object>? metadata = null
    )
    {
        return Result<TValue, TError>.ErrorUnsafe(
            error,
            metadata is null
                ? ReadOnlyDictionary<string, object>.Empty
                : new ReadOnlyDictionary<string, object>(metadata)
        );
    }

    public static Result<TValue, TError> Flatten<TValue, TError>(
        Result<Result<TValue, TError>, TError> result
    )
    {
        ArgumentNullException.ThrowIfNull(result);
        if (result.TryGetValue(out Result<TValue, TError>? value))
            return value;
        return result.Error;
    }

    public delegate bool TryParseFunc<in TIn, TOut>(TIn input, out TOut output);

    public static Result<TOut, StandardError> From<TIn, TOut>(
        TIn input,
        TryParseFunc<TIn, TOut> tryParse
    )
    {
        try
        {
            if (tryParse(input, out var output))
                return output;
            return StandardError.TryPatternFailed;
        }
        catch (Exception)
        {
            return StandardError.ExceptionOccured;
        }
    }

    public static Result<TValue, Exception> Try<TValue>(Func<TValue> func)
    {
        try
        {
            return func();
        }
        catch (Exception e)
        {
            return e;
        }
    }
}

public enum StandardError
{
    TryPatternFailed,
    ExceptionOccured,
}

namespace Darp.Results;

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

/// <summary> A result which might be in the Success or Error state. Option to attach Metadata as well </summary>
/// <typeparam name="TValue"></typeparam>
/// <typeparam name="TError"></typeparam>
[DebuggerDisplay("{IsSuccess ? \"Success: \" + Value : \"Error: \" + Error,nq}")]
public abstract class Result<TValue, TError>
{
    public sealed class Ok : Result<TValue, TError>
    {
        public new TValue Value { get; }

        internal Ok(TValue value, IReadOnlyDictionary<string, object> metadata)
            : base(metadata)
        {
            Value = value;
        }
    }

    public sealed class Err : Result<TValue, TError>
    {
        public new TError Error { get; }

        internal Err(TError error, IReadOnlyDictionary<string, object> metadata)
            : base(metadata)
        {
            Error = error;
        }
    }

    /// <summary> True, if the result is in the 'success' state. False otherwise </summary>
    public bool IsSuccess => this is Ok;

    /// <summary> True, if the result is in the 'error' state. False otherwise </summary>
    public bool IsError => this is Err;

    /// <summary> The value of the result </summary>
    /// <exception cref="InvalidOperationException"> Thrown if the result is in the 'error' state </exception>
    public TValue Value =>
        this is Ok ok ? ok.Value : throw new InvalidOperationException("Result is not in success state");

    /// <summary> The value of the result or the default if the result is in error state </summary>
    // ReSharper disable once ConvertToAutoPropertyWhenPossible
    public TValue? ValueOrDefault => this is Ok ok ? ok.Value : default;

    /// <summary> The error of the result </summary>
    /// <exception cref="InvalidOperationException"> Thrown if the result is in the 'success' state </exception>
    public TError Error =>
        this is Err err ? err.Error : throw new InvalidOperationException("Result is not in error state");

    /// <summary> The optional metadata </summary>
    public IReadOnlyDictionary<string, object> Metadata { get; }

    private Result(IReadOnlyDictionary<string, object> metadata)
    {
        Debug.Assert(this is Ok or Err, "Result has to extend either Ok or Err. No custom inheritance is allowed");
        Metadata = metadata;
    }

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
        return this switch
        {
            Ok ok => new Result<TNewValue, TError>.Ok(func(ok.Value), Metadata),
            Err err => new Result<TNewValue, TError>.Err(err.Error, Metadata),
            _ => throw new UnreachableException(),
        };
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
        return TryGetValue(out TValue? value) ? func(value) : defaultValue;
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
        return this switch
        {
            Ok ok => new Result<TValue, TNewError>.Ok(ok.Value, Metadata),
            Err err => new Result<TValue, TNewError>.Err(func(err.Error), Metadata),
            _ => throw new UnreachableException(),
        };
    }

    public bool TryGetValue([NotNullWhen(true)] out TValue? value)
    {
        if (this is Ok ok)
        {
            value = ok.Value!;
            return true;
        }
        value = default;
        return false;
    }

    public bool TryGetValue<TNewValue>(
        [NotNullWhen(true)] out TValue? value,
        [NotNullWhen(false)] out Result<TNewValue, TError>? failedResult
    )
    {
        if (this is Ok ok)
        {
            value = ok.Value!;
            failedResult = null;
            return true;
        }
        value = default;
        failedResult = new Result<TNewValue, TError>.Err(Error, Metadata);
        return false;
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

    public TValue Expect(string message)
    {
        if (this is not Ok ok)
            throw new InvalidOperationException(message);
        return ok.Value;
    }

    public TError ExpectError(string message)
    {
        if (this is not Err err)
            throw new InvalidOperationException(message);
        return err.Error;
    }

    public Result<TNewValue, TError> And<TNewValue>(Result<TNewValue, TError> result)
    {
        return this switch
        {
            Ok => result,
            Err err => new Result<TNewValue, TError>.Err(err.Error, Metadata),
            _ => throw new UnreachableException(),
        };
    }

    public Result<TNewValue, TError> And<TNewValue>(Func<Result<TNewValue, TError>> lazyResult)
    {
        return this switch
        {
            Ok => lazyResult(),
            Err err => new Result<TNewValue, TError>.Err(err.Error, Metadata),
            _ => throw new UnreachableException(),
        };
    }

    public Result<TValue, TError> Or(Result<TValue, TError> result)
    {
        return this switch
        {
            Ok => this,
            Err => result,
            _ => throw new UnreachableException(),
        };
    }

    public Result<TValue, TError> Or(Func<Result<TValue, TError>> lazyResult)
    {
        return this switch
        {
            Ok => this,
            Err => lazyResult(),
            _ => throw new UnreachableException(),
        };
    }

    public static implicit operator Result<TValue, TError>(TValue value) => Result.Ok<TValue, TError>(value);

    public static implicit operator Result<TValue, TError>(TError error) => Result.Error<TValue, TError>(error);

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
        return this switch
        {
            Ok ok => new Ok(ok.Value, newMetadata),
            Err err => new Err(err.Error, newMetadata),
            _ => throw new UnreachableException(),
        };
    }

    public Result<TValue, TError> WithMetadata(IDictionary<string, object> metadata)
    {
        var newMetadata = new ReadOnlyDictionary<string, object>(Metadata.Concat(metadata).ToDictionary());
        return this switch
        {
            Ok ok => new Ok(ok.Value, newMetadata),
            Err err => new Err(err.Error, newMetadata),
            _ => throw new UnreachableException(),
        };
    }
}

public static class Result
{
    public static Result<TValue, TError> Ok<TValue, TError>(TValue value, IDictionary<string, object>? metadata = null)
    {
        return new Result<TValue, TError>.Ok(
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
        return new Result<TValue, TError>.Err(
            error,
            metadata is null
                ? ReadOnlyDictionary<string, object>.Empty
                : new ReadOnlyDictionary<string, object>(metadata)
        );
    }

    public static Result<TValue, TError> Flatten<TValue, TError>(Result<Result<TValue, TError>, TError> result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return result switch
        {
            Result<Result<TValue, TError>, TError>.Ok ok => ok.Value,
            Result<Result<TValue, TError>, TError>.Err err => new Result<TValue, TError>.Err(err.Error, err.Metadata),
            _ => throw new UnreachableException(),
        };
    }

    public delegate bool TryParseFunc<in TIn, TOut>(TIn input, out TOut output);

    public static Result<TOut, StandardError> From<TIn, TOut>(TIn input, TryParseFunc<TIn, TOut> tryParse)
    {
        try
        {
            if (tryParse(input, out TOut output))
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

using System.Collections.ObjectModel;

namespace Darp.Results;

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
        return result.TryGetValue(out Result<TValue, TError>? value, out Result<TValue, TError>.Err? err) ? value : err;
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

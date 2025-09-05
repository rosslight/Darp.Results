using System.Collections.ObjectModel;

namespace Darp.Results;

/// <summary> A static class containing methods for creating and manipulating results. </summary>
public static class Result
{
    /// <summary> Creates a new result with the given value and metadata. </summary>
    /// <param name="value"> The value of the result. </param>
    /// <param name="metadata"> The metadata of the result. </param>
    /// <typeparam name="TValue"> The type of the value. </typeparam>
    /// <typeparam name="TError"> The type of the error. </typeparam>
    /// <returns> The result with the given value and metadata. </returns>
    public static Result<TValue, TError> Ok<TValue, TError>(TValue value, IDictionary<string, object>? metadata = null)
    {
        return new Result<TValue, TError>.Ok(
            value,
            metadata is null
                ? ReadOnlyDictionary<string, object>.Empty
                : new ReadOnlyDictionary<string, object>(metadata)
        );
    }

    /// <summary> Creates a new result with the given error and metadata. </summary>
    /// <param name="error"> The error of the result. </param>
    /// <param name="metadata"> The metadata of the result. </param>
    /// <typeparam name="TValue"> The type of the value. </typeparam>
    /// <typeparam name="TError"> The type of the error. </typeparam>
    /// <returns> The result with the given error and metadata. </returns>
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

    /// <summary> Flattens a nested result. </summary>
    /// <param name="result"> The nested result. </param>
    /// <typeparam name="TValue"> The type of the value. </typeparam>
    /// <typeparam name="TError"> The type of the error. </typeparam>
    /// <returns> The flattened result. </returns>
    public static Result<TValue, TError> Flatten<TValue, TError>(Result<Result<TValue, TError>, TError> result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return result.TryGetValue(out Result<TValue, TError>? value, out Result<TValue, TError>.Err? err) ? value : err;
    }

    /// <summary> Tries to parse the input using the given function. </summary>
    /// <param name="input"> The input to parse. </param>
    /// <param name="tryParse"> The function to parse the input. </param>
    /// <typeparam name="TIn"> The type of the input. </typeparam>
    /// <typeparam name="TOut"> The type of the output. </typeparam>
    /// <returns> The parsed result. </returns>
    public delegate bool TryParseFunc<in TIn, TOut>(TIn input, out TOut output);

    /// <summary> Tries to parse the input using the given function. </summary>
    /// <param name="input"> The input to parse. </param>
    /// <param name="tryParse"> The function to parse the input. </param>
    /// <typeparam name="TIn"> The type of the input. </typeparam>
    /// <typeparam name="TOut"> The type of the output. </typeparam>
    /// <returns> The parsed result. </returns>
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

    /// <summary> Tries to execute the given function. </summary>
    /// <param name="func"> The function to execute. </param>
    /// <typeparam name="TValue"> The type of the value. </typeparam>
    /// <returns> The result of the function. </returns>
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

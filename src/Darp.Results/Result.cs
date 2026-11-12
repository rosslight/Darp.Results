namespace Darp.Results;

/// <summary> A static class containing methods for creating and manipulating results. </summary>
public static class Result
{
    /// <summary> Creates the success case. It converts into any result with the same value type. </summary>
    /// <param name="value"> The value. </param>
    /// <typeparam name="TValue"> The type of the value. </typeparam>
    /// <returns> The success case. </returns>
    public static Ok<TValue> Ok<TValue>(TValue value) => new(value);

    /// <summary> Creates the failure case. It converts into any result with the same error type. </summary>
    /// <param name="error"> The error. </param>
    /// <typeparam name="TError"> The type of the error. </typeparam>
    /// <returns> The failure case. </returns>
    public static Err<TError> Err<TError>(TError error) => new(error);

    /// <summary> Flattens a nested result. </summary>
    /// <param name="result"> The nested result. </param>
    /// <typeparam name="TValue"> The type of the value. </typeparam>
    /// <typeparam name="TError"> The type of the error. </typeparam>
    /// <returns> The flattened result. </returns>
    public static Result<TValue, TError> Flatten<TValue, TError>(Result<Result<TValue, TError>, TError> result) =>
        result.TryGetValue(out Result<TValue, TError> value, out Err<TError> err) ? value : err;

    /// <summary> Tries to parse the input using the given function. </summary>
    /// <param name="input"> The input to parse. </param>
    /// <param name="output"> The output of the parse operation. </param>
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
        ArgumentNullException.ThrowIfNull(tryParse);
        try
        {
            if (tryParse(input, out TOut output))
                return Ok(output);
            return Err(StandardError.TryPatternFailed);
        }
        catch (Exception)
        {
            return Err(StandardError.ExceptionOccured);
        }
    }

    /// <summary> Tries to execute the given function. </summary>
    /// <param name="func"> The function to execute. </param>
    /// <typeparam name="TValue"> The type of the value. </typeparam>
    /// <returns> The result of the function. </returns>
    public static Result<TValue, Exception> Try<TValue>(Func<TValue> func)
    {
        ArgumentNullException.ThrowIfNull(func);
        try
        {
            return Ok(func());
        }
        catch (Exception e)
        {
            return Err(e);
        }
    }
}

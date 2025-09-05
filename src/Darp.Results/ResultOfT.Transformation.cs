namespace Darp.Results;

partial class Result<TValue, TError>
{
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
        return TryGetValue(out TValue? value, out Result<TNewValue, TError>.Err? err)
            ? new Result<TNewValue, TError>.Ok(func(value), Metadata)
            : err;
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
        return TryGetError(out TError? error, out Result<TValue, TNewError>.Ok? ok)
            ? new Result<TValue, TNewError>.Err(func(error), Metadata)
            : ok;
    }

    public Result<TNewValue, TError> And<TNewValue>(Result<TNewValue, TError> result)
    {
        return TryGetValue(out _, out Result<TNewValue, TError>.Err? error) ? result : error;
    }

    public Result<TNewValue, TError> And<TNewValue>(Func<TValue, Result<TNewValue, TError>> resultProvider)
    {
        ArgumentNullException.ThrowIfNull(resultProvider);
        return TryGetValue(out TValue? value, out Result<TNewValue, TError>.Err? error) ? resultProvider(value) : error;
    }

    public Result<TValue, TNewError> Or<TNewError>(Result<TValue, TNewError> result)
    {
        return TryGetError(out TError? _, out Result<TValue, TNewError>.Ok? ok) ? result : ok;
    }

    public Result<TValue, TNewError> Or<TNewError>(Func<TError, Result<TValue, TNewError>> resultProvider)
    {
        ArgumentNullException.ThrowIfNull(resultProvider);
        return TryGetError(out TError? error, out Result<TValue, TNewError>.Ok? ok) ? resultProvider(error) : ok;
    }
}

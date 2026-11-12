namespace Darp.Results;

partial struct Result<TValue, TError>
{
    /// <summary>
    /// Maps the result value to the <typeparamref name="TNewValue"/> by applying a function to a contained Ok value, leaving an Error value untouched.
    /// </summary>
    /// <param name="func"> The function to transform the value </param>
    /// <typeparam name="TNewValue"> The type of the new value </typeparam>
    /// <returns> The result with a new value or the existing error. Metadata is kept. </returns>
    /// <seealso href="https://doc.rust-lang.org/std/result/enum.html#method.map"/>
    public Result<TNewValue, TError> Map<TNewValue>(Func<TValue, TNewValue> func)
    {
        ArgumentNullException.ThrowIfNull(func);
        return TryGetValue(out TValue? value, out Err<TError> err)
            ? new Result<TNewValue, TError>(func(value), default!, _metadata, ResultState.Ok)
            : new Result<TNewValue, TError>(default!, err.Error, _metadata, ResultState.Err);
    }

    /// <summary>
    /// Maps the result error to the <typeparamref name="TNewError"/> by applying a function to a contained error, leaving a value untouched.
    /// </summary>
    /// <param name="func"> The function to transform the error </param>
    /// <typeparam name="TNewError"> The type of the error </typeparam>
    /// <returns> The result with a new error or the existing value. Metadata is kept. </returns>
    public Result<TValue, TNewError> MapError<TNewError>(Func<TError, TNewError> func)
    {
        ArgumentNullException.ThrowIfNull(func);
        return TryGetError(out TError? error, out Ok<TValue> ok)
            ? new Result<TValue, TNewError>(default!, func(error), _metadata, ResultState.Err)
            : new Result<TValue, TNewError>(ok.Value, default!, _metadata, ResultState.Ok);
    }

    /// <summary>
    /// Combines two results by returning the result of the first one if it is an error or the second one if it is a success.
    /// </summary>
    /// <param name="result"> The result to combine with </param>
    /// <typeparam name="TNewValue"> The type of the new value </typeparam>
    /// <returns> The result with a new value or the existing error </returns>
    public Result<TNewValue, TError> And<TNewValue>(Result<TNewValue, TError> result) =>
        TryGetValue(out _, out Err<TError> error) ? result : ErrAs<TNewValue>(error);

    /// <summary>
    /// Combines two results by returning the result of the first one if it is an error or the second one if it is a success.
    /// </summary>
    /// <param name="resultProvider"> The function to provide the result to combine with </param>
    /// <typeparam name="TNewValue"> The type of the new value </typeparam>
    /// <returns> The result with a new value or the existing error </returns>
    public Result<TNewValue, TError> And<TNewValue>(Func<TValue, Result<TNewValue, TError>> resultProvider)
    {
        ArgumentNullException.ThrowIfNull(resultProvider);
        return TryGetValue(out TValue? value, out Err<TError> error) ? resultProvider(value) : ErrAs<TNewValue>(error);
    }

    /// <summary>
    /// Combines two results by returning the result of the first one if it is a success or the second one if it is an error.
    /// </summary>
    /// <param name="result"> The result to combine with </param>
    /// <typeparam name="TNewError"> The type of the new error </typeparam>
    /// <returns> The result with a new error or the existing value </returns>
    public Result<TValue, TNewError> Or<TNewError>(Result<TValue, TNewError> result) =>
        TryGetError(out _, out Ok<TValue> ok) ? result : OkAs<TNewError>(ok);

    /// <summary>
    /// Combines two results by returning the result of the first one if it is a success or the second one if it is an error.
    /// </summary>
    /// <param name="resultProvider"> The function to provide the result to combine with </param>
    /// <typeparam name="TNewError"> The type of the new error </typeparam>
    /// <returns> The result with a new error or the existing value </returns>
    public Result<TValue, TNewError> Or<TNewError>(Func<TError, Result<TValue, TNewError>> resultProvider)
    {
        ArgumentNullException.ThrowIfNull(resultProvider);
        return TryGetError(out TError? error, out Ok<TValue> ok) ? resultProvider(error) : OkAs<TNewError>(ok);
    }

    // Keep metadata when a case moves into a result of another type.
    private Result<TNewValue, TError> ErrAs<TNewValue>(Err<TError> err) =>
        new(default!, err.Error, _metadata, ResultState.Err);

    private Result<TValue, TNewError> OkAs<TNewError>(Ok<TValue> ok) =>
        new(ok.Value, default!, _metadata, ResultState.Ok);
}

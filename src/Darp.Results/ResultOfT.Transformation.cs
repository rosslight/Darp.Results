using System.Diagnostics;

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
        return this switch
        {
            Ok ok => new Result<TNewValue, TError>.Ok(func(ok.Value), Metadata),
            Err err => new Result<TNewValue, TError>.Err(err.Error, Metadata),
            _ => throw new UnreachableException(InvalidCaseType),
        };
    }

    /// <summary>
    /// Returns the provided <paramref name="defaultValue"/> (if Error), or applies the <paramref name="func"/> to the contained value (if Ok).
    /// </summary>
    /// <param name="func"> The function to transform the value </param>
    /// <param name="defaultValue"> The default value if the result has an error </param>
    /// <typeparam name="TNewValue"> The type of the new value </typeparam>
    /// <returns> The mapped value or default </returns>
    /// <seealso href="https://doc.rust-lang.org/std/result/enum.Result.html#method.map_or"/>
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
            _ => throw new UnreachableException(InvalidCaseType),
        };
    }

    public Result<TNewValue, TError> And<TNewValue>(Result<TNewValue, TError> result)
    {
        return TryGetValue(out _, out Result<TNewValue, TError>.Err? error) ? result : error;
    }

    public Result<TNewValue, TError> And<TNewValue>(Func<Result<TNewValue, TError>> lazyResult)
    {
        return TryGetValue(out _, out Result<TNewValue, TError>.Err? error) ? lazyResult() : error;
    }

    public Result<TValue, TError> Or(Result<TValue, TError> result)
    {
        return IsOk ? this : result;
    }

    public Result<TValue, TError> Or(Func<Result<TValue, TError>> lazyResult)
    {
        return IsOk ? this : lazyResult();
    }
}

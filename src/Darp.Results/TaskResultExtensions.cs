using System.Diagnostics.Contracts;

namespace Darp.Results;

/// <summary>Task-based helpers to chain <see cref="Result{TValue,TError}"/> without manual awaits.</summary>
public static class TaskResultExtensions
{
    /// <summary>Maps the Ok value after awaiting the task.</summary>
    /// <param name="task">The task producing a result.</param>
    /// <param name="func">Value transform invoked only when the awaited result is Ok.</param>
    /// <typeparam name="TValue">Original value type.</typeparam>
    /// <typeparam name="TError">Error type.</typeparam>
    /// <typeparam name="TNewValue">Mapped value type.</typeparam>
    /// <returns>A task producing the mapped result.</returns>
    [Pure]
    public static async Task<Result<TNewValue, TError>> Map<TValue, TError, TNewValue>(
        this Task<Result<TValue, TError>> task,
        Func<TValue, TNewValue> func
    )
    {
        ArgumentNullException.ThrowIfNull(task);
        Result<TValue, TError> result = await task.ConfigureAwait(false);
        return result.Map(func);
    }

    /// <summary>Maps the Err value after awaiting the task.</summary>
    /// <param name="task">The task producing a result.</param>
    /// <param name="func">Error transform invoked only when the awaited result is Err.</param>
    /// <typeparam name="TValue">Original value type.</typeparam>
    /// <typeparam name="TError">Original error type.</typeparam>
    /// <typeparam name="TNewError">Mapped error type.</typeparam>
    /// <returns>A task producing the mapped result.</returns>
    [Pure]
    public static async Task<Result<TValue, TNewError>> MapError<TValue, TError, TNewError>(
        this Task<Result<TValue, TError>> task,
        Func<TError, TNewError> func
    )
    {
        ArgumentNullException.ThrowIfNull(task);
        Result<TValue, TError> result = await task.ConfigureAwait(false);
        return result.MapError(func);
    }

    /// <summary>Combines with another result if the awaited task is Ok.</summary>
    /// <param name="task">The task producing a result.</param>
    /// <param name="result">The result to return when the awaited result is Ok.</param>
    /// <typeparam name="TValue">Original value type.</typeparam>
    /// <typeparam name="TError">Error type.</typeparam>
    /// <typeparam name="TNewValue">Value type of the next result.</typeparam>
    /// <returns>The second result if Ok, otherwise the first error.</returns>
    [Pure]
    public static async Task<Result<TNewValue, TError>> And<TValue, TError, TNewValue>(
        this Task<Result<TValue, TError>> task,
        Result<TNewValue, TError> result
    )
    {
        ArgumentNullException.ThrowIfNull(task);
        Result<TValue, TError> awaited = await task.ConfigureAwait(false);
        return awaited.And(result);
    }

    /// <summary>Combines with a result factory if the awaited task is Ok.</summary>
    /// <param name="task">The task producing a result.</param>
    /// <param name="resultProvider">Factory invoked with the Ok value.</param>
    /// <typeparam name="TValue">Original value type.</typeparam>
    /// <typeparam name="TError">Error type.</typeparam>
    /// <typeparam name="TNewValue">Value type of the produced result.</typeparam>
    /// <returns>The produced result if Ok, otherwise the first error.</returns>
    [Pure]
    public static Task<Result<TNewValue, TError>> And<TValue, TError, TNewValue>(
        this Task<Result<TValue, TError>> task,
        Func<TValue, TNewValue> resultProvider
    )
    {
        return task.And<TValue, TError, TNewValue>(value => new Result.Ok<TNewValue, TError>(resultProvider(value)));
    }

    /// <summary>Combines with a result factory if the awaited task is Ok.</summary>
    /// <param name="task">The task producing a result.</param>
    /// <param name="resultProvider">Factory invoked with the Ok value.</param>
    /// <typeparam name="TValue">Original value type.</typeparam>
    /// <typeparam name="TError">Error type.</typeparam>
    /// <typeparam name="TNewValue">Value type of the produced result.</typeparam>
    /// <returns>The produced result if Ok, otherwise the first error.</returns>
    [Pure]
    public static async Task<Result<TNewValue, TError>> And<TValue, TError, TNewValue>(
        this Task<Result<TValue, TError>> task,
        Func<TValue, Result<TNewValue, TError>> resultProvider
    )
    {
        ArgumentNullException.ThrowIfNull(task);
        Result<TValue, TError> result = await task.ConfigureAwait(false);
        return result.And(resultProvider);
    }

    /// <summary>Returns the awaited result or a replacement error result if it was Err.</summary>
    /// <param name="task">The task producing a result.</param>
    /// <param name="result">Result returned when the awaited result is Err.</param>
    /// <typeparam name="TValue">Value type.</typeparam>
    /// <typeparam name="TError">Original error type.</typeparam>
    /// <typeparam name="TNewError">Fallback error type.</typeparam>
    /// <returns>The awaited result if Ok, otherwise the fallback.</returns>
    [Pure]
    public static async Task<Result<TValue, TNewError>> Or<TValue, TError, TNewError>(
        this Task<Result<TValue, TError>> task,
        Result<TValue, TNewError> result
    )
    {
        ArgumentNullException.ThrowIfNull(task);
        Result<TValue, TError> awaited = await task.ConfigureAwait(false);
        return awaited.Or(result);
    }

    /// <summary>Returns the awaited result or a replacement error result produced by the factory.</summary>
    /// <param name="task">The task producing a result.</param>
    /// <param name="resultProvider">Factory invoked with the Err value.</param>
    /// <typeparam name="TValue">Value type.</typeparam>
    /// <typeparam name="TError">Original error type.</typeparam>
    /// <typeparam name="TNewError">Fallback error type.</typeparam>
    /// <returns>The awaited result if Ok, otherwise the produced fallback.</returns>
    [Pure]
    public static Task<Result<TValue, TNewError>> Or<TValue, TError, TNewError>(
        this Task<Result<TValue, TError>> task,
        Func<TError, TNewError> resultProvider
    )
    {
        return task.Or<TValue, TError, TNewError>(error => new Result.Err<TValue, TNewError>(resultProvider(error)));
    }

    /// <summary>Returns the awaited result or a replacement error result produced by the factory.</summary>
    /// <param name="task">The task producing a result.</param>
    /// <param name="resultProvider">Factory invoked with the Err value.</param>
    /// <typeparam name="TValue">Value type.</typeparam>
    /// <typeparam name="TError">Original error type.</typeparam>
    /// <typeparam name="TNewError">Fallback error type.</typeparam>
    /// <returns>The awaited result if Ok, otherwise the produced fallback.</returns>
    [Pure]
    public static async Task<Result<TValue, TNewError>> Or<TValue, TError, TNewError>(
        this Task<Result<TValue, TError>> task,
        Func<TError, Result<TValue, TNewError>> resultProvider
    )
    {
        ArgumentNullException.ThrowIfNull(task);
        Result<TValue, TError> result = await task.ConfigureAwait(false);
        return result.Or(resultProvider);
    }

    /// <summary>Unwraps the Ok value or throws if Err after awaiting the task.</summary>
    /// <param name="task">The task producing a result.</param>
    /// <typeparam name="TValue">Value type.</typeparam>
    /// <typeparam name="TError">Error type.</typeparam>
    /// <returns>The underlying Ok value.</returns>
    [Pure]
    public static async Task<TValue> Unwrap<TValue, TError>(this Task<Result<TValue, TError>> task)
    {
        ArgumentNullException.ThrowIfNull(task);
        Result<TValue, TError> result = await task.ConfigureAwait(false);
        return result.Unwrap();
    }

    /// <summary>Unwraps the Ok value or returns the provided default if Err after awaiting the task.</summary>
    /// <param name="task">The task producing a result.</param>
    /// <param name="defaultValue">Value to return when Err.</param>
    /// <typeparam name="TValue">Value type.</typeparam>
    /// <typeparam name="TError">Error type.</typeparam>
    /// <returns>The Ok value or the provided default.</returns>
    [Pure]
    public static async Task<TValue> Unwrap<TValue, TError>(this Task<Result<TValue, TError>> task, TValue defaultValue)
    {
        ArgumentNullException.ThrowIfNull(task);
        Result<TValue, TError> result = await task.ConfigureAwait(false);
        return result.Unwrap(defaultValue);
    }

    /// <summary>Unwraps the Ok value or returns the provider's value if Err after awaiting the task.</summary>
    /// <param name="task">The task producing a result.</param>
    /// <param name="valueProvider">Provider invoked with the Err value.</param>
    /// <typeparam name="TValue">Value type.</typeparam>
    /// <typeparam name="TError">Error type.</typeparam>
    /// <returns>The Ok value or provider result.</returns>
    [Pure]
    public static async Task<TValue> Unwrap<TValue, TError>(
        this Task<Result<TValue, TError>> task,
        Func<TError, TValue> valueProvider
    )
    {
        ArgumentNullException.ThrowIfNull(task);
        Result<TValue, TError> result = await task.ConfigureAwait(false);
        return result.Unwrap(valueProvider);
    }

    /// <summary>Unwraps the Ok value or default(TValue) if Err after awaiting the task.</summary>
    /// <param name="task">The task producing a result.</param>
    /// <typeparam name="TValue">Value type.</typeparam>
    /// <typeparam name="TError">Error type.</typeparam>
    /// <returns>The Ok value or default.</returns>
    [Pure]
    public static async Task<TValue?> UnwrapOrDefault<TValue, TError>(this Task<Result<TValue, TError>> task)
    {
        ArgumentNullException.ThrowIfNull(task);
        Result<TValue, TError> result = await task.ConfigureAwait(false);
        return result.UnwrapOrDefault();
    }

    /// <summary>Unwraps the Err value or throws if Ok after awaiting the task.</summary>
    /// <param name="task">The task producing a result.</param>
    /// <typeparam name="TValue">Value type.</typeparam>
    /// <typeparam name="TError">Error type.</typeparam>
    /// <returns>The underlying Err value.</returns>
    [Pure]
    public static async Task<TError> UnwrapError<TValue, TError>(this Task<Result<TValue, TError>> task)
    {
        ArgumentNullException.ThrowIfNull(task);
        Result<TValue, TError> result = await task.ConfigureAwait(false);
        return result.UnwrapError();
    }

    /// <summary>Unwraps the Ok value with a custom message or throws if Err after awaiting the task.</summary>
    /// <param name="task">The task producing a result.</param>
    /// <param name="message">The message used when throwing on Err.</param>
    /// <typeparam name="TValue">Value type.</typeparam>
    /// <typeparam name="TError">Error type.</typeparam>
    /// <returns>The underlying Ok value.</returns>
    [Pure]
    public static async Task<TValue> Expect<TValue, TError>(this Task<Result<TValue, TError>> task, string message)
    {
        ArgumentNullException.ThrowIfNull(task);
        Result<TValue, TError> result = await task.ConfigureAwait(false);
        return result.Expect(message);
    }

    /// <summary>Unwraps the Err value with a custom message or throws if Ok after awaiting the task.</summary>
    /// <param name="task">The task producing a result.</param>
    /// <param name="message">The message used when throwing on Ok.</param>
    /// <typeparam name="TValue">Value type.</typeparam>
    /// <typeparam name="TError">Error type.</typeparam>
    /// <returns>The underlying Err value.</returns>
    [Pure]
    public static async Task<TError> ExpectError<TValue, TError>(this Task<Result<TValue, TError>> task, string message)
    {
        ArgumentNullException.ThrowIfNull(task);
        Result<TValue, TError> result = await task.ConfigureAwait(false);
        return result.ExpectError(message);
    }
}

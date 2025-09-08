using System.Diagnostics.Contracts;
using static Darp.Results.Result;

namespace Darp.Results;

/// <summary> A result which might be in the Success or Error state. Option to attach Metadata as well </summary>
/// <typeparam name="TValue"> The type of the value. </typeparam>
/// <typeparam name="TError"> The type of the error. </typeparam>
public abstract partial class Result<TValue, TError> : IEquatable<Result<TValue, TError>>
{
    private const string InvalidCaseType =
        $"Result should either be {nameof(Ok<TValue, TError>)} or {nameof(Err<TValue, TError>)}";

    /// <summary> The optional metadata </summary>
    public IReadOnlyDictionary<string, object> Metadata { get; }

    /// <summary> Initializes a new instance of the <see cref="Result{TValue, TError}"/> class. </summary>
    /// <param name="metadata"> The metadata of the result. </param>
    internal Result(IReadOnlyDictionary<string, object> metadata)
    {
        if (this is not (Ok<TValue, TError> or Err<TValue, TError>))
        {
            throw new InvalidOperationException(
                $"Result has to extend either {nameof(Ok<TValue, TError>)} or {nameof(Err<TValue, TError>)}. No custom inheritance is allowed"
            );
        }
        Metadata = metadata;
    }

    /// <summary> Indicates whether the result is in the <see cref="Darp.Results.Result.Ok{TValue,TError}"/> state. </summary>
    public bool IsOk => this is Ok<TValue, TError>;

    /// <summary> Indicates whether the result is in the <see cref="Result.Err{TValue,TError}"/> state. </summary>
    public bool IsErr => this is Err<TValue, TError>;

    /// <summary> Implicitly converts a value to a <see cref="Result{TValue, TError}"/> in the <see cref="Darp.Results.Result.Ok{TValue,TError}"/> state. </summary>
    /// <param name="value"> The value to convert. </param>
    /// <returns> The result in the <see cref="Darp.Results.Result.Ok{TValue,TError}"/> state. </returns>
#pragma warning disable CA2225 // Alternative provided as public constructors
    public static implicit operator Result<TValue, TError>(TValue value) => new Ok<TValue, TError>(value);
#pragma warning restore CA2225

    /// <summary> Implicitly converts an error to a <see cref="Result{TValue, TError}"/> in the <see cref="Result.Err{TValue,TError}"/> state. </summary>
    /// <param name="error"> The error to convert. </param>
    /// <returns> The result in the <see cref="Result.Err{TValue,TError}"/> state. </returns>
#pragma warning disable CA2225 // Alternative provided as public constructors
    public static implicit operator Result<TValue, TError>(TError error) => new Err<TValue, TError>(error);
#pragma warning restore CA2225

    /// <summary>
    /// Enumerates the values of the result.
    /// Returns the value if in <see cref="Darp.Results.Result.Ok{TValue,TError}"/> state or an empty enumerator if in <see cref="Result.Err{TValue,TError}"/> state.
    /// </summary>
    /// <returns> The enumerator of the result. </returns>
    /// <seealso href="https://doc.rust-lang.org/std/result/enum.Result.html#method.iter"/>
    [Pure]
    public IEnumerator<TValue> GetEnumerator() => AsEnumerable().GetEnumerator();

    /// <summary>
    /// Enumerates the values of the result.
    /// Returns the value if in <see cref="Darp.Results.Result.Ok{TValue,TError}"/> state or an empty enumerable if in <see cref="Result.Err{TValue,TError}"/> state.
    /// </summary>
    /// <returns> The enumerable of the result. </returns>
    /// <seealso href="https://doc.rust-lang.org/std/result/enum.Result.html#method.iter"/>
    [Pure]
    public IEnumerable<TValue> AsEnumerable()
    {
        if (this is not Ok<TValue, TError> ok)
            yield break;
        yield return ok.Value;
    }

    /// <summary> Indicates whether the current result is equal to another result of the same type. </summary>
    /// <param name="other"> The other result to compare to. </param>
    /// <returns> True if the results are equal, false otherwise. </returns>
    public abstract bool Equals(Result<TValue, TError>? other);

    /// <inheritdoc />
    public abstract override int GetHashCode();

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is Result<TValue, TError> result && Equals(result);
    }
}

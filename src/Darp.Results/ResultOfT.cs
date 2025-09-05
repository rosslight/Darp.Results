using System.Diagnostics;
using System.Diagnostics.Contracts;

namespace Darp.Results;

/// <summary> A result which might be in the Success or Error state. Option to attach Metadata as well </summary>
/// <typeparam name="TValue"> The type of the value. </typeparam>
/// <typeparam name="TError"> The type of the error. </typeparam>
public abstract partial class Result<TValue, TError> : IEquatable<Result<TValue, TError>>
{
    internal const string InvalidCaseType = $"Result should either be {nameof(Ok)} or {nameof(Err)}";

    /// <summary> The optional metadata </summary>
    public IReadOnlyDictionary<string, object> Metadata { get; }

    /// <summary> Initializes a new instance of the <see cref="Result{TValue, TError}"/> class. </summary>
    /// <param name="metadata"> The metadata of the result. </param>
    private Result(IReadOnlyDictionary<string, object> metadata)
    {
        if (this is not (Ok or Err))
        {
            throw new InvalidOperationException(
                $"Result has to extend either {nameof(Ok)} or {nameof(Err)}. No custom inheritance is allowed"
            );
        }
        Metadata = metadata;
    }

    public bool IsOk => this is Ok;
    public bool IsErr => this is Err;

    public static implicit operator Result<TValue, TError>(TValue value) => Result.Ok<TValue, TError>(value);

    public static implicit operator Result<TValue, TError>(TError error) => Result.Error<TValue, TError>(error);

    /// <seealso href="https://doc.rust-lang.org/std/result/enum.Result.html#method.iter"/>
    [Pure]
    public IEnumerator<TValue> GetEnumerator() => AsEnumerable().GetEnumerator();

    /// <seealso href="https://doc.rust-lang.org/std/result/enum.Result.html#method.iter"/>
    [Pure]
    public IEnumerable<TValue> AsEnumerable()
    {
        if (this is not Ok ok)
            yield break;
        yield return ok.Value;
    }

    /// <summary>
    /// Indicates whether the current result is equal to another result of the same type.
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public abstract bool Equals(Result<TValue, TError>? other);

    /// <inheritdoc />
    public abstract override int GetHashCode();

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is Result<TValue, TError> result && Equals(result);
    }

    /// <summary> Represents a successful result. </summary>
    [DebuggerDisplay("{\"Ok: \" + Value,nq}")]
    public sealed class Ok : Result<TValue, TError>
    {
        /// <summary> The value of the result. </summary>
        public TValue Value { get; }

        /// <summary> Initializes a new instance of the <see cref="Result{TValue,TError}.Ok"/> class. </summary>
        /// <param name="value"> The value of the result. </param>
        /// <param name="metadata"> The metadata of the result. </param>
        internal Ok(TValue value, IReadOnlyDictionary<string, object> metadata)
            : base(metadata)
        {
            Value = value;
        }

        /// <summary> Converts the result to a new error type. </summary>
        /// <typeparam name="TNewError"> The type of the new error. </typeparam>
        /// <returns> The result with a new error or the existing value. </returns>
        public Result<TValue, TNewError>.Ok As<TNewError>()
        {
            // Do not allocate a new ok if the TNewError == TError
            if (this is Result<TValue, TNewError>.Ok ok)
                return ok;
            return new Result<TValue, TNewError>.Ok(Value, Metadata);
        }

        /// <inheritdoc />
        public override bool Equals(Result<TValue, TError>? other)
        {
            return other is Ok ok && EqualityComparer<TValue>.Default.Equals(Value, ok.Value);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return Value is null ? 0 : EqualityComparer<TValue>.Default.GetHashCode(Value);
        }
    }

    /// <summary> Represents a failed result. </summary>
    [DebuggerDisplay("{\"Err: \" + Error,nq}")]
    public sealed class Err : Result<TValue, TError>
    {
        /// <summary> The error of the result. </summary>
        public TError Error { get; }

        /// <summary> Initializes a new instance of the <see cref="Result{TValue,TError}.Err"/> class. </summary>
        /// <param name="error"> The error of the result. </param>
        /// <param name="metadata"> The metadata of the result. </param>
        internal Err(TError error, IReadOnlyDictionary<string, object> metadata)
            : base(metadata)
        {
            Error = error;
        }

        /// <summary> Converts the result to a new value type. </summary>
        /// <typeparam name="TNewValue"> The type of the new value. </typeparam>
        /// <returns> The result with a new value or the existing error. </returns>
        public Result<TNewValue, TError>.Err As<TNewValue>()
        {
            // Do not allocate a new error if the TNewValue == TValue
            if (this is Result<TNewValue, TError>.Err err)
                return err;
            return new Result<TNewValue, TError>.Err(Error, Metadata);
        }

        /// <inheritdoc />
        public override bool Equals(Result<TValue, TError>? other)
        {
            return other is Err err && EqualityComparer<TError>.Default.Equals(Error, err.Error);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return Error is null ? 0 : EqualityComparer<TError>.Default.GetHashCode(Error);
        }
    }
}

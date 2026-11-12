using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;

namespace Darp.Results;

/// <summary> A result which is either <see cref="Ok{TValue}"/> or <see cref="Err{TError}"/>, with optional metadata. </summary>
/// <typeparam name="TValue"> The type of the value. </typeparam>
/// <typeparam name="TError"> The type of the error. </typeparam>
/// <remarks>
/// <para> This is a C# 15 union: switch expressions over <see cref="Ok{TValue}"/> and <see cref="Err{TError}"/> are exhaustive. </para>
/// <para> <c>default(Result)</c> is neither Ok nor Err. Switching over it throws, like switching over <see langword="null"/>. </para>
/// </remarks>
[Union]
[DebuggerDisplay("{ToString(),nq}")]
public readonly partial struct Result<TValue, TError>
    : Result<TValue, TError>.IUnionMembers,
        IUnion,
        IEquatable<Result<TValue, TError>>
{
    private readonly TValue _value;
    private readonly TError _error;
    private readonly IReadOnlyDictionary<string, object>? _metadata;
    private readonly ResultState _state;

    /// <summary> The members the compiler uses to treat <see cref="Result{TValue,TError}"/> as a union. </summary>
    /// <remarks> Keeping them off the struct keeps <see cref="Result{TValue, TError}.TryGetValue(out TValue)"/> unambiguous for <c>out var</c>. </remarks>
#pragma warning disable CA1000 // The union pattern requires static Create members
    public interface IUnionMembers
    {
        /// <summary> Creates a result in the <see cref="Ok{TValue}"/> state. </summary>
        /// <param name="value"> The success case. </param>
        /// <returns> The result. </returns>
        public static Result<TValue, TError> Create(Ok<TValue> value) =>
            new(value.Value, default!, value.MetadataOrNull, ResultState.Ok);

        /// <summary> Creates a result in the <see cref="Err{TError}"/> state. </summary>
        /// <param name="value"> The failure case. </param>
        /// <returns> The result. </returns>
        public static Result<TValue, TError> Create(Err<TError> value) =>
            new(default!, value.Error, value.MetadataOrNull, ResultState.Err);

        /// <summary> The boxed case, or <see langword="null"/> for <c>default(Result)</c>. </summary>
        object? Value { get; }

        /// <summary> Whether the result is Ok or Err. False only for <c>default(Result)</c>. </summary>
        bool HasValue { get; }

        /// <summary> Gets the success case without boxing. </summary>
        /// <param name="value"> The success case. </param>
        /// <returns> True if the result is Ok. </returns>
        bool TryGetValue(out Ok<TValue> value);

        /// <summary> Gets the failure case without boxing. </summary>
        /// <param name="value"> The failure case. </param>
        /// <returns> True if the result is Err. </returns>
        bool TryGetValue(out Err<TError> value);
    }
#pragma warning restore CA1000

    private Result(TValue value, TError error, IReadOnlyDictionary<string, object>? metadata, ResultState state)
    {
        _value = value;
        _error = error;
        _metadata = metadata;
        _state = state;
    }

    /// <summary> The metadata of the current case </summary>
    public IReadOnlyDictionary<string, object> Metadata => _metadata ?? ReadOnlyDictionary<string, object>.Empty;

    /// <summary> Indicates whether the result is in the <see cref="Ok{TValue}"/> state. </summary>
    public bool IsOk => _state is ResultState.Ok;

    /// <summary> Indicates whether the result is in the <see cref="Err{TError}"/> state. </summary>
    public bool IsErr => _state is ResultState.Err;

    object? IUnionMembers.Value => BoxedCase;
    object? IUnion.Value => BoxedCase;
    bool IUnionMembers.HasValue => _state is not ResultState.None;

    bool IUnionMembers.TryGetValue(out Ok<TValue> value)
    {
        value = OkCase;
        return _state is ResultState.Ok;
    }

    bool IUnionMembers.TryGetValue(out Err<TError> value)
    {
        value = ErrCase;
        return _state is ResultState.Err;
    }

    private object? BoxedCase =>
        _state switch
        {
            ResultState.Ok => OkCase,
            ResultState.Err => ErrCase,
            _ => null,
        };

    private Ok<TValue> OkCase => Ok<TValue>.Create(_value, _metadata);
    private Err<TError> ErrCase => Err<TError>.Create(_error, _metadata);

    /// <summary> Implicitly converts a value to a result in the <see cref="Ok{TValue}"/> state. </summary>
    /// <param name="value"> The value to convert. </param>
#pragma warning disable CA2225 // Alternative provided through Ok<TValue>
    public static implicit operator Result<TValue, TError>(TValue value) =>
        new(value, default!, null, ResultState.Ok);
#pragma warning restore CA2225

    /// <summary> Implicitly converts an error to a result in the <see cref="Err{TError}"/> state. </summary>
    /// <param name="error"> The error to convert. </param>
#pragma warning disable CA2225 // Alternative provided through Err<TError>
    public static implicit operator Result<TValue, TError>(TError error) =>
        new(default!, error, null, ResultState.Err);
#pragma warning restore CA2225

    /// <summary>
    /// Enumerates the values of the result.
    /// Returns the value if in <see cref="Ok{TValue}"/> state or an empty enumerator if in <see cref="Err{TError}"/> state.
    /// </summary>
    /// <returns> The enumerator of the result. </returns>
    /// <seealso href="https://doc.rust-lang.org/std/result/enum.Result.html#method.iter"/>
    [Pure]
    public IEnumerator<TValue> GetEnumerator() => AsEnumerable().GetEnumerator();

    /// <summary>
    /// Enumerates the values of the result.
    /// Returns the value if in <see cref="Ok{TValue}"/> state or an empty enumerable if in <see cref="Err{TError}"/> state.
    /// </summary>
    /// <returns> The enumerable of the result. </returns>
    /// <seealso href="https://doc.rust-lang.org/std/result/enum.Result.html#method.iter"/>
    [Pure]
    public IEnumerable<TValue> AsEnumerable() => IsOk ? [_value] : [];

    /// <summary> Compares case and payload. Metadata is ignored. </summary>
    /// <param name="other"> The other result to compare to. </param>
    /// <returns> True if the results are equal, false otherwise. </returns>
    public bool Equals(Result<TValue, TError> other) =>
        _state == other._state
        && _state switch
        {
            ResultState.Ok => EqualityComparer<TValue>.Default.Equals(_value, other._value),
            ResultState.Err => EqualityComparer<TError>.Default.Equals(_error, other._error),
            _ => true,
        };

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Result<TValue, TError> result && Equals(result);

    /// <inheritdoc />
    public override int GetHashCode() =>
        _state switch
        {
            ResultState.Ok => HashCode.Combine(_state, _value),
            ResultState.Err => HashCode.Combine(_state, _error),
            _ => 0,
        };

    /// <summary> Compares case and payload. Metadata is ignored. </summary>
    /// <param name="left"> The left result. </param>
    /// <param name="right"> The right result. </param>
    /// <returns> True if the results are equal. </returns>
    public static bool operator ==(Result<TValue, TError> left, Result<TValue, TError> right) => left.Equals(right);

    /// <summary> Compares case and payload. Metadata is ignored. </summary>
    /// <param name="left"> The left result. </param>
    /// <param name="right"> The right result. </param>
    /// <returns> True if the results differ. </returns>
    public static bool operator !=(Result<TValue, TError> left, Result<TValue, TError> right) => !left.Equals(right);

    /// <inheritdoc />
    public override string ToString() => BoxedCase?.ToString() ?? "<uninitialized>";

    private static InvalidOperationException Uninitialized() =>
        new("The result is uninitialized. Create it from an Ok or Err value.");
}

internal enum ResultState : byte
{
    None,
    Ok,
    Err,
}

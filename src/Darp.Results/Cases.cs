using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Darp.Results;

/// <summary> The success case of a <see cref="Result{TValue,TError}"/>, with optional metadata. </summary>
/// <typeparam name="TValue"> The type of the value. </typeparam>
/// <remarks>
/// Converts implicitly into every <see cref="Result{TValue,TError}"/> with a matching value type and keeps its metadata.
/// Equality compares the value only.
/// </remarks>
/// <seealso href="https://doc.rust-lang.org/std/result/enum.Result.html#variant.Ok"/>
[DebuggerDisplay("{ToString(),nq}")]
public readonly struct Ok<TValue> : IEquatable<Ok<TValue>>
{
    private readonly IReadOnlyDictionary<string, object>? _metadata;

    /// <summary> Initializes the success case. </summary>
    /// <param name="value"> The value of the result. </param>
    public Ok(TValue value) => Value = value;

    /// <summary> Initializes the success case with metadata. </summary>
    /// <param name="value"> The value of the result. </param>
    /// <param name="metadata"> The metadata of the result. It is copied. </param>
    public Ok(TValue value, IDictionary<string, object> metadata)
    {
        Value = value;
        _metadata = new ReadOnlyDictionary<string, object>(new Dictionary<string, object>(metadata));
    }

    private Ok(TValue value, IReadOnlyDictionary<string, object>? metadata)
    {
        Value = value;
        _metadata = metadata;
    }

    /// <summary> The value of the result. </summary>
    public TValue Value { get; }

    /// <summary> The optional metadata </summary>
    public IReadOnlyDictionary<string, object> Metadata => _metadata ?? ReadOnlyDictionary<string, object>.Empty;

    internal IReadOnlyDictionary<string, object>? MetadataOrNull => _metadata;

    // Does not copy: the metadata is already read-only.
    internal static Ok<TValue> Create(TValue value, IReadOnlyDictionary<string, object>? metadata) =>
        new(value, metadata);

    /// <summary> Deconstructs the case into its value </summary>
    /// <param name="value"> The value of the result </param>
    public void Deconstruct(out TValue value) => value = Value;

    /// <summary> Deconstructs the case into its value and metadata </summary>
    /// <param name="value"> The value of the result </param>
    /// <param name="metadata"> The metadata of the result </param>
    public void Deconstruct(out TValue value, out IReadOnlyDictionary<string, object> metadata)
    {
        value = Value;
        metadata = Metadata;
    }

    /// <summary> Compares the value. Metadata is ignored. </summary>
    /// <param name="other"> The other case. </param>
    /// <returns> True if the values are equal. </returns>
    public bool Equals(Ok<TValue> other) => EqualityComparer<TValue>.Default.Equals(Value, other.Value);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Ok<TValue> other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => Value is null ? 0 : EqualityComparer<TValue>.Default.GetHashCode(Value);

    /// <summary> Compares the value. Metadata is ignored. </summary>
    /// <param name="left"> The left case. </param>
    /// <param name="right"> The right case. </param>
    /// <returns> True if the values are equal. </returns>
    public static bool operator ==(Ok<TValue> left, Ok<TValue> right) => left.Equals(right);

    /// <summary> Compares the value. Metadata is ignored. </summary>
    /// <param name="left"> The left case. </param>
    /// <param name="right"> The right case. </param>
    /// <returns> True if the values differ. </returns>
    public static bool operator !=(Ok<TValue> left, Ok<TValue> right) => !left.Equals(right);

    /// <inheritdoc />
    public override string ToString() => $"Ok({Value})";
}

/// <summary> The failure case of a <see cref="Result{TValue,TError}"/>, with optional metadata. </summary>
/// <typeparam name="TError"> The type of the error. </typeparam>
/// <remarks>
/// Converts implicitly into every <see cref="Result{TValue,TError}"/> with a matching error type and keeps its metadata.
/// Equality compares the error only.
/// </remarks>
/// <seealso href="https://doc.rust-lang.org/std/result/enum.Result.html#variant.Err"/>
[DebuggerDisplay("{ToString(),nq}")]
public readonly struct Err<TError> : IEquatable<Err<TError>>
{
    private readonly IReadOnlyDictionary<string, object>? _metadata;

    /// <summary> Initializes the failure case. </summary>
    /// <param name="error"> The error of the result. </param>
    public Err(TError error) => Error = error;

    /// <summary> Initializes the failure case with metadata. </summary>
    /// <param name="error"> The error of the result. </param>
    /// <param name="metadata"> The metadata of the result. It is copied. </param>
    public Err(TError error, IDictionary<string, object> metadata)
    {
        Error = error;
        _metadata = new ReadOnlyDictionary<string, object>(new Dictionary<string, object>(metadata));
    }

    private Err(TError error, IReadOnlyDictionary<string, object>? metadata)
    {
        Error = error;
        _metadata = metadata;
    }

    /// <summary> The error of the result. </summary>
    public TError Error { get; }

    /// <summary> The optional metadata </summary>
    public IReadOnlyDictionary<string, object> Metadata => _metadata ?? ReadOnlyDictionary<string, object>.Empty;

    internal IReadOnlyDictionary<string, object>? MetadataOrNull => _metadata;

    // Does not copy: the metadata is already read-only.
    internal static Err<TError> Create(TError error, IReadOnlyDictionary<string, object>? metadata) =>
        new(error, metadata);

    /// <summary> Deconstructs the case into its error </summary>
    /// <param name="error"> The error of the result </param>
    public void Deconstruct(out TError error) => error = Error;

    /// <summary> Deconstructs the case into its error and metadata </summary>
    /// <param name="error"> The error of the result </param>
    /// <param name="metadata"> The metadata of the result </param>
    public void Deconstruct(out TError error, out IReadOnlyDictionary<string, object> metadata)
    {
        error = Error;
        metadata = Metadata;
    }

    /// <summary> Compares the error. Metadata is ignored. </summary>
    /// <param name="other"> The other case. </param>
    /// <returns> True if the errors are equal. </returns>
    public bool Equals(Err<TError> other) => EqualityComparer<TError>.Default.Equals(Error, other.Error);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Err<TError> other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => Error is null ? 0 : EqualityComparer<TError>.Default.GetHashCode(Error);

    /// <summary> Compares the error. Metadata is ignored. </summary>
    /// <param name="left"> The left case. </param>
    /// <param name="right"> The right case. </param>
    /// <returns> True if the errors are equal. </returns>
    public static bool operator ==(Err<TError> left, Err<TError> right) => left.Equals(right);

    /// <summary> Compares the error. Metadata is ignored. </summary>
    /// <param name="left"> The left case. </param>
    /// <param name="right"> The right case. </param>
    /// <returns> True if the errors differ. </returns>
    public static bool operator !=(Err<TError> left, Err<TError> right) => !left.Equals(right);

    /// <inheritdoc />
    public override string ToString() => $"Err({Error})";
}

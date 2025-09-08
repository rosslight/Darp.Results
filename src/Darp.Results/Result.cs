using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Darp.Results;

/// <summary> A static class containing methods for creating and manipulating results. </summary>
public static class Result
{
    /// <summary> Flattens a nested result. </summary>
    /// <param name="result"> The nested result. </param>
    /// <typeparam name="TValue"> The type of the value. </typeparam>
    /// <typeparam name="TError"> The type of the error. </typeparam>
    /// <returns> The flattened result. </returns>
    public static Result<TValue, TError> Flatten<TValue, TError>(Result<Result<TValue, TError>, TError> result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return result.TryGetValue(out Result<TValue, TError>? value, out Err<TValue, TError>? err) ? value : err;
    }

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
        ArgumentNullException.ThrowIfNull(func);
        try
        {
            return func();
        }
        catch (Exception e)
        {
            return e;
        }
    }

    /// <summary> Represents a successful result. </summary>
    /// <seealso href="https://doc.rust-lang.org/std/result/enum.Result.html#variant.Ok"/>
    [DebuggerDisplay("{\"Ok: \" + Value,nq}")]
    public sealed class Ok<TValue, TError> : Result<TValue, TError>
    {
        /// <summary> The value of the result. </summary>
        public TValue Value { get; }

        /// <summary> Initializes a new instance of the <see cref="Ok{TValue,TError}"/> class. </summary>
        /// <param name="value"> The value of the result. </param>
        public Ok(TValue value)
            : this(value, readOnlyMetadata: ReadOnlyDictionary<string, object>.Empty) { }

        /// <summary> Initializes a new instance of the <see cref="Ok{TValue,TError}"/> class. </summary>
        /// <param name="value"> The value of the result. </param>
        /// <param name="metadata"> The metadata of the result. </param>
        public Ok(TValue value, IDictionary<string, object> metadata)
            : this(value, readOnlyMetadata: new ReadOnlyDictionary<string, object>(metadata)) { }

        /// <summary> Initializes a new instance of the <see cref="Ok{TValue,TError}"/> class. </summary>
        /// <param name="value"> The value of the result. </param>
        /// <param name="readOnlyMetadata"> The metadata of the result. </param>
        /// <remarks> The metadata passed here will not be protected with a new allocation! </remarks>
        internal Ok(TValue value, IReadOnlyDictionary<string, object> readOnlyMetadata)
            : base(readOnlyMetadata)
        {
            Value = value;
        }

        /// <summary> Converts the result to a new error type. </summary>
        /// <typeparam name="TNewError"> The type of the new error. </typeparam>
        /// <returns> The result with a new error or the existing value. </returns>
        public Ok<TValue, TNewError> As<TNewError>()
        {
            // Do not allocate a new ok if the TNewError == TError
#pragma warning disable CA1508 // Statis analysis does not recognize that TNewValue might be TValue
            if (this is Ok<TValue, TNewError> ok)
                return ok;
#pragma warning restore CA1508
            return new Ok<TValue, TNewError>(Value, Metadata);
        }

        /// <inheritdoc />
        public override bool Equals(Result<TValue, TError>? other)
        {
            return other is Ok<TValue, TError> ok && EqualityComparer<TValue>.Default.Equals(Value, ok.Value);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return Value is null ? 0 : EqualityComparer<TValue>.Default.GetHashCode(Value);
        }

        /// <summary> Deconstructs the result into the underlying value </summary>
        /// <param name="value"> The value of the <see cref="Ok{TValue,TError}"/> result </param>
        public void Deconstruct(out TValue value) => value = Value;

        /// <summary> Deconstructs the result into the underlying value </summary>
        /// <param name="value"> The value of the <see cref="Ok{TValue,TError}"/> result </param>
        /// <param name="metadata"> The metadata of the result </param>
        public void Deconstruct(out TValue value, out IReadOnlyDictionary<string, object> metadata)
        {
            value = Value;
            metadata = Metadata;
        }
    }

    /// <summary> Represents a failed result. </summary>
    /// <seealso href="https://doc.rust-lang.org/std/result/enum.Result.html#variant.Err"/>
    [DebuggerDisplay("{\"Err: \" + Error,nq}")]
    public sealed class Err<TValue, TError> : Result<TValue, TError>
    {
        /// <summary> The error of the result. </summary>
        public TError Error { get; }

        /// <summary> Initializes a new instance of the <see cref="Err{TValue,TError}"/> class. </summary>
        /// <param name="error"> The error of the result. </param>
        public Err(TError error)
            : this(error, readOnlyMetadata: ReadOnlyDictionary<string, object>.Empty) { }

        /// <summary> Initializes a new instance of the <see cref="Err{TValue,TError}"/> class. </summary>
        /// <param name="error"> The error of the result. </param>
        /// <param name="metadata"> The metadata of the result. </param>
        public Err(TError error, IDictionary<string, object> metadata)
            : this(error, readOnlyMetadata: new ReadOnlyDictionary<string, object>(metadata)) { }

        /// <summary> Initializes a new instance of the <see cref="Err{TValue,TError}"/> class. </summary>
        /// <param name="error"> The error of the result. </param>
        /// <param name="readOnlyMetadata"> The metadata of the result. </param>
        /// <remarks> The metadata passed here will not be protected with a new allocation! </remarks>
        internal Err(TError error, IReadOnlyDictionary<string, object> readOnlyMetadata)
            : base(readOnlyMetadata)
        {
            Error = error;
        }

        /// <summary> Converts the result to a new value type. </summary>
        /// <typeparam name="TNewValue"> The type of the new value. </typeparam>
        /// <returns> The result with a new value or the existing error. </returns>
        public Err<TNewValue, TError> As<TNewValue>()
        {
            // Do not allocate a new error if the TNewValue == TValue
#pragma warning disable CA1508 // Statis analysis does not recognize that TNewValue might be TValue
            if (this is Err<TNewValue, TError> err)
                return err;
#pragma warning restore CA1508
            return new Err<TNewValue, TError>(Error, Metadata);
        }

        /// <inheritdoc />
        public override bool Equals(Result<TValue, TError>? other)
        {
            return other is Err<TValue, TError> err && EqualityComparer<TError>.Default.Equals(Error, err.Error);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return Error is null ? 0 : EqualityComparer<TError>.Default.GetHashCode(Error);
        }

        /// <summary> Deconstructs the result into the underlying error </summary>
        /// <param name="error"> The value of the <see cref="Err{TValue,TError}"/> result </param>
        public void Deconstruct(out TError error) => error = Error;

        /// <summary> Deconstructs the result into the underlying error </summary>
        /// <param name="error"> The value of the <see cref="Err{TValue,TError}"/> result </param>
        /// <param name="metadata"> The metadata of the result </param>
        public void Deconstruct(out TError error, out IReadOnlyDictionary<string, object> metadata)
        {
            error = Error;
            metadata = Metadata;
        }
    }
}

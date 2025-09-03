namespace Darp.Results;

partial class Result<TValue, TError>
{
    /// <summary> Represents a successful result. </summary>
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
    }

    /// <summary> Represents a failed result. </summary>
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
        public Result<TNewValue, TError>.Err As<TNewValue>() => new(Error, Metadata);
    }
}

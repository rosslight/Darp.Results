namespace Darp.Results;

/// <summary> Standard errors that can be returned by the <see cref="Result{TValue, TError}"/> class. </summary>
public enum StandardError
{
    /// <summary> The try pattern failed. </summary>
    TryPatternFailed,

    /// <summary> An exception occurred. </summary>
    ExceptionOccured,
}

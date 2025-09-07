using Shouldly;

namespace Darp.Results.Shouldly;

/// <summary> Extensions for working with Shouldly and <see cref="Result{T,E}"/> </summary>
public static class ShouldlyResultExtensions
{
    /// <summary>Asserts Success; on failure includes the error string.</summary>
    /// <param name="result"> The result to assert. </param>
    /// <param name="customMessage"> The custom message to include in the assertion. </param>
    /// <typeparam name="TValue"> The type of the value. </typeparam>
    /// <typeparam name="TError"> The type of the error. </typeparam>
    /// <returns> The value of the result. </returns>
    public static TValue ShouldBeSuccess<TValue, TError>(
        this Result<TValue, TError> result,
        string? customMessage = null
    )
    {
        result.ShouldNotBeNull(customMessage ?? "Result should not be null.");
        if (result.TryGetValue(out TValue? value, out Result<TValue, TError>.Err? error))
            return value;
        string err = StringifyError(error.Error);
        throw new ShouldAssertException(
            customMessage ?? $"Expected result to be Success, but it was Error with error: {err}"
        );
    }

    /// <summary>Asserts Success and the Value equals expected; on failure includes the error string.</summary>
    /// <param name="result"> The result to assert. </param>
    /// <param name="expected"> The expected value. </param>
    /// <param name="customMessage"> The custom message to include in the assertion. </param>
    /// <typeparam name="TValue"> The type of the value. </typeparam>
    /// <typeparam name="TError"> The type of the error. </typeparam>
    /// <returns> The value of the result. </returns>
    public static TValue ShouldHaveValue<TValue, TError>(
        this Result<TValue, TError> result,
        TValue expected,
        string? customMessage = null
    )
    {
        TValue value = result.ShouldBeSuccess(customMessage);
        value.ShouldBe(expected, customMessage);
        return value;
    }

    /// <summary>Asserts Success, then runs additional checks on Value; on failure includes the error string.</summary>
    /// <param name="result"> The result to assert. </param>
    /// <param name="assertions"> The assertions to run on the value. </param>
    /// <param name="customMessage"> The custom message to include in the assertion. </param>
    /// <typeparam name="TValue"> The type of the value. </typeparam>
    /// <typeparam name="TError"> The type of the error. </typeparam>
    /// <returns> The value of the result. </returns>
    public static TValue ShouldHaveValue<TValue, TError>(
        this Result<TValue, TError> result,
        Action<TValue> assertions,
        string? customMessage = null
    )
    {
        assertions.ShouldNotBeNull();
        TValue value = result.ShouldBeSuccess(customMessage);
        assertions(value);
        return value;
    }

    /// <summary>Asserts Error; if it was Success, includes the value string.</summary>
    /// <param name="result"> The result to assert. </param>
    /// <param name="customMessage"> The custom message to include in the assertion. </param>
    /// <typeparam name="TValue"> The type of the value. </typeparam>
    /// <typeparam name="TError"> The type of the error. </typeparam>
    /// <returns> The error of the result. </returns>
    public static TError ShouldBeError<TValue, TError>(this Result<TValue, TError> result, string? customMessage = null)
    {
        result.ShouldNotBeNull(customMessage ?? "Result should not be null.");

        if (result.TryGetError(out TError? error, out Result<TValue, TError>.Ok? ok))
            return error;
        string val = StringifyValue(ok.Value);
        throw new ShouldAssertException(
            customMessage ?? $"Expected result to be Error, but it was Success with value: {val}"
        );
    }

    /// <summary>Asserts Error equals expected; if it was Success, includes the value string.</summary>
    /// <param name="result"> The result to assert. </param>
    /// <param name="expected"> The expected error. </param>
    /// <param name="customMessage"> The custom message to include in the assertion. </param>
    /// <typeparam name="TValue"> The type of the value. </typeparam>
    /// <typeparam name="TError"> The type of the error. </typeparam>
    /// <returns> The error of the result. </returns>
    public static TError ShouldHaveError<TValue, TError>(
        this Result<TValue, TError> result,
        TError expected,
        string? customMessage = null
    )
    {
        TError error = result.ShouldBeError(customMessage);
        error.ShouldBe(expected, customMessage);
        return error;
    }

    /// <summary>Asserts Error, then runs additional checks on Error; if it was Success, includes the value string.</summary>
    /// <param name="result"> The result to assert. </param>
    /// <param name="assertions"> The assertions to run on the error. </param>
    /// <param name="customMessage"> The custom message to include in the assertion. </param>
    /// <typeparam name="TValue"> The type of the value. </typeparam>
    /// <typeparam name="TError"> The type of the error. </typeparam>
    /// <returns> The error of the result. </returns>
    public static TError ShouldHaveError<TValue, TError>(
        this Result<TValue, TError> result,
        Action<TError> assertions,
        string? customMessage = null
    )
    {
        assertions.ShouldNotBeNull();
        TError error = result.ShouldBeError(customMessage);
        assertions(error);
        return error;
    }

    // ---- helpers ----

    private static string StringifyError<TError>(TError? err) =>
        err switch
        {
            null => "<null>",
            Exception ex => $"{ex.GetType().Name}: {ex.Message}",
            _ => err.ToString() ?? "<null>",
        };

    private static string StringifyValue<TValue>(TValue? val) =>
        val switch
        {
            null => "<null>",
            _ => val.ToString() ?? "<null>",
        };
}

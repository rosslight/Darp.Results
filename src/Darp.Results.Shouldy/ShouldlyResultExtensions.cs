using Shouldly;

namespace Darp.Results.Shouldy;

public static class ShouldlyResultExtensions
{
    /// <summary>Asserts Success; on failure includes the error string.</summary>
    public static TValue ShouldBeSuccess<TValue, TError>(
        this Result<TValue, TError> result,
        string? customMessage = null
    )
    {
        result.ShouldNotBeNull(customMessage ?? "Result should not be null.");
        if (result.TryGetValue(out var value))
            return value;
        var err = StringifyError(result.Error);
        throw new ShouldAssertException(
            customMessage ?? $"Expected result to be Success, but it was Error with error: {err}"
        );
    }

    /// <summary>Asserts Success and the Value equals expected; on failure includes the error string.</summary>
    public static TValue ShouldHaveValue<TValue, TError>(
        this Result<TValue, TError> result,
        TValue expected,
        string? customMessage = null
    )
    {
        var value = result.ShouldBeSuccess(customMessage);
        value.ShouldBe(expected, customMessage);
        return value;
    }

    /// <summary>Asserts Success, then runs additional checks on Value; on failure includes the error string.</summary>
    public static TValue ShouldHaveValue<TValue, TError>(
        this Result<TValue, TError> result,
        Action<TValue> assertions,
        string? customMessage = null
    )
    {
        assertions.ShouldNotBeNull();
        var value = result.ShouldBeSuccess(customMessage);
        assertions(value);
        return value;
    }

    /// <summary>Asserts Error; if it was Success, includes the value string.</summary>
    public static TError ShouldBeError<TValue, TError>(
        this Result<TValue, TError> result,
        string? customMessage = null
    )
    {
        result.ShouldNotBeNull(customMessage ?? "Result should not be null.");

        if (result.TryGetError(out var error))
            return error;
        var val = StringifyValue(result.Value);
        throw new ShouldAssertException(
            customMessage ?? $"Expected result to be Error, but it was Success with value: {val}"
        );
    }

    /// <summary>Asserts Error equals expected; if it was Success, includes the value string.</summary>
    public static TError ShouldHaveError<TValue, TError>(
        this Result<TValue, TError> result,
        TError expected,
        string? customMessage = null
    )
    {
        var error = result.ShouldBeError(customMessage);
        error.ShouldBe(expected, customMessage);
        return error;
    }

    /// <summary>Asserts Error, then runs additional checks on Error; if it was Success, includes the value string.</summary>
    public static TError ShouldHaveError<TValue, TError>(
        this Result<TValue, TError> result,
        Action<TError> assertions,
        string? customMessage = null
    )
    {
        assertions.ShouldNotBeNull();
        var error = result.ShouldBeError(customMessage);
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

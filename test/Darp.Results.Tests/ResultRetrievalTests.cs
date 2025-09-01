using Shouldly;

namespace Darp.Results.Tests;

public sealed class ResultRetrievalTests
{
    [Fact]
    public void TryGetValue_Value()
    {
        const int value = 42;

        Result<int, Error> result = value;

        var isOk = result.TryGetValue(out var retrievedValue);
        isOk.ShouldBeTrue();
        retrievedValue.ShouldBe(value);
    }

    [Fact]
    public void TryGetValue_Error()
    {
        const Error error = Error.Error1;

        Result<int, Error> result = error;

        var isOk = result.TryGetValue(out var retrievedValue);
        isOk.ShouldBeFalse();
        retrievedValue.ShouldBe(0);
    }

    [Fact]
    public void TryGetError_Value()
    {
        const int value = 42;

        Result<int, Error> result = value;

        var isError = result.TryGetError(out var retrievedError);
        isError.ShouldBeFalse();
        retrievedError.ShouldBe(default);
    }

    [Fact]
    public void TryGetError_Error()
    {
        const Error error = Error.Error1;

        Result<int, Error> result = error;

        var isError = result.TryGetError(out var retrievedError);
        isError.ShouldBeTrue();
        retrievedError.ShouldBe(error);
    }

    private Result<int, Error> TryGetValueEarlyReturn(Result<string, Error> result)
    {
        if (!result.TryGetValue(out string? value, out Result<int, Error>? errorResult))
            return errorResult;
        return 1;
    }

    private Result<string, Error> TryGetValueEarlyReturn2(Result<string, Error> result)
    {
        if (!result.TryGetValue(out string? value, out Result<string, Error>? errorResult))
            return errorResult;
        return value;
    }

    public Result<string, StandardError> X()
    {
        Result<int, string> r = Result.Try(() => 1).MapError(e => e.Message);
        var result = Result.From<string, int>("42", int.TryParse);

        // Should error
        _ = result.Value;
        _ = result.Error;

        // Warning
        Result.From<string, int>("42", int.TryParse); // Result not checked

        // Should info
        if (result.IsError)
            return result.Error; // Loses optional metadata, replace with if (result.TryGetError(out Result<string, Error> resultWithError)) return resultWithError;
        return r switch
        {
            Result<int, string>.Ok ok => ok.Value.ToString(),
            _ => StandardError.ExceptionOccured,
        };
    }
}

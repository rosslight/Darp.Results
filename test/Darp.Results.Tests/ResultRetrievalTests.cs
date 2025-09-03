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
        if (!result.TryGetValue(out string? value, out Result<int, Error>.Err? errorResult))
            return errorResult;
        return 1;
    }

    private Result<string, Error> TryGetValueEarlyReturn2(Result<string, Error> result)
    {
        if (!result.TryGetValue(out string? value, out Result<string, Error>.Err? errorResult))
            return errorResult;
        return value;
    }

    public Result<string, StandardError> Xx(Result<int, StandardError> r)
    {
        if (!r.TryGetValue(out int value, out Result<string, StandardError>.Err? e1))
            return e1;
        if (r is Result<int, StandardError>.Err err)
            return err.As<string>();
        return "Ok";
    }

    public Result<int, StandardError> Xxx(Result<int, StandardError> r)
    {
        if (!r.TryGetValue(out int v1, out Result<int, StandardError>.Err? e1))
            return e1;
        if (r is Result<int, StandardError>.Err e2)
            return e2;
        if (!r.TryGetValue(out int v2))
            return r;
        return r switch
        {
            Result<int, StandardError>.Ok ok => ok.Value,
            Result<int, StandardError>.Err err => err.Error,
        };
    }

    public Result<string, StandardError> X()
    {
        Result<int, string> r = Result.Try(() => 1).MapError(e => e.Message);
        var result = Result.From<string, int>("42", int.TryParse);

        // Should error
        _ = result.Unwrap();

        // Warning
        Result.From<string, int>("42", int.TryParse); // Result not checked
        // Should info
        //if (result.IsError)
        //    return result.Error; // Loses optional metadata, replace with if (result.TryGetError(out Result<string, Error> resultWithError)) return resultWithError;
        Result<Result<int, string>, string> rr = null!;
        return Result.From<string, int>("42", int.TryParse) switch
        {
            Result<int, StandardError>.Ok => throw new System.NotImplementedException(),
            Result<int, StandardError>.Err => throw new System.NotImplementedException(),
        };
    }
}

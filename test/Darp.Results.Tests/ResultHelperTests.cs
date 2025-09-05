using Darp.Results.Shouldly;

namespace Darp.Results.Tests;

public sealed class ResultFlattenAndFactoriesTests
{
    [Fact]
    public void Flatten_OkOk_ReturnsInnerOk()
    {
        var inner = Result.Ok<int, Error>(1);
        var outer = Result.Ok<Result<int, Error>, Error>(inner);

        var flat = Result.Flatten(outer);

        flat.ShouldHaveValue(1);
    }

    [Fact]
    public void Flatten_OkErr_ReturnsErr()
    {
        var inner = Result.Error<int, Error>(Error.Error1);
        var outer = Result.Ok<Result<int, Error>, Error>(inner);

        var flat = Result.Flatten(outer);

        flat.ShouldHaveError(Error.Error1);
    }

    [Fact]
    public void Flatten_Err_ReturnsOuterErr()
    {
        var outer = Result.Error<Result<int, Error>, Error>(Error.Error2);

        var flat = Result.Flatten(outer);

        flat.ShouldHaveError(Error.Error2);
    }

    [Fact]
    public void Try_Factory_Success_CapturesValue()
    {
        var res = Result.Try(() => 10);
        res.ShouldHaveValue(10);
    }

    [Fact]
    public void Try_Factory_Failure_CapturesException()
    {
        var ex = new InvalidOperationException("boom");
        var res = Result.Try<int>(() => throw ex);
        res.ShouldHaveError(ex);
    }

    [Fact]
    public void From_TryParse_Success()
    {
        var res = Result.From<string, int>("42", int.TryParse);
        res.ShouldHaveValue(42);
    }

    [Fact]
    public void From_TryParse_Failure_ReturnsStandardErrorTryPatternFailed()
    {
        var res = Result.From<string, int>("xx", int.TryParse);
        res.ShouldHaveError(StandardError.TryPatternFailed);
    }

    [Fact]
    public void From_TryParse_Exception_ReturnsStandardErrorExceptionOccured()
    {
        var res = Result.From<string, int>("42", Throwing);
        res.ShouldHaveError(StandardError.ExceptionOccured);
        return;

        static bool Throwing(string _, out int output)
        {
            output = 0;
            throw new Exception("fail");
        }
    }
}

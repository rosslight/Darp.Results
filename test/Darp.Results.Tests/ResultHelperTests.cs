using Darp.Results.Shouldly;

namespace Darp.Results.Tests;

public sealed class ResultFlattenAndFactoriesTests
{
    [Fact]
    public void Flatten_OkOk_ReturnsInnerOk()
    {
        Result<int, Error> inner = new Ok<int>(1);
        Result<Result<int, Error>, Error> outer = new Ok<Result<int, Error>>(inner);

        var flat = Flatten(outer);

        flat.ShouldHaveValue(1);
    }

    [Fact]
    public void Flatten_OkErr_ReturnsErr()
    {
        Result<int, Error> inner = new Err<Error>(Error.Error1);
        Result<Result<int, Error>, Error> outer = new Ok<Result<int, Error>>(inner);

        var flat = Flatten(outer);

        flat.ShouldHaveError(Error.Error1);
    }

    [Fact]
    public void Flatten_Err_ReturnsOuterErr()
    {
        Result<Result<int, Error>, Error> outer = new Err<Error>(Error.Error2);

        var flat = Flatten(outer);

        flat.ShouldHaveError(Error.Error2);
    }

    [Fact]
    public void Try_Factory_Success_CapturesValue()
    {
        var res = Try(() => 10);
        res.ShouldHaveValue(10);
    }

    [Fact]
    public void Try_Factory_Failure_CapturesException()
    {
        var ex = new InvalidOperationException("boom");
        var res = Try<int>(() => throw ex);
        res.ShouldHaveError(ex);
    }

    [Fact]
    public void From_TryParse_Success()
    {
        var res = From<string, int>("42", int.TryParse);
        res.ShouldHaveValue(42);
    }

    [Fact]
    public void From_TryParse_Failure_ReturnsStandardErrorTryPatternFailed()
    {
        var res = From<string, int>("xx", int.TryParse);
        res.ShouldHaveError(StandardError.TryPatternFailed);
    }

    [Fact]
    public void From_TryParse_Exception_ReturnsStandardErrorExceptionOccured()
    {
        var res = From<string, int>("42", Throwing);
        res.ShouldHaveError(StandardError.ExceptionOccured);
        return;

        static bool Throwing(string _, out int output)
        {
            output = 0;
            throw new Exception("fail");
        }
    }
}

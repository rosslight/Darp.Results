using System.Diagnostics;
using Darp.Results.Shouldy;
using Shouldly;

namespace Darp.Results.Tests;

public sealed class ResultLogicalCombinatorsTests
{
    [Fact]
    public void And_OkOk_LateOk()
    {
        Result<uint, Error> x = 2;
        Result<string, Error> y = "4";
        Result<string, Error> result = x.And(y);
        result.ShouldHaveValue("4");
    }

    [Fact]
    public void And_Ok_Err_LateError()
    {
        Result<uint, Error> x = 2;
        Result<string, Error> y = Error.Error2;
        Result<string, Error> result = x.And(y);
        result.ShouldHaveError(Error.Error2);
    }

    [Fact]
    public void And_ErrOk_EarlyError()
    {
        Result<uint, Error> x = Error.Error1;
        Result<string, Error> y = "2";
        Result<string, Error> result = x.And(y);
        result.ShouldHaveError(Error.Error1);
    }

    [Fact]
    public void And_ErrErr_EarlyError()
    {
        Result<uint, Error> x = Error.Error1;
        Result<string, Error> y = Error.Error2;
        Result<string, Error> result = x.And(y);
        result.ShouldHaveError(Error.Error1);
    }

    [Fact]
    public void And_Factory_OkOk_LateOk()
    {
        Result<uint, Error> x = 2;
        Result<string, Error> y = "4";
        Result<string, Error> result = x.And(v =>
        {
            v.ShouldBe(2U);
            return y;
        });
        result.ShouldHaveValue("4");
    }

    [Fact]
    public void And_Factory_Ok_Err_LateError()
    {
        Result<uint, Error> x = 2;
        Result<string, Error> y = Error.Error2;
        Result<string, Error> result = x.And(v =>
        {
            v.ShouldBe(2U);
            return y;
        });
        result.ShouldHaveError(Error.Error2);
    }

    [Fact]
    public void And_Factory_Err_EarlyError()
    {
        Result<uint, Error> x = Error.Error1;
        Result<string, Error> result = x.And<string>(_ => throw new UnreachableException());
        result.ShouldHaveError(Error.Error1);
    }

    [Fact]
    public void Or_WithOk_ReturnsSelf()
    {
        Result<int, Error> a = 5;
        var b = Result.Ok<int, Error>(10);
        Result<int, Error> r = a.Or(b);
        r.ShouldHaveValue(5);
    }

    [Fact]
    public void Or_WithErr_ReturnsProvided()
    {
        Result<int, Error> a = Error.Error1;
        var b = Result.Ok<int, Error>(10);
        Result<int, Error> r = a.Or(b);
        r.ShouldHaveValue(10);
    }

    [Fact]
    public void Or_Lambda_SameError_Err_CallsLambda()
    {
        Result<int, Error> a = Error.Error2;
        Result<int, Error> r = a.Or(e => Result.Ok<int, Error>((int)e));
        r.ShouldHaveValue((int)Error.Error2);
    }

    [Fact]
    public void Or_Lambda_SameError_Ok_SkipsLambda()
    {
        Result<int, Error> a = 3;
        Result<int, Error> r = a.Or(_ => Result.Ok<int, Error>(999));
        r.ShouldHaveValue(3);
    }

    [Fact]
    public void Or_Lambda_NewError_Err_CallsLambda()
    {
        Result<int, Error> a = Error.Error1;
        Result<int, NewError> r = a.Or<NewError>(_ => Result.Error<int, NewError>(NewError.Error2));
        r.ShouldHaveError(NewError.Error2);
    }
}

using System.Diagnostics;
using Darp.Results.Shouldly;
using Shouldly;

namespace Darp.Results.Tests;

public sealed class ResultTransformationTests
{
    [Fact]
    public void Map_Value()
    {
        const int value = 42;
        const string newValue = "newValue";

        Result<int, Error> result = value;
        Result<string, Error> newResult = result.Map(v =>
        {
            v.ShouldBe(value);
            return newValue;
        });

        result.ShouldHaveValue(value);
        newResult.ShouldHaveValue(newValue);
    }

    [Fact]
    public void Map_Error()
    {
        const Error error = Error.Error1;

        Result<int, Error> result = error;
        Result<string, Error> newResult = result.Map<string>(_ => throw new UnreachableException());

        result.ShouldHaveError(error);
        newResult.ShouldHaveError(error);
    }

    [Fact]
    public void MapError_Value()
    {
        const int value = 42;

        Result<int, Error> result = value;
        Result<int, NewError> newResult = result.MapError<NewError>(_ => throw new UnreachableException());

        result.ShouldHaveValue(value);
        newResult.ShouldHaveValue(value);
    }

    [Fact]
    public void MapError_Error()
    {
        const Error error = Error.Error1;
        const NewError newError = NewError.Error2;

        Result<int, Error> result = error;
        Result<int, NewError> newResult = result.MapError(err =>
        {
            err.ShouldBe(error);
            return newError;
        });

        result.ShouldHaveError(error);
        newResult.ShouldHaveError(newError);
        _ = result.Map(x => "").Unwrap("132");
    }
}

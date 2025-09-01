using System.Diagnostics;
using Darp.Results.Shouldy;
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
        var newResult = result.Map(v =>
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
        var newResult = result.Map<string>(_ => throw new UnreachableException());

        result.ShouldHaveError(error);
        newResult.ShouldHaveError(error);
    }

    [Fact]
    public void MapOr_Value()
    {
        const int value = 42;
        const string newValue = "newValue";
        const string defaultValue = "defaultValue";

        Result<int, Error> result = value;
        var mappedValue = result.MapOr(
            v =>
            {
                v.ShouldBe(value);
                return newValue;
            },
            defaultValue
        );

        result.ShouldHaveValue(value);
        mappedValue.ShouldBe(newValue);
    }

    [Fact]
    public void MapOr_Error()
    {
        const Error error = Error.Error1;
        const string defaultValue = "defaultValue";

        Result<int, Error> result = error;
        var mappedValue = result.MapOr<string>(_ => throw new UnreachableException(), defaultValue);

        result.ShouldHaveError(error);
        mappedValue.ShouldBe(defaultValue);
    }

    [Fact]
    public void MapError_Value()
    {
        const int value = 42;

        Result<int, Error> result = value;
        var newResult = result.MapError<NewError>(_ => throw new UnreachableException());

        result.ShouldHaveValue(value);
        newResult.ShouldHaveValue(value);
    }

    [Fact]
    public void MapError_Error()
    {
        const Error error = Error.Error1;
        const NewError newError = NewError.Error2;

        Result<int, Error> result = error;
        var newResult = result.MapError(err =>
        {
            err.ShouldBe(error);
            return newError;
        });

        result.ShouldHaveError(error);
        newResult.ShouldHaveError(newError);
    }
}

using Darp.Results.Shouldy;
using Shouldly;

namespace Darp.Results.Tests;

public sealed class ResultCreationTests
{
    [Fact]
    public void ImplicitConversion_Value()
    {
        const int value = 42;

        Result<int, Error> result = value;

        result.ShouldHaveValue(value);
    }

    [Fact]
    public void ImplicitConversion_Error()
    {
        const Error error = Error.Error1;

        Result<int, Error> result = error;

        result.ShouldHaveError(error);
    }

    [Fact]
    public void Factory_Value()
    {
        const int value = 42;

        var result = Result.Ok<int, Error>(value);

        result.ShouldHaveValue(value);
    }

    [Fact]
    public void Factory_Error()
    {
        const Error error = Error.Error1;

        var result = Result.Error<int, Error>(error);

        result.ShouldHaveError(error);
    }

    [Fact]
    public void Factory_WithMetadata_Value()
    {
        const int value = 42;
        var metadata = new Dictionary<string, object> { ["Key1"] = "Value1", ["Key2"] = 2 };

        var result = Result.Ok<int, Error>(value, metadata);

        result.ShouldHaveValue(value);
        result.Metadata.ShouldBe(metadata);
    }

    [Fact]
    public void Factory_WithMetadata_Error()
    {
        const Error error = Error.Error1;
        var metadata = new Dictionary<string, object> { ["Key1"] = "Value1", ["Key2"] = 2 };

        var result = Result.Error<int, Error>(error, metadata);

        result.ShouldHaveError(error);
        result.Metadata.ShouldBe(metadata);
    }

    [Fact]
    public void WithMetadata_Value()
    {
        const int value = 42;
        var metadata = new Dictionary<string, object> { ["Key1"] = "Value1", ["Key2"] = 2 };
        var result = Result.Ok<int, Error>(value);
        var newResult = result.WithMetadata(metadata);

        result.ShouldHaveValue(value);
        result.Metadata.Count.ShouldBe(0);
        newResult.ShouldHaveValue(value);
        newResult.Metadata.ShouldBe(metadata);
    }

    [Fact]
    public void WithMetadata_Error()
    {
        const Error error = Error.Error1;
        var metadata = new Dictionary<string, object> { ["Key1"] = "Value1", ["Key2"] = 2 };

        var result = Result.Error<int, Error>(error);
        var newResult = result.WithMetadata(metadata);

        result.ShouldHaveError(error);
        result.Metadata.Count.ShouldBe(0);
        newResult.ShouldHaveError(error);
        newResult.Metadata.ShouldBe(metadata);
    }
}

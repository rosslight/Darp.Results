using Darp.Results.Shouldly;
using Shouldly;

namespace Darp.Results.Tests;

public sealed class ResultMetadataTests
{
    [Fact]
    public void WithMetadata_KeyValue_Value()
    {
        const int value = 42;
        var result = Result.Ok<int, Error>(value);

        Result<int, Error> newResult = result.WithMetadata("TraceId", "abc");

        result.ShouldHaveValue(value);
        result.Metadata.Count.ShouldBe(0);
        newResult.ShouldHaveValue(value);
        newResult.Metadata.ShouldBe(new Dictionary<string, object> { ["TraceId"] = "abc" });
    }

    [Fact]
    public void WithMetadata_KeyValue_Error()
    {
        const Error error = Error.Error1;
        var result = Result.Error<int, Error>(error);

        Result<int, Error> newResult = result.WithMetadata("TraceId", 123);

        result.ShouldHaveError(error);
        result.Metadata.Count.ShouldBe(0);
        newResult.ShouldHaveError(error);
        newResult.Metadata.ShouldBe(new Dictionary<string, object> { ["TraceId"] = 123 });
    }

    [Fact]
    public void WithMetadata_Value()
    {
        const int value = 42;
        var metadata = new Dictionary<string, object> { ["Key1"] = "Value1", ["Key2"] = 2 };
        var result = Result.Ok<int, Error>(value);
        Result<int, Error> newResult = result.WithMetadata(metadata);

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
        Result<int, Error> newResult = result.WithMetadata(metadata);

        result.ShouldHaveError(error);
        result.Metadata.Count.ShouldBe(0);
        newResult.ShouldHaveError(error);
        newResult.Metadata.ShouldBe(metadata);
    }
}

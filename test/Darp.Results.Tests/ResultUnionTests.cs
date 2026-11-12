using System.Runtime.CompilerServices;
using Darp.Results.Shouldly;
using Shouldly;

namespace Darp.Results.Tests;

public sealed class ResultUnionTests
{
    private static string Describe(Result<int, string> result) =>
        result switch
        {
            Ok<int>(var value) => $"ok {value}",
            Err<string>(var error) => $"err {error}",
        };

    [Fact]
    public void Switch_CoversBothCases()
    {
        Describe(Result.Ok(1)).ShouldBe("ok 1");
        Describe(Result.Err("x")).ShouldBe("err x");
    }

    [Fact]
    public void Default_IsNeitherOkNorErr()
    {
        Result<int, string> result = default;

        result.IsOk.ShouldBeFalse();
        result.IsErr.ShouldBeFalse();
        Should.Throw<SwitchExpressionException>(() => Describe(result));
        Should.Throw<InvalidOperationException>(() => result.Map(x => x + 1));
    }

    [Fact]
    public void SameValueAndErrorType_KeepsCasesApart()
    {
        Result<string, string> ok = Ok("a");
        Result<string, string> err = Err("a");

        ok.ShouldHaveValue("a");
        err.ShouldHaveError("a");
        ok.ShouldNotBe(err);
    }

    [Fact]
    public void ErrorCase_ConvertsToResultWithOtherValueType()
    {
        Result<int, string> source = Result.Err("x");
        source = source.WithMetadata("k", 1);

        static Result<bool, string> Forward(Result<int, string> r) =>
            r.TryGetValue(out int value, out Err<string> err) ? value > 0 : err;

        Forward(source).ShouldHaveError("x");
        source.Map(x => x > 0).Metadata["k"].ShouldBe(1);
    }

    [Fact]
    public void Equality_IgnoresMetadata()
    {
        Result<int, string> plain = Result.Ok(1);
        Result<int, string> withMetadata = plain.WithMetadata("k", 1);

        withMetadata.ShouldBe(plain);
        withMetadata.GetHashCode().ShouldBe(plain.GetHashCode());
    }

    [Fact]
    public void EarlyReturn_KeepsMetadata()
    {
        Result<int, string> source = new Err<string>("x", new Dictionary<string, object> { ["k"] = 1 });

        static Result<bool, string> Forward(Result<int, string> r)
        {
            if (!r.TryGetValue(out int value, out Err<string> err))
                return err;
            return value > 0;
        }

        Result<bool, string> forwarded = Forward(source);
        forwarded.ShouldHaveError("x");
        forwarded.Metadata["k"].ShouldBe(1);
    }

    [Fact]
    public void TryGetError_OkCase_KeepsMetadata()
    {
        Result<int, string> source = Result.Ok(5);
        source = source.WithMetadata("k", 1);

        source.TryGetError(out _, out Ok<int> ok).ShouldBeFalse();
        Result<int, bool> moved = ok;

        moved.ShouldHaveValue(5);
        moved.Metadata["k"].ShouldBe(1);
    }

    [Fact]
    public void Flatten_OuterErr_KeepsMetadata()
    {
        Result<Result<int, string>, string> outer = new Err<string>("x", new Dictionary<string, object> { ["k"] = 1 });

        Result<int, string> flat = Result.Flatten(outer);

        flat.ShouldHaveError("x");
        flat.Metadata["k"].ShouldBe(1);
    }

    [Fact]
    public void Pattern_DeconstructsMetadata()
    {
        Result<int, string> result = new Ok<int>(5, new Dictionary<string, object> { ["k"] = 1 });

        object? metadataValue = result switch
        {
            Ok<int>(_, var metadata) => metadata["k"],
            Err<string> => null,
        };

        metadataValue.ShouldBe(1);
    }

    [Fact]
    public void CaseEquality_IgnoresMetadata()
    {
        var withMetadata = new Err<string>("x", new Dictionary<string, object> { ["k"] = 1 });

        withMetadata.ShouldBe(new Err<string>("x"));
        withMetadata.GetHashCode().ShouldBe(new Err<string>("x").GetHashCode());
    }
}

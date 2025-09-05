using Shouldly;

namespace Darp.Results.Tests;

public sealed class ResultGetterAndIterationTests
{
    [Fact]
    public void IEnumerable_ForOk_YieldsSingleValue()
    {
        Result<int, Error> result = 7;
        result.AsEnumerable().ToArray().ShouldBe([7]);
    }

    [Fact]
    public void IEnumerable_ForErr_YieldsEmpty()
    {
        Result<int, Error> result = Error.Error1;
        result.AsEnumerable().Any().ShouldBeFalse();
    }

    [Fact]
    public void GetEnumerator_ForOk_YieldsSingleValue()
    {
        Result<int, Error> result = 7;
        using IEnumerator<int> enumerator = result.GetEnumerator();

        enumerator.MoveNext().ShouldBeTrue();
        enumerator.Current.ShouldBe(7);
        enumerator.MoveNext().ShouldBeFalse();
    }

    [Fact]
    public void GetEnumerator_ForErr_YieldsEmpty()
    {
        Result<int, Error> result = Error.Error1;
        using IEnumerator<int> enumerator = result.GetEnumerator();

        enumerator.MoveNext().ShouldBeFalse();
    }
}

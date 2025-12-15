using Darp.Results.Shouldly;
using Shouldly;

namespace Darp.Results.Tests;

public sealed class TaskResultExtensionsTests
{
    [Fact]
    public async Task Map_Ok_TransformsValue()
    {
        var task = Task.FromResult<Result<int, Error>>(2);

        Result<string, Error> result = await task.Map(v =>
        {
            v.ShouldBe(2);
            return "mapped";
        });

        result.ShouldHaveValue("mapped");
    }

    [Fact]
    public async Task Map_Err_DoesNotInvokeSelector()
    {
        var task = Task.FromResult<Result<int, Error>>(Error.Error1);
        bool called = false;

        Result<string, Error> result = await task.Map(_ =>
        {
            called = true;
            return "should-not-run";
        });

        called.ShouldBeFalse();
        result.ShouldHaveError(Error.Error1);
    }

    [Fact]
    public async Task MapError_Err_TransformsError()
    {
        var task = Task.FromResult<Result<int, Error>>(Error.Error2);

        Result<int, NewError> result = await task.MapError(err =>
        {
            err.ShouldBe(Error.Error2);
            return NewError.Error1;
        });

        result.ShouldHaveError(NewError.Error1);
    }

    [Fact]
    public async Task MapError_Ok_DoesNotInvokeSelector()
    {
        var task = Task.FromResult<Result<int, Error>>(7);
        bool called = false;

        Result<int, NewError> result = await task.MapError(_ =>
        {
            called = true;
            return NewError.Error2;
        });

        called.ShouldBeFalse();
        result.ShouldHaveValue(7);
    }

    [Fact]
    public async Task And_Ok_Ok_ReturnsSecond()
    {
        var first = Task.FromResult<Result<int, Error>>(1);
        Result<string, Error> second = "next";

        Result<string, Error> result = await first.And(second);

        result.ShouldHaveValue("next");
    }

    [Fact]
    public async Task And_Ok_Err_ReturnsErr()
    {
        var first = Task.FromResult<Result<int, Error>>(1);
        Result<string, Error> second = Error.Error2;

        Result<string, Error> result = await first.And(second);

        result.ShouldHaveError(Error.Error2);
    }

    [Fact]
    public async Task And_Err_Ok_DoesNotInvokeFactory()
    {
        var first = Task.FromResult<Result<int, Error>>(Error.Error1);
        bool called = false;

        Result<string, Error> result = await first.And(_ =>
        {
            called = true;
            return "should-not-run";
        });

        called.ShouldBeFalse();
        result.ShouldHaveError(Error.Error1);
    }

    [Fact]
    public async Task And_Ok_UsesFactory()
    {
        var first = Task.FromResult<Result<int, Error>>(3);

        Result<string, Error> result = await first.And(v =>
        {
            v.ShouldBe(3);
            return "factory";
        });

        result.ShouldHaveValue("factory");
    }

    [Fact]
    public async Task And_Ok_ValueFactoryIsWrappedAsOk()
    {
        var first = Task.FromResult<Result<int, Error>>(4);

        Result<string, Error> result = await first.And(v =>
        {
            v.ShouldBe(4);
            return v.ToString();
        });

        result.ShouldHaveValue("4");
    }

    [Fact]
    public async Task And_Err_ValueFactoryIsNotInvoked()
    {
        var first = Task.FromResult<Result<int, Error>>(Error.Error1);
        bool called = false;

        Result<string, Error> result = await first.And(_ =>
        {
            called = true;
            return "should-not-run";
        });

        called.ShouldBeFalse();
        result.ShouldHaveError(Error.Error1);
    }

    [Fact]
    public async Task Or_Err_Ok_UsesFallback()
    {
        var first = Task.FromResult<Result<int, Error>>(Error.Error2);
        Result<int, Error> fallback = 99;

        Result<int, Error> result = await first.Or(fallback);

        result.ShouldHaveValue(99);
    }

    [Fact]
    public async Task Or_Ok_SkipsFallback()
    {
        var first = Task.FromResult<Result<int, Error>>(5);
        bool called = false;

        Result<int, Error> result = await first.Or(_ =>
        {
            called = true;
            return Error.Error1;
        });

        called.ShouldBeFalse();
        result.ShouldHaveValue(5);
    }

    [Fact]
    public async Task Or_Err_UsesFallbackFactory()
    {
        var first = Task.FromResult<Result<int, Error>>(Error.Error1);

        Result<int, NewError> result = await first.Or(err =>
        {
            err.ShouldBe(Error.Error1);
            return NewError.Error2;
        });

        result.ShouldHaveError(NewError.Error2);
    }

    [Fact]
    public async Task Or_Err_ErrorFactoryIsWrappedAsErr()
    {
        var first = Task.FromResult<Result<int, Error>>(Error.Error2);

        Result<int, NewError> result = await first.Or(err =>
        {
            err.ShouldBe(Error.Error2);
            return NewError.Error1;
        });

        result.ShouldHaveError(NewError.Error1);
    }

    [Fact]
    public async Task Or_Ok_ErrorFactoryIsNotInvoked()
    {
        var first = Task.FromResult<Result<int, Error>>(8);
        bool called = false;

        Result<int, NewError> result = await first.Or(_ =>
        {
            called = true;
            return NewError.Error2;
        });

        called.ShouldBeFalse();
        result.ShouldHaveValue(8);
    }

    [Fact]
    public async Task Unwrap_Ok_ReturnsValue()
    {
        var task = Task.FromResult<Result<int, Error>>(10);

        int value = await task.Unwrap();

        value.ShouldBe(10);
    }

    [Fact]
    public async Task Unwrap_Err_Throws()
    {
        var task = Task.FromResult<Result<int, Error>>(Error.Error1);

        await Should.ThrowAsync<InvalidOperationException>(() => task.Unwrap());
    }

    [Fact]
    public async Task Unwrap_Default_ReturnsDefaultOnErr()
    {
        var task = Task.FromResult<Result<int, Error>>(Error.Error1);

        int value = await task.Unwrap(123);

        value.ShouldBe(123);
    }

    [Fact]
    public async Task Unwrap_Provider_UsesProviderOnErr()
    {
        var task = Task.FromResult<Result<int, Error>>(Error.Error2);

        int value = await task.Unwrap(err =>
        {
            err.ShouldBe(Error.Error2);
            return 321;
        });

        value.ShouldBe(321);
    }

    [Fact]
    public async Task UnwrapOrDefault_Err_ReturnsDefault()
    {
        var task = Task.FromResult<Result<string, Error>>(Error.Error1);

        string? value = await task.UnwrapOrDefault();

        value.ShouldBeNull();
    }

    [Fact]
    public async Task UnwrapError_Err_ReturnsError()
    {
        var task = Task.FromResult<Result<int, Error>>(Error.Error2);

        Error error = await task.UnwrapError();

        error.ShouldBe(Error.Error2);
    }

    [Fact]
    public async Task UnwrapError_Ok_Throws()
    {
        var task = Task.FromResult<Result<int, Error>>(7);

        await Should.ThrowAsync<InvalidOperationException>(() => task.UnwrapError());
    }

    [Fact]
    public async Task Expect_Ok_ReturnsValue()
    {
        var task = Task.FromResult<Result<int, Error>>(11);

        int value = await task.Expect("should not throw");

        value.ShouldBe(11);
    }

    [Fact]
    public async Task Expect_Err_ThrowsWithMessage()
    {
        var task = Task.FromResult<Result<int, Error>>(Error.Error1);
        const string message = "boom";

        InvalidOperationException ex = await Should.ThrowAsync<InvalidOperationException>(() => task.Expect(message));
        ex.Message.ShouldBe(message);
    }

    [Fact]
    public async Task ExpectError_Err_ReturnsError()
    {
        var task = Task.FromResult<Result<int, Error>>(Error.Error2);

        Error error = await task.ExpectError("should not throw");

        error.ShouldBe(Error.Error2);
    }

    [Fact]
    public async Task ExpectError_Ok_ThrowsWithMessage()
    {
        var task = Task.FromResult<Result<int, Error>>(42);
        const string message = "wrong state";

        InvalidOperationException ex = await Should.ThrowAsync<InvalidOperationException>(() =>
            task.ExpectError(message)
        );
        ex.Message.ShouldBe(message);
    }
}

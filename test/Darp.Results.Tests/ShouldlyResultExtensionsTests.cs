using Darp.Results.Shouldly;
using Shouldly;

namespace Darp.Results.Tests;

public sealed class ShouldlyResultExtensionsTests
{
    [Fact]
    public async Task ShouldBeSuccess_TaskOk_ReturnsValue()
    {
        var task = Task.FromResult<Result<int, Error>>(12);

        int value = await task.ShouldBeSuccess();

        value.ShouldBe(12);
    }

    [Fact]
    public async Task ShouldBeSuccess_TaskErr_ThrowsWithMessage()
    {
        var task = Task.FromResult<Result<int, Error>>(Error.Error1);
        const string message = "expected success";

        ShouldAssertException ex = await Assert.ThrowsAsync<ShouldAssertException>(async () =>
            await task.ShouldBeSuccess(message)
        );
        ex.Message.ShouldBe(message);
    }

    [Fact]
    public async Task ShouldHaveValue_TaskOk_ReturnsValue()
    {
        var task = Task.FromResult<Result<int, Error>>(13);

        int value = await task.ShouldHaveValue(13);

        value.ShouldBe(13);
    }

    [Fact]
    public async Task ShouldHaveValue_TaskOk_RunsAssertions()
    {
        var task = Task.FromResult<Result<int, Error>>(14);
        bool called = false;

        int value = await task.ShouldHaveValue(v =>
        {
            called = true;
            v.ShouldBe(14);
        });

        called.ShouldBeTrue();
        value.ShouldBe(14);
    }

    [Fact]
    public async Task ShouldHaveValue_TaskErr_DoesNotRunAssertions()
    {
        var task = Task.FromResult<Result<int, Error>>(Error.Error1);
        bool called = false;

        await Assert.ThrowsAsync<ShouldAssertException>(async () =>
            await task.ShouldHaveValue(_ =>
            {
                called = true;
            })
        );

        called.ShouldBeFalse();
    }

    [Fact]
    public async Task ShouldHaveValue_TaskOkWrongValue_ThrowsWithMessage()
    {
        var task = Task.FromResult<Result<int, Error>>(15);
        const string message = "wrong value";

        ShouldAssertException ex = await Assert.ThrowsAsync<ShouldAssertException>(async () =>
            await task.ShouldHaveValue(16, message)
        );
        ex.Message.ShouldContain(message);
    }

    [Fact]
    public async Task ShouldBeError_TaskErr_ReturnsError()
    {
        var task = Task.FromResult<Result<int, Error>>(Error.Error2);

        Error error = await task.ShouldBeError();

        error.ShouldBe(Error.Error2);
    }

    [Fact]
    public async Task ShouldBeError_TaskOk_ThrowsWithMessage()
    {
        var task = Task.FromResult<Result<int, Error>>(17);
        const string message = "expected error";

        ShouldAssertException ex = await Assert.ThrowsAsync<ShouldAssertException>(async () =>
            await task.ShouldBeError(message)
        );
        ex.Message.ShouldBe(message);
    }

    [Fact]
    public async Task ShouldHaveError_TaskErr_ReturnsError()
    {
        var task = Task.FromResult<Result<int, Error>>(Error.Error1);

        Error error = await task.ShouldHaveError(Error.Error1);

        error.ShouldBe(Error.Error1);
    }

    [Fact]
    public async Task ShouldHaveError_TaskErr_RunsAssertions()
    {
        var task = Task.FromResult<Result<int, Error>>(Error.Error2);
        bool called = false;

        Error error = await task.ShouldHaveError(err =>
        {
            called = true;
            err.ShouldBe(Error.Error2);
        });

        called.ShouldBeTrue();
        error.ShouldBe(Error.Error2);
    }

    [Fact]
    public async Task ShouldHaveError_TaskOk_DoesNotRunAssertions()
    {
        var task = Task.FromResult<Result<int, Error>>(18);
        bool called = false;

        await Assert.ThrowsAsync<ShouldAssertException>(async () =>
            await task.ShouldHaveError(_ =>
            {
                called = true;
            })
        );

        called.ShouldBeFalse();
    }

    [Fact]
    public async Task ShouldHaveError_TaskErrWrongError_ThrowsWithMessage()
    {
        var task = Task.FromResult<Result<int, Error>>(Error.Error1);
        const string message = "wrong error";

        ShouldAssertException ex = await Assert.ThrowsAsync<ShouldAssertException>(async () =>
            await task.ShouldHaveError(Error.Error2, message)
        );
        ex.Message.ShouldContain(message);
    }

    [Fact]
    public async Task ShouldBeSuccess_NullTask_ThrowsArgumentNullException()
    {
        Task<Result<int, Error>>? task = null;

        await Should.ThrowAsync<ArgumentNullException>(() => task!.ShouldBeSuccess());
    }

    [Fact]
    public async Task ShouldBeError_NullTask_ThrowsArgumentNullException()
    {
        Task<Result<int, Error>>? task = null;

        await Should.ThrowAsync<ArgumentNullException>(() => task!.ShouldBeError());
    }
}

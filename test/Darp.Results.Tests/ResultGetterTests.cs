using Shouldly;

namespace Darp.Results.Tests;

public sealed class ResultGetterTests
{
    [Fact]
    public void TryGetValue_WithOutErr_PropagatesErrType()
    {
        Result<int, Error> result = Error.Error2;
        bool ok = result.TryGetValue(out int v, out Err<string, Error>? e);
        ok.ShouldBeFalse();
        v.ShouldBe(0);
        _ = e.ShouldNotBeNull();
        e.Error.ShouldBe(Error.Error2);
    }

    [Fact]
    public void TryGetError_WithOutOk_PropagatesOkType()
    {
        Result<int, Error> result = 5;
        bool isErr = result.TryGetError(out Error err, out Ok<int, NewError>? ok);
        isErr.ShouldBeFalse();
        err.ShouldBe(default);
        _ = ok.ShouldNotBeNull();
        ok.Value.ShouldBe(5);
    }

    [Fact]
    public void Unwrap_Value_ReturnsValue()
    {
        Result<int, Error> result = 1;
        int value = result.Unwrap();
        value.ShouldBe(1);
    }

    [Fact]
    public void Unwrap_Err_Throws()
    {
        Result<int, Error> result = Error.Error1;
        InvalidOperationException ex = Should.Throw<InvalidOperationException>(() => _ = result.Unwrap());
        ex.Message.ShouldBe("Called Result.Unwrap() on an Err value: Error1");
    }

    [Fact]
    public void Unwrap_Ok_WithDefault_ReturnsValue()
    {
        Result<int, Error> result = 2;
        int value = result.Unwrap(10);
        value.ShouldBe(2);
    }

    [Fact]
    public void Unwrap_Err_WithDefault_ReturnsDefault()
    {
        Result<int, Error> result = Error.Error1;
        int value = result.Unwrap(10);
        value.ShouldBe(10);
    }

    [Fact]
    public void Unwrap_Err_WithFactory_ReturnsFactoryValue()
    {
        Result<int, Error> result = Error.Error2;
        int value = result.Unwrap(err =>
        {
            err.ShouldBe(Error.Error2);
            return 123;
        });
        value.ShouldBe(123);
    }

    [Fact]
    public void Expect_Err_ThrowsWithMessage()
    {
        Result<int, Error> result = Error.Error1;
        InvalidOperationException ex = Should.Throw<InvalidOperationException>(() => _ = result.Expect("custom"));
        ex.Message.ShouldBe("custom: Error1");
    }

    [Fact]
    public void Expect_ErrWithNullError_ThrowsWithNullPayload()
    {
        Result<int, string?> result = new Err<int, string?>(null);

        InvalidOperationException ex = Should.Throw<InvalidOperationException>(() => _ = result.Expect("custom"));

        ex.Message.ShouldBe("custom: <null>");
    }

    [Fact]
    public void Expect_ErrWithThrowingToString_ThrowsWithFallbackPayload()
    {
        Result<int, ThrowingToString> result = new Err<int, ThrowingToString>(new ThrowingToString());

        InvalidOperationException ex = Should.Throw<InvalidOperationException>(() => _ = result.Expect("custom"));

        ex.Message.ShouldBe(
            "custom: <Darp.Results.Tests.ResultGetterTests+ThrowingToString.ToString() threw System.InvalidOperationException>"
        );
    }

    [Fact]
    public void UnwrapError_Err_ReturnsError()
    {
        Result<int, Error> result = Error.Error2;
        Error error = result.UnwrapError();
        error.ShouldBe(Error.Error2);
    }

    [Fact]
    public void UnwrapError_Ok_Throws()
    {
        Result<int, Error> result = 1;
        InvalidOperationException ex = Should.Throw<InvalidOperationException>(() => _ = result.UnwrapError());
        ex.Message.ShouldBe("Called Result.UnwrapError() on an Ok value: 1");
    }

    [Fact]
    public void ExpectError_Ok_ThrowsWithMessage()
    {
        Result<int, Error> result = 1;
        InvalidOperationException ex = Should.Throw<InvalidOperationException>(() => _ = result.ExpectError("err-msg"));
        ex.Message.ShouldBe("err-msg: 1");
    }

    [Fact]
    public void ExpectError_OkWithNullValue_ThrowsWithNullPayload()
    {
        Result<string?, Error> result = new Ok<string?, Error>(null);

        InvalidOperationException ex = Should.Throw<InvalidOperationException>(() => _ = result.ExpectError("err-msg"));

        ex.Message.ShouldBe("err-msg: <null>");
    }

    [Fact]
    public void UnwrapOrDefault_Ok_ValueType_ReturnsValue()
    {
        Result<int, Error> result = 7;
        int value = result.UnwrapOrDefault();
        value.ShouldBe(7);
    }

    [Fact]
    public void UnwrapOrDefault_Err_ValueType_ReturnsDefault()
    {
        Result<int, Error> result = Error.Error1;
        int value = result.UnwrapOrDefault();
        value.ShouldBe(0);
    }

    [Fact]
    public void UnwrapOrDefault_Ok_ReferenceType_ReturnsValue()
    {
        Result<string, Error> result = "abc";
        string? value = result.UnwrapOrDefault();
        value.ShouldBe("abc");
    }

    [Fact]
    public void UnwrapOrDefault_Err_ReferenceType_ReturnsNull()
    {
        Result<string, Error> result = Error.Error2;
        string? value = result.UnwrapOrDefault();
        value.ShouldBeNull();
    }

    private sealed class ThrowingToString
    {
        public override string ToString() => throw new InvalidOperationException();
    }
}

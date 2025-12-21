namespace AStar.Dev.Functional.Extensions.Tests.Unit;

public class ConvenienceResultExtensionsShould
{
    [Fact]
    public void ReturnTheValueFromGetOrThrowOnSuccess()
    {
        var ok = new Result<int, Exception>.Ok(123);

        var value = ok.GetOrThrow();

        value.ShouldBe(123);
    }

    [Fact]
    public void ThrowTheCapturedExceptionFromGetOrThrowOnError()
    {
        var ex = new InvalidOperationException("boom");
        var err = new Result<int, Exception>.Error(ex);

        InvalidOperationException thrown = Should.Throw<InvalidOperationException>(() => err.GetOrThrow());

        thrown.ShouldBeSameAs(ex);
    }

    [Fact]
    public async Task ReturnTheValueFromGetOrThrowAsyncOnSuccess()
    {
        static Task<Result<string, Exception>> ResultTask()
        {
            return Task.FromResult<Result<string, Exception>>(new Result<string, Exception>.Ok("ok"));
        }

        var value = await ResultTask().GetOrThrowAsync();

        value.ShouldBe("ok");
    }

    [Fact]
    public async Task ThrowTheCapturedExceptionFromGetOrThrowAsyncOnError()
    {
        var ex = new Exception("bad");

        Task<Result<string, Exception>> ResultTask()
        {
            return Task.FromResult<Result<string, Exception>>(new Result<string, Exception>.Error(ex));
        }

        Exception thrown = await Should.ThrowAsync<Exception>(async () => await ResultTask().GetOrThrowAsync());

        thrown.ShouldBeSameAs(ex);
    }

    [Fact]
    public void ReturnEmptyStringFromToErrorMessageOnSuccess()
    {
        var ok = new Result<bool, Exception>.Ok(true);

        var msg = ok.ToErrorMessage();

        msg.ShouldBe(string.Empty);
    }

    [Fact]
    public void ReturnExceptionMessageFromToErrorMessageOnError()
    {
        var ex = new Exception("something went wrong");
        var err = new Result<object, Exception>.Error(ex);

        var msg = err.ToErrorMessage();

        msg.ShouldBe("something went wrong");
    }
}

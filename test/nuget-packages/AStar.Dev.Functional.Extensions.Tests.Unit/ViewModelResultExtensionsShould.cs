namespace AStar.Dev.Functional.Extensions.Tests.Unit;

public class ViewModelResultExtensionsShould
{
    [Fact]
    public void ApplyInvokeOnSuccessForOk()
    {

        var ok = new Result<string, Exception>.Ok("value");
        string? captured = null;
        var errorCalled = false;

        ok.Apply(v => captured = v, _ => errorCalled = true);

        captured.ShouldBe("value");
        errorCalled.ShouldBeFalse();
    }

    [Fact]
    public void ApplyInvokeOnErrorWhenErrorAndHandlerProvided()
    {

        var ex = new InvalidOperationException("boom");
        var err = new Result<int, Exception>.Error(ex);
        Exception? captured = null;
        var successCalled = false;

        err.Apply(_ => successCalled = true, e => captured = e);

        successCalled.ShouldBeFalse();
        captured.ShouldBeSameAs(ex);
    }

    [Fact]
    public void ApplyDoNothingOnErrorWhenNoOnErrorProvided()
    {

        var err = new Result<int, Exception>.Error(new Exception("x"));
        var successCalled = false;

        err.Apply(_ => successCalled = true);

        successCalled.ShouldBeFalse();
    }

    [Fact]
    public async Task ApplyAsyncWithActionHandlersInvokesSuccessForOk()
    {

        static Task<Result<int, Exception>> TaskOk()
        {
            return Task.FromResult<Result<int, Exception>>(new Result<int, Exception>.Ok(7));
        }

        var captured = 0;
        var errorCalled = false;

        await TaskOk().ApplyAsync(v => captured = v, _ => errorCalled = true);

        captured.ShouldBe(7);
        errorCalled.ShouldBeFalse();
    }

    [Fact]
    public async Task ApplyAsyncWithActionHandlersInvokesErrorForError()
    {

        var ex = new Exception("bad");

        Task<Result<int, Exception>> TaskErr()
        {
            return Task.FromResult<Result<int, Exception>>(new Result<int, Exception>.Error(ex));
        }

        Exception? captured = null;
        var successCalled = false;

        await TaskErr().ApplyAsync(_ => successCalled = true, e => captured = e);

        successCalled.ShouldBeFalse();
        captured.ShouldBeSameAs(ex);
    }

    [Fact]
    public async Task ApplyAsyncWithAsyncHandlersInvokesSuccessForOk()
    {

        static Task<Result<string, Exception>> TaskOk()
        {
            return Task.FromResult<Result<string, Exception>>(new Result<string, Exception>.Ok("ok"));
        }

        string? captured = null;
        var errorCalled = false;

        await TaskOk().ApplyAsync(async v =>
        {
            await Task.Delay(1);
            captured = v;
        }, async _ =>
        {
            await Task.Delay(1);
            errorCalled = true;
        });

        captured.ShouldBe("ok");
        errorCalled.ShouldBeFalse();
    }

    [Fact]
    public async Task ApplyAsyncWithAsyncHandlersInvokesErrorWhenProvided()
    {

        var ex = new InvalidOperationException("nope");

        Task<Result<bool, Exception>> TaskErr()
        {
            return Task.FromResult<Result<bool, Exception>>(new Result<bool, Exception>.Error(ex));
        }

        Exception? captured = null;
        var successCalled = false;

        await TaskErr().ApplyAsync(async _ =>
        {
            await Task.Delay(1);
            successCalled = true;
        }, async e =>
        {
            await Task.Delay(1);
            captured = e;
        });

        successCalled.ShouldBeFalse();
        captured.ShouldBeSameAs(ex);
    }

    [Fact]
    public async Task ApplyAsyncWithAsyncHandlersDoNothingOnErrorWhenNoOnErrorProvided()
    {

        static Task<Result<int, Exception>> TaskErr()
        {
            return Task.FromResult<Result<int, Exception>>(new Result<int, Exception>.Error(new Exception("err")));
        }

        var successCalled = false;

        await TaskErr().ApplyAsync(async _ =>
        {
            await Task.Delay(1);
            successCalled = true;
        });

        successCalled.ShouldBeFalse();
    }
}

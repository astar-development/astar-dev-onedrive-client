using System.Collections.ObjectModel;

namespace AStar.Dev.Functional.Extensions.Tests.Unit;

public class CollectionAndStatusExtensionsShould
{
    [Fact]
    public async Task ReplaceTheTargetCollectionItemsOnSuccess()
    {
        var target = new ObservableCollection<int> { 1, 2, 3 };
        static Task<Result<IEnumerable<int>, Exception>> ResultTask()
        {
            return Task.FromResult<Result<IEnumerable<int>, Exception>>(new Result<IEnumerable<int>, Exception>.Ok([10, 20]));
        }

        await ResultTask().ApplyToCollectionAsync(target);

        target.ShouldBe([10, 20]);
    }

    [Fact]
    public async Task InvokeOnErrorAndLeaveCollectionUnchangedOnFailure()
    {
        var target = new ObservableCollection<string> { "a", "b" };
        var ex = new InvalidOperationException("boom");

        Task<Result<IEnumerable<string>, Exception>> ResultTask()
        {
            return Task.FromResult<Result<IEnumerable<string>, Exception>>(new Result<IEnumerable<string>, Exception>.Error(ex));
        }

        Exception? captured = null;

        await ResultTask().ApplyToCollectionAsync(target, e => captured = e);

        captured.ShouldBe(ex);
        target.ShouldBe(["a", "b"]);
    }

    [Fact]
    public async Task ClearCollectionWhenSuccessValueIsNull()
    {
        var target = new ObservableCollection<int> { 1, 2, 3 };

        static Task<Result<IEnumerable<int>, Exception>> ResultTask()
        {
            return Task.FromResult<Result<IEnumerable<int>, Exception>>(new Result<IEnumerable<int>, Exception>.Ok(null!));
        }

        await ResultTask().ApplyToCollectionAsync(target);

        target.ShouldBeEmpty();
    }

    [Fact]
    public void MapToStatusStringOnSuccess()
    {
        var result = new Result<int, Exception>.Ok(42);

        var status = result.ToStatus(i => $"Got {i}");

        status.ShouldBe("Got 42");
    }

    [Fact]
    public void MapToStatusStringUsingDefaultErrorMessageOnFailure()
    {
        var err = new Result<int, Exception>.Error(new Exception("Failure happened"));

        var status = err.ToStatus(_ => "Should not be used");

        status.ShouldBe("Failure happened");
    }

    [Fact]
    public void MapToStatusStringUsingCustomErrorFormatterOnFailure()
    {
        var err = new Result<string, Exception>.Error(new InvalidOperationException("nope"));

        var status = err.ToStatus(s => s.ToUpperInvariant(), e => $"ERR: {e.GetType().Name}:{e.Message}");

        status.ShouldBe("ERR: InvalidOperationException:nope");
    }
}

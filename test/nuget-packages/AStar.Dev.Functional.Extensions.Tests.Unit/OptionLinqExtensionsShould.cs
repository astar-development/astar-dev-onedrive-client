namespace AStar.Dev.Functional.Extensions.Tests.Unit;

public class OptionLinqExtensionsShould
{
    [Fact]
    public void ProjectTheValueWithSelectWhenSome()
    {
        Option<int> some = new Option<int>.Some(5);

        Option<int> projected = some.Select(x => x * 2);

        projected.ShouldBeOfType<Option<int>.Some>()
            .Value.ShouldBe(10);
    }

    [Fact]
    public void PreserveNoneWithSelect()
    {
        var none = Option.None<int>();

        Option<int> projected = none.Select(x => x * 2);

        _ = projected.ShouldBeOfType<Option<int>.None>();
    }

    [Fact]
    public void BindAndProjectWithSelectManyWhenAllSome()
    {
        Option<int> some = new Option<int>.Some(3);

        Option<string> result = some.SelectMany(
            x => new Option<string>.Some((x * 2).ToString()),
            (x, y) => $"{x}:{y}");

        result.ShouldBeOfType<Option<string>.Some>()
            .Value.ShouldBe("3:6");
    }

    [Fact]
    public void SelectManyShortCircuitsWhenFirstIsNone()
    {
        var none = Option.None<int>();

        Option<string> result = none.SelectMany(
            x => new Option<string>.Some((x * 2).ToString()),
            (x, y) => $"{x}:{y}");

        _ = result.ShouldBeOfType<Option<string>.None>();
    }

    [Fact]
    public void SelectManyShortCircuitsWhenBindReturnsNone()
    {
        Option<int> some = new Option<int>.Some(7);

        Option<string> result = some.SelectMany(
            _ => Option.None<string>(),
            (x, y) => $"{x}:{y}");

        _ = result.ShouldBeOfType<Option<string>.None>();
    }

    [Fact]
    public async Task SelectAwaitProjectsAsynchronouslyWhenSome()
    {
        Task<Option<int>> task = Task.FromResult<Option<int>>(new Option<int>.Some(4));

        Option<int> projected = await task.SelectAwait(async x =>
        {
            await Task.Delay(1);
            return x * 3;
        });

        projected.ShouldBeOfType<Option<int>.Some>()
            .Value.ShouldBe(12);
    }

    [Fact]
    public async Task SelectAwaitPreservesNone()
    {
        Task<Option<int>> task = Task.FromResult(Option.None<int>());

        Option<int> projected = await task.SelectAwait(async x =>
        {
            await Task.Delay(1);
            return x * 3;
        });

        _ = projected.ShouldBeOfType<Option<int>.None>();
    }
}

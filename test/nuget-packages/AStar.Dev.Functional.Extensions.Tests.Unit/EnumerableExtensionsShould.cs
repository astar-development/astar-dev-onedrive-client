namespace AStar.Dev.Functional.Extensions.Tests.Unit;

public class EnumerableExtensionsShould
{
    [Fact]
    public void FirstOrNoneShouldReturnSomeWhenPredicateMatches()
    {
        var list = new List<string> { "apple", "banana", "cherry" };

        Option<string> result = list.FirstOrNone(s => s.StartsWith('b'));

        _ = result.ShouldBeOfType<Option<string>.Some>();
        var some = result as Option<string>.Some;
        some!.Value.ShouldBe("banana");
    }

    [Fact]
    public void FirstOrNoneShouldReturnNoneWhenNoPredicateMatches()
    {
        var list = new List<int> { 1, 2, 3 };

        Option<int> result = list.FirstOrNone(n => n > 10);

        _ = result.ShouldBeOfType<Option<int>.None>();
    }

    [Fact]
    public void FirstOrNoneShouldReturnNoneForEmptySequence()
    {
        var list = new List<int>();

        Option<int> result = list.FirstOrNone(n => n == 0);

        _ = result.ShouldBeOfType<Option<int>.None>();
    }

    [Fact]
    public void FirstOrNoneShouldReturnFirstMatchingItem()
    {
        var list = new List<int> { 2, 4, 6 };

        Option<int> result = list.FirstOrNone(n => n % 2 == 0);

        Option<int>.Some some = result.ShouldBeOfType<Option<int>.Some>();
        some.Value.ShouldBe(2);
    }
}

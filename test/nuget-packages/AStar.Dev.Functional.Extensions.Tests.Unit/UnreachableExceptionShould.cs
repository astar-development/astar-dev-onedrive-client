namespace AStar.Dev.Functional.Extensions.Tests.Unit;

public class UnreachableExceptionShould
{
    [Fact]
    public void ExtendException()
        => new UnreachableException().ShouldBeAssignableTo<Exception>();
}

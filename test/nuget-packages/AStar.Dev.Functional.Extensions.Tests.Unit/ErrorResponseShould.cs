using AStar.Dev.Utilities;

namespace AStar.Dev.Functional.Extensions.Tests.Unit;

public class ErrorResponseShould
{
    [Fact]
    public void ContainTheExpectedProperties()
        => new ErrorResponse("Test Message").ToJson().ShouldMatchApproved();
}

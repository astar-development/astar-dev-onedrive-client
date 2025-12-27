using AStar.Dev.Logging.Extensions.EventIds;

namespace AStar.Dev.Logging.Extensions.Tests.Unit;

[TestSubject(typeof(AStarEventIds))]
public class AStarEventIdsShould
{
    [Fact]
    public void HaveTheExpectedId()
    {
        const int    expectedId   = 1000;

        EventId eventId = AStarEventIds.Website.PageView;

        eventId.Id.ShouldBe(expectedId);
    }
}

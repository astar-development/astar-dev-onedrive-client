
using AStar.Dev.Logging.Extensions.EventIds;

namespace AStar.Dev.Logging.Extensions;

[TestSubject(typeof(AStarEventIds))]
public class AStarEventIdsShould
{
    [Fact]
    public void PageView_HasExpectedIdAndName()
    {
        const int    expectedId   = 1000;

        EventId eventId = AStarEventIds.Website.PageView;

        Assert.Equal(expectedId, eventId.Id);
    }

    [Fact]
    public void PageView_IsNotNull()
    {
        EventId eventId = AStarEventIds.Website.PageView;

#pragma warning disable xUnit2002
        Assert.NotNull(eventId);
#pragma warning restore xUnit2002
    }
}

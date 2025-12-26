using AStar.Dev.OneDrive.Client.Data;

namespace AStar.Dev.OneDrive.Client.Tests.Unit.Data;

public class ItemIdShould
{
    [Fact]
    public void SetTheExpectedValue()
    {
        var testGuid = Guid.NewGuid();

        var itemId = new ItemId(testGuid);

        itemId.Id.ShouldBe(testGuid);
    }
}

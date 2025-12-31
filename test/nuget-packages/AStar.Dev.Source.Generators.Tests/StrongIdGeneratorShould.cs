using System;

namespace AStar.Dev.Source.Generators.Tests;

public class StrongIdGeneratorShould
{
    [Fact]
    public void CreateStringBasedIdWithValidValue()
    {
        var id = new UserId("user123");

        id.Value.ShouldBe("user123");
        string implicitValue = id;
        implicitValue.ShouldBe("user123");
        id.ToString().ShouldBe("user123");
    }

    [Fact]
    public void RejectNullOrWhitespaceStringId()
    {
        _ = Should.Throw<ArgumentException>(() => new UserId(null!));
        _ = Should.Throw<ArgumentException>(() => new UserId(""));
        _ = Should.Throw<ArgumentException>(() => new UserId("   "));
    }

    [Fact]
    public void CreateIntBasedIdWithValidValue()
    {
        var id = new OrderId(42);

        id.Value.ShouldBe(42);
        int implicitValue = id;
        implicitValue.ShouldBe(42);
        id.ToString().ShouldBe("42");
    }

    [Fact]
    public void RejectZeroOrNegativeIntId()
    {
        _ = Should.Throw<ArgumentException>(() => new OrderId(0));
        _ = Should.Throw<ArgumentException>(() => new OrderId(-1));
        _ = Should.Throw<ArgumentException>(() => new OrderId(-100));
    }

    [Fact]
    public void CreateGuidBasedIdWithValidValue()
    {
        var guid = Guid.NewGuid();
        var id = new EntityId(guid);

        id.Value.ShouldBe(guid);
        Guid implicitValue = id;
        implicitValue.ShouldBe(guid);
        id.ToString().ShouldBe(guid.ToString());
    }

    [Fact]
    public void RejectEmptyGuidId() => Should.Throw<ArgumentException>(() => new EntityId(Guid.Empty));

    [Fact]
    public void CreateDriveIdWithValidString()
    {
        var id = new UserId("drive123");

        id.Value.ShouldBe("drive123");
        string implicitValue = id;
        implicitValue.ShouldBe("drive123");
    }
}

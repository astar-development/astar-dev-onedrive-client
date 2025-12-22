namespace AStar.Dev.OneDrive.Client.Data;

public readonly partial record struct ItemId(Guid Id)
{
    public static ItemId Empty => new(Guid.Empty);
}

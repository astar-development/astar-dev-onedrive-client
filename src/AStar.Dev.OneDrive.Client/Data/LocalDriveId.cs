namespace AStar.Dev.OneDrive.Client.Data;

public readonly partial record struct LocalDriveId(Guid Id)
{
    public static LocalDriveId Empty => new(Guid.Empty);
}

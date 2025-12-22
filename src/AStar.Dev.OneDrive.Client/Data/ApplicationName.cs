namespace AStar.Dev.OneDrive.Client.Data;

public readonly partial record struct ApplicationName(string Name)
{
    public static ApplicationName Empty => new(string.Empty);
}

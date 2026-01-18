namespace AStar.Dev.OneDrive.Client.Core.Entities;

/// <summary>
/// Entity for storing window position and size preferences in the database.
/// </summary>
public sealed class WindowPreferencesEntity
{
    public int Id { get; set; }
    public double? X { get; set; }
    public double? Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public bool IsMaximized { get; set; }
}

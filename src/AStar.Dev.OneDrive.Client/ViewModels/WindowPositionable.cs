using Avalonia;

namespace AStar.Dev.OneDrive.Client.ViewModels;

public class WindowPositionable(double width, double height, PixelPoint position) : IWindowPositionable
{
    public double Width { get => width; set => width = value; }
    public double Height { get => height; set => height = value; }
    public PixelPoint Position { get => position; set => position = value; }
}

namespace SeaWolfBlazorVersion.Engine.Models;

public class Torpedo
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Speed { get; init; } = 6f;
    public bool Active { get; set; } = true;
    public int Width { get; } = 8;
    public int Height { get; } = 20;
}

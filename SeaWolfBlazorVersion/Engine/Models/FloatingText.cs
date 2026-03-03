namespace SeaWolfBlazorVersion.Engine.Models;

public class FloatingText
{
    public float X { get; set; }
    public float Y { get; set; }
    public string Text { get; set; } = "";
    public string Color { get; set; } = "#FFD700";
    public float Life { get; set; }
    public float MaxLife { get; set; }
    public bool Active { get; set; } = true;
}

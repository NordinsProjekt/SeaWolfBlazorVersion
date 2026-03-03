namespace SeaWolfBlazorVersion.Engine.Models;

public class Explosion
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Radius { get; set; } = 5f;
    public float MaxRadius { get; init; } = 50f;
    public float Opacity { get; set; } = 1f;
    public bool Active { get; set; } = true;
    public List<ExplosionSpark> Sparks { get; set; } = new();
}

public class ExplosionSpark
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Vx { get; set; }
    public float Vy { get; set; }
    public float Life { get; set; } = 1f;
}

using SeaWolfBlazorVersion.Engine.Models.Enums;

namespace SeaWolfBlazorVersion.Engine.Models;

public class Ship
{
    public Guid Id { get; } = Guid.NewGuid();
    public ShipType Type { get; init; }
    public float X { get; set; }
    public float Y { get; set; }
    public float BaseSpeed { get; init; }
    public float CurrentSpeed => DamageState == ShipDamageState.Burning
        ? BaseSpeed * 0.5f
        : BaseSpeed;
    public int Width { get; init; }
    public int Height { get; init; }
    public int BasePoints { get; init; }
    public ShipDamageState DamageState { get; set; } = ShipDamageState.Healthy;
    public bool Active { get; set; } = true;

    // Sinking animation state
    public float SinkTimer { get; set; }
    public const float SinkDuration = 1.5f;

    // Fire particles — populated by GameEngine when Burning is set
    public List<FireParticle> FireParticles { get; set; } = new();

    // Cargo requires two hits; destroyers and PT boats go down in one
    public bool RequiresTwoHits { get; init; }

    // 1 = spawns left, moves right  |  -1 = spawns right, moves left
    public int Direction { get; init; } = 1;

    // Returns points for this hit (partial or kill)
    public int HitPoints => (int)(BasePoints * 0.30f);
    public int KillPoints => (int)(BasePoints * 0.70f);

    public bool CheckCollision(float torpX, float torpY)
    {
        return MathF.Abs(torpX - X) < Width / 2f
            && MathF.Abs(torpY - Y) < Height / 2f;
    }

    public static Ship Create(ShipType type, float difficultyMultiplier, int direction = 1)
    {
        float startX(int width) => direction == 1 ? -width : 900 + width;
        float y = 295 + Random.Shared.NextSingle() * 110;

        return type switch
        {
            ShipType.Destroyer => new Ship
            {
                Type = type, X = startX(130), Y = y,
                BaseSpeed = 1.5f * difficultyMultiplier,
                Width = 130, Height = 45, BasePoints = 100,
                Direction = direction
            },
            ShipType.PtBoat => new Ship
            {
                Type = type, X = startX(90), Y = y,
                BaseSpeed = 3.0f * difficultyMultiplier,
                Width = 90, Height = 30, BasePoints = 200,
                Direction = direction
            },
            ShipType.Cargo => new Ship
            {
                Type = type, X = startX(160), Y = y,
                BaseSpeed = 1.0f * difficultyMultiplier,
                Width = 160, Height = 55, BasePoints = 150,
                RequiresTwoHits = true,
                Direction = direction
            },
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };
    }
}

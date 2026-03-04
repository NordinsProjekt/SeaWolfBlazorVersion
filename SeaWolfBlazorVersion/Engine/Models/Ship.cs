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
    // 1.0 = near lane (default); 0.55 = far lane (small, near horizon, harder to hit)
    public float DepthScale { get; init; } = 1.0f;

    // Returns points for this hit (partial or kill)
    public int HitPoints => (int)(BasePoints * 0.30f);
    public int KillPoints => (int)(BasePoints * 0.70f);

    public bool CheckCollision(float torpX, float torpY)
    {
        return MathF.Abs(torpX - X) < Width / 2f
            && MathF.Abs(torpY - Y) < Height / 2f;
    }

    public static Ship Create(ShipType type, float difficultyMultiplier, int direction = 1, bool farLane = false)
    {
        float depthScale = farLane ? 0.55f : 1.0f;
        float pointsScale = farLane ? 1.8f : 1.0f;
        float startX(int width) => direction == 1 ? -width : 1380 + width;
        float y = farLane
            ? 335f + Random.Shared.NextSingle() * 30f
            : 354f + Random.Shared.NextSingle() * 132f;

        return type switch
        {
            ShipType.Destroyer => new Ship
            {
                Type = type, X = startX((int)(130 * depthScale)), Y = y,
                BaseSpeed = 1.5f * difficultyMultiplier,
                Width = (int)(130 * depthScale), Height = (int)(45 * depthScale),
                BasePoints = (int)(100 * pointsScale),
                DepthScale = depthScale, Direction = direction
            },
            ShipType.PtBoat => new Ship
            {
                Type = type, X = startX((int)(90 * depthScale)), Y = y,
                BaseSpeed = 3.0f * difficultyMultiplier,
                Width = (int)(90 * depthScale), Height = (int)(30 * depthScale),
                BasePoints = (int)(200 * pointsScale),
                DepthScale = depthScale, Direction = direction
            },
            ShipType.Cargo => new Ship
            {
                Type = type, X = startX((int)(160 * depthScale)), Y = y,
                BaseSpeed = 1.0f * difficultyMultiplier,
                Width = (int)(160 * depthScale), Height = (int)(55 * depthScale),
                BasePoints = (int)(150 * pointsScale),
                RequiresTwoHits = true,
                DepthScale = depthScale, Direction = direction
            },
            ShipType.Cruiser => new Ship
            {
                Type = type, X = startX((int)(155 * depthScale)), Y = y,
                BaseSpeed = 1.2f * difficultyMultiplier,
                Width = (int)(155 * depthScale), Height = (int)(52 * depthScale),
                BasePoints = (int)(300 * pointsScale),
                RequiresTwoHits = true,
                DepthScale = depthScale, Direction = direction
            },
            ShipType.FishingBoat => new Ship
            {
                Type = type, X = startX((int)(95 * depthScale)), Y = y,
                BaseSpeed = 0.7f * difficultyMultiplier,
                Width = (int)(95 * depthScale), Height = (int)(32 * depthScale),
                BasePoints = (int)(75 * pointsScale),
                DepthScale = depthScale, Direction = direction
            },
            ShipType.Tanker => new Ship
            {
                Type = type, X = startX((int)(185 * depthScale)), Y = y,
                BaseSpeed = 0.6f * difficultyMultiplier,
                Width = (int)(185 * depthScale), Height = (int)(60 * depthScale),
                BasePoints = (int)(400 * pointsScale),
                RequiresTwoHits = true,
                DepthScale = depthScale, Direction = direction
            },
            ShipType.Carrier => new Ship
            {
                Type = type, X = startX((int)(220 * depthScale)), Y = y,
                BaseSpeed = 0.45f * difficultyMultiplier,
                Width = (int)(220 * depthScale), Height = (int)(68 * depthScale),
                BasePoints = (int)(700 * pointsScale),
                RequiresTwoHits = true,
                DepthScale = depthScale, Direction = direction
            },
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };
    }
}

using GameEngine.Engine.Models.Enums;

namespace GameEngine.Engine.Models;

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
        // Horizontal: torpedo must be within the ship's width
        if (MathF.Abs(torpX - X) >= Width / 2f) return false;

        // Vertical: torpedo must strike the bottom (waterline) strip only.
        // Bottom edge is Y + Height/2; the hit band is the lower ~30 % of the sprite,
        // minimum 10 px, so small far-lane ships remain hittable.
        float bottomEdge = Y + Height / 2f;
        float hitBand    = MathF.Max(10f, Height * 0.30f);

        return torpY >= bottomEdge - hitBand   // entered the bottom zone from below
            && torpY <= bottomEdge + 6f;       // small tolerance for per-frame travel speed
    }

    private readonly record struct ShipTemplate(
        int Width, int Height, float Speed, int Points, bool RequiresTwoHits);

    private static readonly Dictionary<ShipType, ShipTemplate> Templates = new()
    {
        [ShipType.Destroyer]   = new(130, 45, 1.5f,  100, false),
        [ShipType.PtBoat]      = new(90,  30, 3.0f,  200, false),
        [ShipType.Cargo]       = new(160, 55, 1.0f,  150, true),
        [ShipType.Cruiser]     = new(155, 52, 1.2f,  300, true),
        [ShipType.FishingBoat] = new(95,  32, 0.7f,  75,  false),
        [ShipType.Tanker]      = new(185, 60, 0.6f,  400, true),
        [ShipType.Carrier]     = new(220, 68, 0.45f, 700, true),
    };

    public static Ship Create(ShipType type, float difficultyMultiplier, int direction = 1, bool farLane = false)
    {
        if (!Templates.TryGetValue(type, out var tmpl))
            throw new ArgumentOutOfRangeException(nameof(type));

        float depthScale  = farLane ? 0.55f : 1.0f;
        float pointsScale = farLane ? 1.8f  : 1.0f;
        float y      = farLane ? 335f + Random.Shared.NextSingle() * 30f
                                : 354f + Random.Shared.NextSingle() * 132f;
        int   w      = (int)(tmpl.Width  * depthScale);
        int   h      = (int)(tmpl.Height * depthScale);
        float startX = direction == 1 ? -w : 1380f + w;

        return new Ship
        {
            Type            = type,
            X               = startX,
            Y               = y,
            BaseSpeed       = tmpl.Speed * difficultyMultiplier,
            Width           = w,
            Height          = h,
            BasePoints      = (int)(tmpl.Points * pointsScale),
            RequiresTwoHits = tmpl.RequiresTwoHits,
            DepthScale      = depthScale,
            Direction       = direction
        };
    }
}

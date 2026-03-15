using GameEngine.Engine.Models.Enums;

namespace GameEngine.Engine.Models;

public class Ship
{
    // ── Spatial constants ─────────────────────────────────────────────────────
    /// <summary>Canvas width used to compute the off-screen spawn X for rightward ships.</summary>
    private const float CanvasWidth = 1380f;

    /// <summary>Y-range for the far (horizon) lane: base Y and random spread.</summary>
    private const float FarLaneBaseY   = 335f;
    private const float FarLaneSpread  = 30f;

    /// <summary>Y-range for the near lane: base Y and random spread.</summary>
    private const float NearLaneBaseY  = 354f;
    private const float NearLaneSpread = 132f;

    // ── Collision constants ───────────────────────────────────────────────────
    /// <summary>Fraction of sprite height that counts as the hittable waterline band.</summary>
    private const float HitBandFraction  = 0.30f;

    /// <summary>Minimum hittable band height in pixels (keeps far-lane ships shootable).</summary>
    private const float HitBandMinPixels = 10f;

    /// <summary>Downward tolerance added to the bottom edge to absorb per-frame travel.</summary>
    private const float HitBandTolerance = 6f;

    // ── Properties ───────────────────────────────────────────────────────────
    public Guid Id { get; } = Guid.NewGuid();
    public ShipType Type { get; init; }
    public float X { get; set; }
    public float Y { get; set; }
    public float BaseSpeed { get; init; }
    public float CurrentSpeed => DamageState == ShipDamageState.Burning
        ? BaseSpeed * 0.5f
        : BaseSpeed;
    public int Width  { get; init; }
    public int Height { get; init; }
    public int BasePoints { get; init; }
    public ShipDamageState DamageState { get; set; } = ShipDamageState.Healthy;
    public bool Active { get; set; } = true;

    // Sinking animation state
    public float SinkTimer { get; set; }
    public const float SinkDuration = 1.5f;

    // Fire particles — populated by GameEngine when Burning is set
    public List<FireParticle> FireParticles { get; } = new();

    // Cargo/Cruiser/Tanker/Carrier require two hits; smaller ships go down in one
    public bool RequiresTwoHits { get; init; }

    // 1 = spawns left, moves right  |  -1 = spawns right, moves left
    public int Direction { get; init; } = 1;
    // 1.0 = near lane (default); 0.55 = far lane (small, near horizon, harder to hit)
    public float DepthScale { get; init; } = 1.0f;

    /// <summary>Points awarded on the first (burning) hit of a two-hit ship.</summary>
    public int HitPoints  => (int)(BasePoints * 0.30f);

    /// <summary>Points awarded on the killing blow (guaranteed to sum to BasePoints).</summary>
    public int KillPoints => BasePoints - HitPoints;

    // ── Collision ─────────────────────────────────────────────────────────────
    public bool CheckCollision(float torpX, float torpY)
    {
        if (MathF.Abs(torpX - X) >= Width / 2f) return false;

        float bottomEdge = Y + Height / 2f;
        float hitBand    = MathF.Max(HitBandMinPixels, Height * HitBandFraction);

        return torpY >= bottomEdge - hitBand
            && torpY <= bottomEdge + HitBandTolerance;
    }

    // ── Factory ───────────────────────────────────────────────────────────────
    private readonly record struct ShipTemplate(
        int   Width,
        int   Height,
        float Speed,
        int   Points,
        bool  RequiresTwoHits);

    private static readonly Dictionary<ShipType, ShipTemplate> _templates = new()
    {
        [ShipType.Destroyer]   = new(Width: 130, Height: 45, Speed: 67.5f,  Points: 100, RequiresTwoHits: false),
        [ShipType.PtBoat]      = new(Width: 90,  Height: 30, Speed: 135f,   Points: 200, RequiresTwoHits: false),
        [ShipType.Cargo]       = new(Width: 160, Height: 55, Speed: 45f,    Points: 150, RequiresTwoHits: true),
        [ShipType.Cruiser]     = new(Width: 155, Height: 52, Speed: 54f,    Points: 300, RequiresTwoHits: true),
        [ShipType.FishingBoat] = new(Width: 95,  Height: 32, Speed: 31.5f,  Points: 75,  RequiresTwoHits: false),
        [ShipType.Tanker]      = new(Width: 185, Height: 60, Speed: 27f,    Points: 400, RequiresTwoHits: true),
        [ShipType.Carrier]     = new(Width: 220, Height: 68, Speed: 20.25f, Points: 700, RequiresTwoHits: true),
    };

    public static Ship Create(ShipType type, float difficultyMultiplier, int direction = 1, bool farLane = false)
    {
        if (!_templates.TryGetValue(type, out var tmpl))
            throw new ArgumentOutOfRangeException(nameof(type));

        float depthScale  = farLane ? 0.55f : 1.0f;
        float pointsScale = farLane ? 1.8f  : 1.0f;
        float y      = farLane
            ? FarLaneBaseY  + Random.Shared.NextSingle() * FarLaneSpread
            : NearLaneBaseY + Random.Shared.NextSingle() * NearLaneSpread;
        int   w      = (int)(tmpl.Width  * depthScale);
        int   h      = (int)(tmpl.Height * depthScale);
        float startX = direction == 1 ? -w : CanvasWidth + w;

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

using SeaWolfBlazorVersion.Engine.Models;
using SeaWolfBlazorVersion.Engine.Models.Enums;

namespace SeaWolfBlazorVersion.Engine;

public class GameState
{
    public GameStatus Status { get; set; } = GameStatus.StartScreen;
    public int Score { get; set; }
    public int HighScore { get; set; }
    public int Wave { get; set; } = 1;

    // Torpedo management
    public int TorpedoCount { get; set; } = 5;
    public const int MaxTorpedoes = 5;
    public bool IsReloading { get; set; }
    public float ReloadTimer { get; set; }
    public const float ReloadDuration = 2f;

    // Wave management
    public int ShipsSpawnedThisWave { get; set; }
    public int ShipsSunkThisWave { get; set; }
    public int ShipsEscaped { get; set; }
    public const int MaxEscaped = 5;           // game over threshold
    public float WaveClearTimer { get; set; }
    public const float WaveClearPause = 3f;

    // Entity lists
    public List<Ship> Ships { get; } = new();
    public List<Torpedo> Torpedoes { get; } = new();
    public List<Explosion> Explosions { get; } = new();

    // Spawn timer
    public float SpawnTimer { get; set; }

    // Mouse position (canvas coordinates)
    public float MouseX { get; set; } = 400;
    public float MouseY { get; set; } = 300;

    // Wave clear bonus display
    public float WaveBonusDisplayTimer { get; set; }
    public int LastWaveBonus { get; set; }

    // Combo multiplier — resets after 2.5 s without a hit or when a ship escapes
    public int ComboCount { get; set; }
    public float ComboTimer { get; set; }
    public const float ComboTimeout = 2.5f;

    // Per-wave accuracy tracking
    public int TorpedosFired { get; set; }
    public int TorpedosHit { get; set; }
    public int LastAccuracy { get; set; }   // 0-100
    public int AccuracyBonus { get; set; }

    // Screen shake (seconds remaining)
    public float ShakeTimer { get; set; }

    // Floating score pop-ups
    public List<FloatingText> FloatingTexts { get; } = new();
}

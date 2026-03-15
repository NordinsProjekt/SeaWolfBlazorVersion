using GameEngine.Engine.Models;
using GameEngine.Engine.Models.Enums;

namespace GameEngine.Engine;

public class GameState
{
    public GameStatus Status { get; set; } = GameStatus.StartScreen;
    public GameMode   Mode   { get; set; } = GameMode.Arcade;
    public int Score { get; set; }
    public int HighScore { get; set; }
    public int Wave { get; set; } = 1;

    // Torpedo management
    public int TorpedoCount { get; set; } = 2;
    public const int MaxTorpedoes = 2;
    public bool IsReloading { get; set; }
    public float ReloadTimer { get; set; }
    public const float ReloadDuration = 4f;

    // Wave management
    public int ShipsSpawnedThisWave { get; set; }
    public int ShipsSunkThisWave { get; set; }
    public int ShipsEscaped { get; set; }
    public const int MaxEscaped = 5;           // game over threshold (arcade)
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

    // Torpedo tube aim
    public float AimX { get; set; } = 640f;
    public int SelectedTube { get; set; } = 2; // 0 = far-left … 4 = far-right

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

    // Per-game totals (survive across wave resets, cleared only on StartGame)
    public int TotalTorpedosFired { get; set; }
    public int TotalTorpedosHit { get; set; }
    public int TotalShipsSunk { get; set; }

    // Screen shake (seconds remaining)
    public float ShakeTimer { get; set; }

    // Wave start banner — seconds elapsed since the current wave entered Playing state
    public float WaveStartTimer { get; set; }

    // Total ships in the current wave (set by GameEngine, serialised for JS HUD)
    public int WaveTotalShips { get; set; }

    // Serialisable alias for the reload-duration constant (const fields don't serialise)
    public float ReloadDurationValue => ReloadDuration;

    // Top-10 highscore list — populated by the Blazor host, read by JS renderer
    public IReadOnlyList<HighScoreEntryDto> HighScoreList { get; set; } = [];

    // Floating score pop-ups
    public List<FloatingText> FloatingTexts { get; } = new();

    // ── Campaign state ────────────────────────────────────────────────────────

    public int  CampaignMission    { get; set; } = 1;
    public int  CampaignLives      { get; set; } = 3;
    public int  CampaignSinks      { get; set; }   // objective-type sinks this mission
    public int  CivilianSinks      { get; set; }   // FishingBoat kills this mission
    public int  TorpedoBudgetLeft  { get; set; }   // 0 = unlimited
    public bool CampaignMissionFailed { get; set; }
    public float MissionScreenTimer { get; set; }  // seconds on briefing/complete screen

    // Per-type sink counts for objective tracking (serialised as a flat int[])
    // Index order matches ShipType enum values (0–6)
    public int[] SinksByType { get; set; } = new int[7];

    public Dictionary<ShipType, int> SinksByTypeDict()
    {
        var d = new Dictionary<ShipType, int>();
        for (int i = 0; i < SinksByType.Length; i++)
            if (SinksByType[i] > 0)
                d[(ShipType)i] = SinksByType[i];
        return d;
    }

    /// <summary>
    /// Serialised mission metadata for the JS renderer to display briefings.
    /// </summary>
    public IReadOnlyList<CampaignMissionInfo> CampaignMissions =>
        CampaignManager.Missions.Select(m => new CampaignMissionInfo(
            m.CodeName,
            m.Briefing,
            m.Objective.RequiredSinks,
            m.Objective.TargetTypes.Select(t => t.ToString()).ToArray(),
            m.Objective.MaxEscaped,
            m.Objective.TorpedoBudget,
            m.Objective.MaxCivilianSinks,
            m.ShipsPerWave
        )).ToList();
}

/// <summary>Flat DTO for JS serialisation of a mission config.</summary>
public record CampaignMissionInfo(
    string CodeName,
    string Briefing,
    int    RequiredSinks,
    string[] TargetTypes,
    int    MaxEscaped,
    int    TorpedoBudget,
    int    MaxCivilianSinks,
    int    ShipsPerWave
);

/// <summary>Flat DTO for a single highscore entry serialised to JS.</summary>
public record HighScoreEntryDto(string Name, int Score, string Mode);

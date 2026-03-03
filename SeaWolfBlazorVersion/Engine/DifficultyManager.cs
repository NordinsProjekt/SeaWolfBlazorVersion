using SeaWolfBlazorVersion.Engine.Models.Enums;

namespace SeaWolfBlazorVersion.Engine;

public record WaveConfig(
    int WaveNumber,
    int TotalShips,
    float SpeedMultiplier,
    float SpawnIntervalSeconds,
    int ExtraPtBoatWeight,
    int WaveBonusPoints
);

public static class DifficultyManager
{
    private static readonly List<WaveConfig> _waves = new()
    {
        new(1,  6,  1.0f, 3.0f, 0,  500),
        new(2,  8,  1.1f, 2.7f, 1,  600),
        new(3,  10, 1.2f, 2.4f, 2,  700),
        new(4,  12, 1.3f, 2.1f, 3,  900),
        new(5,  14, 1.5f, 1.8f, 4,  1100),
        new(6,  15, 1.7f, 1.6f, 5,  1300),
        new(7,  18, 2.0f, 1.4f, 6,  1600),
        new(8,  20, 2.2f, 1.2f, 7,  2000),
        new(9,  22, 2.5f, 1.0f, 8,  2500),
        new(10, 25, 3.0f, 0.8f, 9,  3200),
    };

    public static WaveConfig GetWave(int waveNumber)
    {
        if (waveNumber <= _waves.Count)
            return _waves[waveNumber - 1];

        var extra = waveNumber - 10;
        return new WaveConfig(
            waveNumber,
            TotalShips:           25 + extra * 3,
            SpeedMultiplier:      3.0f + extra * 0.15f,
            SpawnIntervalSeconds: MathF.Max(0.5f, 0.8f - extra * 0.05f),
            ExtraPtBoatWeight:    10,
            WaveBonusPoints:      3200 + extra * 500
        );
    }

    public static ShipType PickShipType(WaveConfig wave)
    {
        var pool = new List<ShipType>();
        pool.AddRange(Enumerable.Repeat(ShipType.Destroyer, 5));
        pool.AddRange(Enumerable.Repeat(ShipType.Cargo, 3));
        pool.AddRange(Enumerable.Repeat(ShipType.PtBoat, 2 + wave.ExtraPtBoatWeight));
        // Cruisers start appearing at wave 3; frequency caps at 4 per pool
        if (wave.WaveNumber >= 3)
            pool.AddRange(Enumerable.Repeat(ShipType.Cruiser, Math.Min(wave.WaveNumber - 2, 4)));
        // Fishing boats: easy targets common in early waves, gone by wave 6
        if (wave.WaveNumber <= 5)
            pool.AddRange(Enumerable.Repeat(ShipType.FishingBoat, Math.Max(1, 5 - wave.WaveNumber)));
        // Tankers: rare high-value targets from wave 2, capped at 3 per pool
        if (wave.WaveNumber >= 2)
            pool.AddRange(Enumerable.Repeat(ShipType.Tanker, Math.Min(wave.WaveNumber / 2, 3)));
        // Carriers: very rare, appear from wave 5, capped at 2 per pool
        if (wave.WaveNumber >= 5)
            pool.AddRange(Enumerable.Repeat(ShipType.Carrier, Math.Min((wave.WaveNumber - 4) / 2, 2)));
        return pool[Random.Shared.Next(pool.Count)];
    }
}

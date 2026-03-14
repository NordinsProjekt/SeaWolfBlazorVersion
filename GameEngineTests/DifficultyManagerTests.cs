using GameEngine.Engine;
using GameEngine.Engine.Models.Enums;

namespace GameEngineTests;

public class DifficultyManagerTests
{
    // ── GetWave ──────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(1,  6,  1.0f, 3.0f,  500)]
    [InlineData(5,  14, 1.5f, 1.8f, 1100)]
    [InlineData(10, 25, 3.0f, 0.8f, 3200)]
    public void GetWave_KnownWaves_ReturnsCorrectConfig(
        int wave, int ships, float speed, float interval, int bonus)
    {
        var cfg = DifficultyManager.GetWave(wave);

        Assert.Equal(wave,     cfg.WaveNumber);
        Assert.Equal(ships,    cfg.TotalShips);
        Assert.Equal(speed,    cfg.SpeedMultiplier);
        Assert.Equal(interval, cfg.SpawnIntervalSeconds);
        Assert.Equal(bonus,    cfg.WaveBonusPoints);
    }

    [Fact]
    public void GetWave_Wave11_ScalesBeyondTable()
    {
        var cfg = DifficultyManager.GetWave(11);

        Assert.Equal(11, cfg.WaveNumber);
        Assert.Equal(28, cfg.TotalShips);          // 25 + 1*3
        Assert.Equal(3.15f, cfg.SpeedMultiplier);  // 3.0 + 1*0.15
        Assert.Equal(3700,  cfg.WaveBonusPoints);  // 3200 + 1*500
        Assert.True(cfg.SpawnIntervalSeconds > 0);
    }

    [Fact]
    public void GetWave_SpawnIntervalFloorAt0_5_ForHighWaves()
    {
        // At wave ~26 (extra=16) the raw formula gives 0.8 - 16*0.05 = 0.0;
        // the MathF.Max(0.5f, …) floor must apply.
        var cfg = DifficultyManager.GetWave(26);
        Assert.True(cfg.SpawnIntervalSeconds >= 0.5f);
    }

    // ── PickShipType ─────────────────────────────────────────────────────────

    [Fact]
    public void PickShipType_Wave1_NeverReturnsCruiserOrCarrier()
    {
        var wave = DifficultyManager.GetWave(1);
        for (int i = 0; i < 500; i++)
        {
            var t = DifficultyManager.PickShipType(wave);
            Assert.NotEqual(ShipType.Cruiser, t);
            Assert.NotEqual(ShipType.Carrier, t);
        }
    }

    [Fact]
    public void PickShipType_Wave1_NeverReturnsTanker()
    {
        var wave = DifficultyManager.GetWave(1);
        for (int i = 0; i < 500; i++)
            Assert.NotEqual(ShipType.Tanker, DifficultyManager.PickShipType(wave));
    }

    [Fact]
    public void PickShipType_Wave6_NoFishingBoats()
    {
        // FishingBoat only appears on waves <= 5
        var wave = DifficultyManager.GetWave(6);
        for (int i = 0; i < 500; i++)
            Assert.NotEqual(ShipType.FishingBoat, DifficultyManager.PickShipType(wave));
    }

    [Fact]
    public void PickShipType_Wave6_CanReturnCarrier()
    {
        // Integer division: (waveNumber-4)/2 first yields 1 at wave 6, not wave 5
        var wave = DifficultyManager.GetWave(6);
        bool seenCarrier = false;
        for (int i = 0; i < 2000 && !seenCarrier; i++)
            seenCarrier = DifficultyManager.PickShipType(wave) == ShipType.Carrier;
        Assert.True(seenCarrier);
    }

    [Fact]
    public void PickShipType_Wave3_CanReturnCruiser()
    {
        var wave = DifficultyManager.GetWave(3);
        bool seen = false;
        for (int i = 0; i < 2000 && !seen; i++)
            seen = DifficultyManager.PickShipType(wave) == ShipType.Cruiser;
        Assert.True(seen);
    }
}

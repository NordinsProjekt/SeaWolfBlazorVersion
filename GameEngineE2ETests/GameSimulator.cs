using GameEngine.Engine;
using GameEngine.Engine.Models;
using GameEngine.Engine.Models.Enums;

namespace GameEngineE2ETests;

/// <summary>
/// Drives a GameEngine instance without a UI.
/// Provides helpers to fast-forward time, inject kills, and assert game-flow states.
/// </summary>
public sealed class GameSimulator
{
    public GameEngine.Engine.GameEngine Engine { get; } = new();
    public GameState State => Engine.State;

    public const float FrameDtPublic = FrameDt;

    private const float FrameDt = 0.016f;   // 60 fps equivalent
    private const int   MaxFrames = 200_000; // safety cap (~55 min of simulated time)

    // ── Time helpers ─────────────────────────────────────────────────────────

    /// <summary>Runs Update in FrameDt steps until <paramref name="predicate"/> is true or the frame cap is hit.</summary>
    public int RunUntil(Func<bool> predicate, int maxFrames = MaxFrames)
    {
        for (int i = 0; i < maxFrames; i++)
        {
            if (predicate()) return i;
            Engine.Update(FrameDt);
        }
        if (predicate()) return maxFrames;
        throw new TimeoutException(
            $"Condition never met after {maxFrames} frames " +
            $"(~{maxFrames * FrameDt:F0}s simulated). " +
            $"Status={State.Status}, Wave={State.Wave}");
    }

    /// <summary>Runs for exactly <paramref name="seconds"/> of simulated time.</summary>
    public void RunFor(float seconds)
    {
        int frames = (int)MathF.Ceiling(seconds / FrameDt);
        for (int i = 0; i < frames; i++)
            Engine.Update(FrameDt);
    }

    // ── Game-flow shortcuts ───────────────────────────────────────────────────

    /// <summary>Starts an arcade game and waits until the first Playing frame.</summary>
    public void StartArcade()
    {
        Engine.StartGame();
        Assert.Equal(GameStatus.Playing, State.Status);
    }

    /// <summary>Starts campaign and immediately advances through the briefing.</summary>
    public void StartCampaignPlaying()
    {
        Engine.StartCampaign();
        Engine.AdvanceMissionBriefing();
        Assert.Equal(GameStatus.Playing, State.Status);
    }

    /// <summary>
    /// Advances through a MissionBriefing screen when the engine reaches one.
    /// Waits up to 1 second of simulated time for the status to arrive.
    /// </summary>
    public void WaitAndAdvanceBriefing()
    {
        RunUntil(() => State.Status == GameStatus.MissionBriefing, maxFrames: 4000);
        Engine.AdvanceMissionBriefing();
    }

    // ── Kill injection ───────────────────────────────────────────────────────

    /// <summary>
    /// Sinks <paramref name="count"/> ships of the given type by directly updating
    /// engine state (bypasses the renderer/collision path).
    /// Also increments TotalShipsSunk and SinksByType so objectives are tracked.
    /// </summary>
    public void InjectSinks(ShipType type, int count)
    {
        int typeIdx = (int)type;
        for (int i = 0; i < count; i++)
        {
            State.SinksByType[typeIdx]++;
            State.TotalShipsSunk++;
            State.ShipsSunkThisWave++;
            State.Score += 100;
        }
    }

    /// <summary>Injects <paramref name="count"/> sinks of any type (uses Destroyer).</summary>
    public void InjectSinks(int count) => InjectSinks(ShipType.Destroyer, count);

    /// <summary>
    /// Injects exactly enough sinks to meet the current mission's objective.
    /// Uses the first TargetType if the mission has specific targets, otherwise Destroyer.
    /// </summary>
    public void InjectObjectiveSinks()
    {
        var mission = CampaignManager.GetMission(State.CampaignMission);
        var type    = mission.Objective.TargetTypes.Count > 0
            ? mission.Objective.TargetTypes[0]
            : ShipType.Destroyer;
        int needed  = mission.Objective.RequiredSinks - State.CampaignSinks;
        if (needed > 0)
            InjectSinks(type, needed);
    }

    /// <summary>
    /// Forces <paramref name="count"/> ships to escape (used to test failure
    /// paths). Mirrors GameEngine.HandleEscape: in Campaign mode, a life is
    /// only lost if the escaped type counts toward the current mission's
    /// objective. Defaults to the mission's own first target type so
    /// existing "escape costs a life" tests don't need to know mission
    /// internals; pass an explicit non-objective <paramref name="type"/> to
    /// test that those escapes are free.
    /// </summary>
    public void InjectEscapes(int count, ShipType? type = null)
    {
        var mission = CampaignManager.GetMission(State.CampaignMission);
        var effectiveType = type ?? (mission.Objective.TargetTypes.Count > 0
            ? mission.Objective.TargetTypes[0]
            : ShipType.Destroyer);
        bool costsLife = State.Mode == GameMode.Campaign
            && CampaignManager.IsObjectiveType(mission, effectiveType);

        for (int i = 0; i < count; i++)
        {
            State.ShipsEscaped++;
            State.ComboCount = 0;
            if (costsLife)
                State.CampaignLives--;
        }
    }

    /// <summary>Sinks a civilian (FishingBoat) — used to test civilian-penalty rules.</summary>
    public void InjectCivilianSink()
    {
        State.CivilianSinks++;
        State.SinksByType[(int)ShipType.FishingBoat]++;
        State.TotalShipsSunk++;
    }

    // ── Wave helpers ─────────────────────────────────────────────────────────

    /// <summary>
    /// Clears all ships currently on screen and marks the wave as fully
    /// spawned, then runs one Update. In Arcade mode this is what makes
    /// CheckWaveClear fire (→ WaveClear). In Campaign mode there's no
    /// per-wave batch to exhaust any more — this just clears the board, so
    /// if the mission objective is already satisfied it completes
    /// immediately, and otherwise play just continues (more ships keep
    /// spawning).
    /// </summary>
    public void ForceWaveClear()
    {
        State.ShipsSpawnedThisWave = State.WaveTotalShips;
        State.Ships.Clear();
        Engine.Update(FrameDt);
    }

    /// <summary>
    /// Waits for WaveClear status then skips the pause timer and returns once
    /// the engine transitions away from WaveClear. Arcade-only — campaign
    /// missions never enter WaveClear (see GameEngine.UpdateCampaignObjective).
    /// </summary>
    public void SkipWaveClear()
    {
        RunUntil(() => State.Status == GameStatus.WaveClear);
        RunFor(GameState.WaveClearPause + 0.1f);
    }

    // ── Assertion helpers ────────────────────────────────────────────────────

    public void AssertStatus(GameStatus expected) =>
        Assert.Equal(expected, State.Status);

    public void AssertMission(int expected) =>
        Assert.Equal(expected, State.CampaignMission);

    public void AssertWave(int expected) =>
        Assert.Equal(expected, State.Wave);
}

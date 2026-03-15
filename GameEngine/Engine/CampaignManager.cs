using GameEngine.Engine.Models.Enums;

namespace GameEngine.Engine;

/// <summary>
/// Defines a single campaign mission.
/// </summary>
public record MissionConfig(
    int MissionNumber,
    string CodeName,
    string Briefing,
    int StartWave,
    int EndWave,
    int Lives,
    MissionObjective Objective,
    /// <summary>Ships spawned per wave in this mission (overrides DifficultyManager.TotalShips).</summary>
    int ShipsPerWave
);

/// <summary>
/// What the player must achieve to complete the mission.
/// </summary>
public record MissionObjective(
    /// <summary>Minimum ships that must be sunk to pass.</summary>
    int RequiredSinks,
    /// <summary>Specific ship types that count toward the objective (empty = all types count).</summary>
    IReadOnlyList<ShipType> TargetTypes,
    /// <summary>Maximum ships allowed to escape before mission fails (overrides MaxEscaped).</summary>
    int MaxEscaped,
    /// <summary>Optional torpedo budget (0 = unlimited).</summary>
    int TorpedoBudget,
    /// <summary>Maximum civilian ships (FishingBoat) that may be sunk before mission fails (-1 = no restriction, 0 = zero tolerance).</summary>
    int MaxCivilianSinks = -1
);

public static class CampaignManager
{
    public static readonly IReadOnlyList<MissionConfig> Missions = new[]
    {
        new MissionConfig(
            MissionNumber : 1,
            CodeName      : "OPERATION COASTAL SWEEP",
            Briefing      : "Enemy patrol boats have been spotted along the coast.\n" +
                            "Sink 4 enemy destroyers or PT boats to clear the sea lane.\n" +
                            "You have 3 lives. Do not let more than 3 ships escape.",
            StartWave     : 1,
            EndWave       : 2,
            Lives         : 3,
            ShipsPerWave  : 8,
            Objective     : new MissionObjective(
                RequiredSinks    : 4,
                TargetTypes      : new[] { ShipType.Destroyer, ShipType.PtBoat },
                MaxEscaped       : 3,
                TorpedoBudget    : 0,
                MaxCivilianSinks : -1
            )
        ),
        new MissionConfig(
            MissionNumber : 2,
            CodeName      : "OPERATION CONVOY RAID",
            Briefing      : "A supply convoy is crossing the strait at dusk.\n" +
                            "Sink 5 cargo ships or tankers to cut off enemy supplies.\n" +
                            "Civilian casualties are unacceptable — no more than 2 escapes.",
            StartWave     : 2,
            EndWave       : 4,
            Lives         : 3,
            ShipsPerWave  : 10,
            Objective     : new MissionObjective(
                RequiredSinks    : 5,
                TargetTypes      : new[] { ShipType.Cargo, ShipType.Tanker },
                MaxEscaped       : 2,
                TorpedoBudget    : 0,
                MaxCivilianSinks : -1
            )
        ),
        new MissionConfig(
            MissionNumber : 3,
            CodeName      : "OPERATION SAFE PASSAGE",
            Briefing      : "Civilian fishing vessels are crossing a contested zone.\n" +
                            "Sink 6 enemy warships to protect the corridor.\n" +
                            "WARNING: Do NOT fire on civilian ships. Any civilian sunk is an immediate mission failure.",
            StartWave     : 3,
            EndWave       : 5,
            Lives         : 3,
            ShipsPerWave  : 12,
            Objective     : new MissionObjective(
                RequiredSinks    : 6,
                TargetTypes      : new[] { ShipType.Destroyer, ShipType.PtBoat, ShipType.Cruiser },
                MaxEscaped       : 4,
                TorpedoBudget    : 0,
                MaxCivilianSinks : 0
            )
        ),
        new MissionConfig(
            MissionNumber : 4,
            CodeName      : "OPERATION IRON CURTAIN",
            Briefing      : "Enemy cruisers are escorting a carrier group.\n" +
                            "Sink the carrier and at least 2 cruisers.\n" +
                            "WARNING: Torpedo reserves are limited to 12 shots. Make them count.",
            StartWave     : 4,
            EndWave       : 6,
            Lives         : 2,
            ShipsPerWave  : 10,
            Objective     : new MissionObjective(
                RequiredSinks    : 3,
                TargetTypes      : new[] { ShipType.Cruiser, ShipType.Carrier },
                MaxEscaped       : 4,
                TorpedoBudget    : 12,
                MaxCivilianSinks : -1
            )
        ),
        new MissionConfig(
            MissionNumber : 5,
            CodeName      : "OPERATION DEEP STRIKE",
            Briefing      : "The enemy fleet is at full battle strength.\n" +
                            "Survive 3 waves and sink at least 12 ships of any type.\n" +
                            "Only 1 life remains. There is no margin for error.",
            StartWave     : 6,
            EndWave       : 8,
            Lives         : 1,
            ShipsPerWave  : 15,
            Objective     : new MissionObjective(
                RequiredSinks    : 12,
                TargetTypes      : Array.Empty<ShipType>(),
                MaxEscaped       : 4,
                TorpedoBudget    : 0,
                MaxCivilianSinks : -1
            )
        ),
        new MissionConfig(
            MissionNumber : 6,
            CodeName      : "OPERATION FINAL STAND",
            Briefing      : "All enemy forces are converging on your position.\n" +
                            "Survive the full assault. Sink 20 ships to achieve victory.\n" +
                            "This is the last mission. Give everything you have.",
            StartWave     : 8,
            EndWave       : 10,
            Lives         : 2,
            ShipsPerWave  : 20,
            Objective     : new MissionObjective(
                RequiredSinks    : 20,
                TargetTypes      : Array.Empty<ShipType>(),
                MaxEscaped       : 3,
                TorpedoBudget    : 0,
                MaxCivilianSinks : -1
            )
        ),
    };

    public static MissionConfig GetMission(int missionNumber) =>
        Missions[Math.Clamp(missionNumber - 1, 0, Missions.Count - 1)];

    public static bool IsLastMission(int missionNumber) =>
        missionNumber >= Missions.Count;

    /// <summary>
    /// Returns how many target-type sinks exist in the current mission progress.
    /// </summary>
    public static int CountObjectiveSinks(MissionConfig mission, int totalSinks,
        Dictionary<ShipType, int> sinksByType)
    {
        if (mission.Objective.TargetTypes.Count == 0)
            return totalSinks;

        return mission.Objective.TargetTypes.Sum(t =>
            sinksByType.TryGetValue(t, out var count) ? count : 0);
    }
}

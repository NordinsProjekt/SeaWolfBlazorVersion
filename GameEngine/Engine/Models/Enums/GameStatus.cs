namespace GameEngine.Engine.Models.Enums;

public enum GameStatus
{
    StartScreen,
    Playing,
    WaveClear,          // brief inter-wave pause
    Paused,
    GameOver,
    MissionBriefing,    // campaign: show mission brief before starting
    MissionComplete,    // campaign: mission succeeded
    CampaignComplete    // campaign: all missions finished
}

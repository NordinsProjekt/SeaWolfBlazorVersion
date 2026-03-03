namespace SeaWolfBlazorVersion.Engine.Models.Enums;

public enum ShipDamageState
{
    Healthy,   // full speed, no VFX
    Burning,   // 50% speed, fire particle overlay
    Sinking    // removing from game, tilt + submerge animation plays
}

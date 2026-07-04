using System.Linq;

namespace GameEngine.Engine;

/// <summary>
/// Shared torpedo-tube geometry — a fixed fan of firing angles (matching the
/// original Sea Wolf periscope) rather than free-aim.
///
/// The angles are deliberately NOT evenly spaced by degree. They're spaced so
/// each tube's line crosses the main near-lane ship band
/// (<see cref="ReferenceLaneY"/>) at evenly-spaced X positions. That's what
/// actually matters for feel: a ship crossing the middle of the screen should
/// have just as good a tube lined up on it as one nearer an edge.
///
/// Five tubes, not seven. An earlier pass spread 7 tubes across the full
/// canvas width (40..1240) to close the old edge dead-zones, but the outer
/// two tubes' crosshairs landed in the periscope vignette's dark corners —
/// visible in theory, effectively dead in practice, since you can't aim at
/// what you can't see well. Five tubes spanning 240..1040 keeps every
/// crosshair inside the clearly-lit periscope circle while still spacing
/// evenly by X (not by angle), which is the part that actually fixed the
/// original "gaps near the edges" complaint.
///
/// Both the engine (firing trajectory) and the JS renderer (drawing the fan)
/// read from this one place so they can never drift out of sync.
/// </summary>
public static class TorpedoTubes
{
    /// <summary>Torpedo launch origin — bottom-centre of the 1280x720 canvas.</summary>
    public const float LaunchX = 640f;
    public const float LaunchY = 680f;

    /// <summary>Y of the main ship lane, used as the reference depth for spacing tubes.</summary>
    public const float ReferenceLaneY = 420f;

    /// <summary>
    /// Where each tube's line crosses <see cref="ReferenceLaneY"/>, evenly
    /// spaced (200 px apart) and kept inside the visible periscope circle.
    /// </summary>
    public static readonly float[] TargetX = [240f, 440f, 640f, 840f, 1040f];

    /// <summary>Firing angle for each tube, in degrees (0 = straight up).</summary>
    public static readonly float[] AngleDeg = TargetX
        .Select(tx => MathF.Atan2(tx - LaunchX, LaunchY - ReferenceLaneY) * (180f / MathF.PI))
        .ToArray();

    public static readonly int Count = TargetX.Length;

    /// <summary>Straight-up tube, selected at the start of every game/wave.</summary>
    public const int DefaultTube = 2;
}

using UnityEngine;



/// <summary>
/// The RandomTeleport ability provides a way for ships to perform a long-range random teleport to another area in the game.
/// </summary>
public sealed class AbilityTeleportRandom : AbilityTeleport
{
    public override string GetGUIName()
    {
        return "Random Teleport";
    }
}

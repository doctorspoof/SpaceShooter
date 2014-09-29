using UnityEngine;



/// <summary>
/// The TargettedTeleport ability allows for ships to teleport to a specific point in the game.
/// </summary>
public sealed class AbilityTeleportTargetted : AbilityTeleport
{
    public override string GetGUIName()
    {
        return "Targetted Teleport";
    }
}

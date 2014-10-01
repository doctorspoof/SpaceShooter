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
    
    public override void ActivateAbility (GameObject caster)
    {
        if(HasCooled())
        {
            Vector2 direction = Random.insideUnitCircle.normalized;
            float range = Random.Range(m_range * 0.25f, m_range);
            
            caster.GetComponent<Ship>().TeleportTo(direction, range);
            
            ResetCooldown();
        }
    }
}

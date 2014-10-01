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
    
    public override void ActivateAbility (GameObject caster)
    {
        if(HasCooled())
        {
            Vector3 direction = Camera.main.ScreenToWorldPoint(Input.mousePosition) - caster.transform.position;
            direction.z = 0.0f;
            direction.Normalize();
            
            caster.GetComponent<Ship>().TeleportTo(new Vector2(direction.x, direction.y), m_range);
        
            ResetCooldown();
        }
    }
}

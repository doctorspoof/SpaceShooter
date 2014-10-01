using UnityEngine;
using System.Collections;

public class AbilityShieldCollapseExplode : AbilityShieldCollapse 
{
    public override string GetGUIName()
    {
        return "Firestorm";
    }
    
    public override void ActivateAbility (GameObject caster)
    {
        if(HasCooled())
        {
            
        }
    }
}

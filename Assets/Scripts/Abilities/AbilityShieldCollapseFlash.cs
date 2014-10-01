using UnityEngine;
using System.Collections;

public class AbilityShieldCollapseFlash : AbilityShieldCollapse 
{
    public override string GetGUIName()
    {
        return "Flashbang";
    }
    
    public override void ActivateAbility (GameObject caster)
    {
        if(HasCooled())
        {
            
        }
    }
}

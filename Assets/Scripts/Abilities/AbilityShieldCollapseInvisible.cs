using UnityEngine;
using System.Collections;

public class AbilityShieldCollapseInvisible : AbilityShieldCollapse 
{
    public override string GetGUIName()
    {
        return "Fade";
    }
    
    public override void ActivateAbility (GameObject caster)
    {
        if(HasCooled())
        {
            
        }
    }
}

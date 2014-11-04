using UnityEngine;
using System.Collections;

public class AbilityShieldCollapseInvisible : AbilityShieldCollapse 
{
    float m_invisDuration = 0.0f;

    #region get/Set
    public void SetDuration(float duration)
    {
        m_invisDuration = duration;
    }
    #endregion

    public override string GetGUIName()
    {
        return "Fade";
    }
    
    public override void ActivateAbility (GameObject caster)
    {
        if(HasCooled())
        {
            caster.GetComponent<Ship>().AddDebuff(new DebuffEthereal(m_invisDuration, caster));
            
            ResetCooldown();
        }
    }
}

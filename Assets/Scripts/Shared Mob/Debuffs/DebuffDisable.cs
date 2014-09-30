using UnityEngine;
using System.Collections;

public class DebuffDisable : Debuff 
{
    public DebuffDisable (float duration, GameObject victim)
    {
        m_duration = duration;
        m_currentDuration = duration;
        m_targetMob = victim;
        
        ApplyEffect();
    }
    
    protected override void ApplyEffect ()
    {
        m_targetMob.GetComponent<Ship>().SetDisabled(true);
    }
    
    protected override void UnapplyEffect ()
    {
        m_targetMob.GetComponent<Ship>().SetDisabled(false);
    }
}

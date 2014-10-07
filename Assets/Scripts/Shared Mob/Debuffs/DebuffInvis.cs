using UnityEngine;
using System.Collections;

public class DebuffInvis : Debuff 
{
    public DebuffInvis (float duration, GameObject victim)
    {
        m_duration = duration;
        m_currentDuration = duration;
        m_targetMob = victim;
        
        ApplyEffect();
    }
    
    protected override void ApplyEffect ()
    {
        m_targetMob.GetComponent<Ship>().SetInvisible(true);
    }
    
    protected override void UnapplyEffect ()
    {
        m_targetMob.GetComponent<Ship>().SetInvisible(false);
    }
}

using UnityEngine;
using System.Collections;

public class DebuffEthereal : Debuff 
{
    public DebuffEthereal (float duration, GameObject victim)
    {
        m_duration = duration;
        m_currentDuration = duration;
        m_targetMob = victim;
        
        ApplyEffect();
    }
    
    protected override void ApplyEffect ()
    {
        m_targetMob.GetComponent<Ship>().SetEthereal(true);
    }
    
    protected override void UnapplyEffect ()
    {
        m_targetMob.GetComponent<Ship>().SetEthereal(false);
    }
}

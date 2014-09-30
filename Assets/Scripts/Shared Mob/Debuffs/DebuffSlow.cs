using UnityEngine;
using System.Collections;

public class DebuffSlow : Debuff 
{
    float m_slowEffect = 0.0f;          //Percentage!

    public DebuffSlow (float duration, float slowEffect, GameObject victim)
    {
        m_slowEffect = slowEffect;
        m_duration = duration;
        m_currentDuration = duration;
        m_targetMob = victim;
        
        ApplyEffect();
    }
	
    protected override void ApplyEffect ()
    {
        m_targetMob.GetComponent<Ship>().SetCurrentShipSpeed(m_targetMob.GetComponent<Ship>().GetCurrentShipSpeed() * m_slowEffect);
    }
    
    protected override void UnapplyEffect ()
    {
        //m_targetMob.GetComponent<Ship>().SetCurrentShipSpeed(m_targetMob.GetComponent<Ship>().GetCurrentShipSpeed() * (1.0f / m_slowEffect));
        m_targetMob.GetComponent<Ship>().SetCurrentShipSpeed(m_targetMob.GetComponent<Ship>().GetMaxShipSpeed());
    }
}

using UnityEngine;
using System.Collections;

public class DebuffDoT : Debuff 
{
    float m_damagePerSecond = 0.0f;
    float m_damageCatch = 0.0f;

    public DebuffDoT (float duration, int totalDamage, GameObject victim)
    {
        m_duration = duration;
        m_damagePerSecond = totalDamage / duration;
        m_damageCatch = 0.0f;
        m_currentDuration = duration;
        m_targetMob = victim;
        
        ApplyEffect();
    }
    
    protected override void Update (float deltaTime)
    {
        m_damageCatch += m_damagePerSecond * deltaTime;
        
        if((int)m_damageCatch > 0)
        {
            int damApply = (int)m_damageCatch;
            m_targetMob.GetComponent<HealthScript>().DamageMob(damApply, null);
            m_damageCatch -= damApply;
        }
    }
}

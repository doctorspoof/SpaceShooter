using UnityEngine;
using System.Collections;

public abstract class AbilityShieldCollapse : AbilityPassive 
{
    protected float m_effectRange = 0f;         
    
    public float GetEffectRange()
    {
        return m_effectRange;
    }
    public void SetEffectRange(float newRange)
    {
        m_effectRange = newRange;
    }
}

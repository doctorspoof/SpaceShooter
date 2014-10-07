using UnityEngine;
using System.Collections;

[System.Serializable]
public class Debuff 
{
    protected Texture m_inGameIcon;
	
    protected float m_duration;
    public float m_currentDuration;
    
    protected GameObject m_targetMob;
    
    /// <summary>
    /// Reduces the cooldown.
    /// </summary>
    /// <returns><c>true</c>, if the debuff is still going, <c>false</c> otherwise.</returns>
    /// <param name="reduction">Reduction.</param>
    public bool ReduceCooldown(float reduction)
    {
        m_currentDuration -= reduction;
        Update(reduction);
        
        if(m_currentDuration <= 0)
        {
            UnapplyEffect();
            return false;
        }
        else
        {
            return true;
        }
    }
    
    protected virtual void Update(float deltaTime)
    {
        
    }
    
    protected virtual void ApplyEffect()
    {
        Debug.Log ("Default effect application was called (Did you forget to override?");
    }
    
    protected virtual void UnapplyEffect()
    {
        Debug.Log ("Default effect unapplication was called (Did you forget to override?");
    }
}

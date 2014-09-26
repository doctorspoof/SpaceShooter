using UnityEngine;



/// <summary>
/// An abstract for each ability, allows for a generic cooldown method in Ship to clean up code.
/// </summary>
[System.Serializable] public abstract class Ability
{
    protected bool m_locked = false;
    protected bool m_cooling = false;

    protected float m_maxCooldown = 0.5f;     //!< How long the ability requires to cool down.
    protected float m_currentCooldown = 0f;   //!< The cooldown for the ability.
    


    public bool IsLocked()
    {
        return m_locked;
    }


    public bool IsCooling()
    {
        return m_cooling;
    }
    
    
    public bool HasCooled()
    {
        return m_currentCooldown <= 0f;
    }
    
    
    public void SetLockState (bool isLocked)
    {
        m_locked = isLocked;
    }


    public void SetCoolingState (bool isCooling)
    {
        m_cooling = isCooling;
    }
    
    
    public float GetMaxCooldown()
    {
        return m_maxCooldown;
    }
    
    
    public float GetCurrentCooldown()
    {
        return m_currentCooldown;
    }
    
    
    public void SetMaxCooldown (float cooldown)
    {
        m_maxCooldown = Mathf.Max (0f, cooldown);

        m_currentCooldown = Mathf.Min (m_currentCooldown, m_maxCooldown);
    }
    
    
    /// <summary>
    /// Used to increment or decrement the current cooldown.
    /// </summary>
    /// <param name="alterBy">This cannot cause the current cooldown to raise above the maximum or zero.</param>
    public void AlterCooldown (float alterBy)
    {
        m_currentCooldown = Mathf.Clamp (m_currentCooldown + alterBy, 0f, m_maxCooldown);
        
        m_cooling = m_currentCooldown != 0f;
    }
    
    
    /// <summary>
    /// Sets the current cooldown to the maximum specified.
    /// </summary>
    public void ResetCooldown()
    {
        m_currentCooldown = m_maxCooldown;
        
        m_cooling = false;
    }
    
    
    /// <summary>
    /// Causes the cooldown to become zero.
    /// </summary>
    public void ImmediatelyCool()
    {
        m_currentCooldown = 0f;
        
        m_cooling = false;
    }
}
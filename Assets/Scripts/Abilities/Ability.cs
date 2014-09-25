using UnityEngine;



/// <summary>
/// An abstract for each ability, allows for a generic cooldown method in Ship to clean up code.
/// </summary>
public abstract class Ability
{
    protected float m_maxCooldown = 0.5f;     //!< How long the ability requires to cool down.
    protected float m_currentCooldown = 0f;   //!< The cooldown for the ability.
    
    
    public bool HasCooled()
    {
        return m_currentCooldown <= 0f;
    }
    
    
    public float GetCurrentCooldown()
    {
        return m_currentCooldown;
    }
    
    
    public float GetMaxCooldown()
    {
        return m_maxCooldown;
    }
    
    
    public void SetMaxCooldown (float cooldown, bool startCooldown = false)
    {
        m_maxCooldown = Mathf.Max (0f, cooldown);
        
        if (startCooldown)
        {
            ResetCooldown();
        }
    }
    
    
    /// <summary>
    /// Used to increment or decrement the current cooldown.
    /// </summary>
    /// <param name="alterBy">This cannot cause the current cooldown to raise above the maximum or zero.</param>
    public void AlterCooldown (float alterBy)
    {
        m_currentCooldown = Mathf.Clamp (m_currentCooldown + alterBy, 0f, m_maxCooldown);
    }
    
    /// <summary>
    /// Sets the current cooldown to the maximum specified.
    /// </summary>
    public void ResetCooldown()
    {
        m_currentCooldown = m_maxCooldown;
    }
    
    /// <summary>
    /// Causes the cooldown to become zero.
    /// </summary>
    public void ImmediateCooldown()
    {
        m_currentCooldown = 0f;
    }
}
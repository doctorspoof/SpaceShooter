using UnityEngine;



/// <summary>
/// The GravityControl ability allows ships to change the intensity of the gravity effect being applied to them.
/// </summary>
public sealed class GravityControl : Ability 
{
    public bool enabled = false;    //!< Used to indicate whether the ability is currently enabled or not.

    float m_maxChange = 0f;         //!< Represents how much gravity can be increased or reduced (+-100%).
    float m_currentChange = 0f;     //!< The current applied effect of the ability.


    public float GetMaxChange()
    {
        return m_maxChange;
    }
    
    
    public float GetCurrentChange()
    {
        return m_currentChange;
    }


    /// <summary>
    /// Sets the maximum gravity change that can be applied.
    /// </summary>
    /// <param name="maxChange">Cannot fall below 0.</param>
    public void SetMaxChange (float maxChange)
    {
        m_maxChange = Mathf.Max (0f, maxChange);
    }


    /// <summary>
    /// Sets the current gravity change.
    /// </summary>
    /// <param name="currentChange">Will be clamped to the maximum or minimum available.</param>
    public void SetCurrentChange (float currentChange)
    {
        m_currentChange = Mathf.Clamp (currentChange, -m_maxChange, m_maxChange);
    }
}
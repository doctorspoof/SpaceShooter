using UnityEngine;



/// <summary>
/// Teleport allows for the usage of teleport abilities such as TargettedTeleport and RandomTeleport.
/// </summary>
public abstract class AbilityTeleport : AbilityActive
{
    protected float m_range = 0f; //!< The range of the teleport.


    public float GetRange()
    {
        return m_range;
    }


    /// <summary>
    /// Sets the range of the teleport.
    /// </summary>
    /// <param name="range">Will be converted to a positive number if a negative is given.</param>
    public void SetRange (float range)
    {
        m_range = Mathf.Abs (range);
    }
}

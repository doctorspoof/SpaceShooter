using UnityEngine;
using System.Collections;

public abstract class BaseEquipment : MonoBehaviour
{    
    [SerializeField, Range (0, 20)]     protected   int         m_numAugments = 1;
    [SerializeField]                    protected   Augment[]   m_augmentSlots = null;
    
    #region Getters/Setters

    public int GetMaxAugmentNum()
    {
        return m_augmentSlots.Length;
    }

    public Augment GetAugmentInSlot(int index)
    {
        try
        {
            return m_augmentSlots[index];
        }
        catch (System.Exception ex) 
        {
            Debug.Log ("Exception caught: " + ex.Message);
        }
        
        Debug.Log ("Couldn't access augment at index " + index);
        return null;
    }

    public void SetAugmentIntoSlot(int index, Augment reference)
    {
        try
        {
            m_augmentSlots[index] = reference;
        }
        catch (System.Exception ex) 
        {
            Debug.Log ("Exception caught: " + ex.Message);
        }
        
        ResetToBaseStats();
        CalculateCurrentStats();
    }

    #endregion

    #region Behaviour Functions

    protected virtual void Awake()
    {
        if (m_augmentSlots.Length != m_numAugments)
        {
            ResizeAugments (m_numAugments);
        }

        ResetToBaseStats();
        CalculateCurrentStats();
    }

    #endregion

    #region Custom Functions

    /// <summary>
    /// Resizes the augments array to the ideal size losing all data beyond the given size. Will not modify the array on failure.
    /// </summary>
    /// <param name="idealLength">The target length for the augments array.</param>
    protected void ResizeAugments (int idealLength)
    {
        if (idealLength >= 0)
        {
            // Avoid out-of-range errors by checking for the maximum value for the loop
            int maxLength = m_augmentSlots.Length < idealLength ? m_augmentSlots.Length : idealLength;
            Augment[] augments = new Augment[idealLength];

            // Copy the data across
            for (int i = 0; i < maxLength; ++i)
            {
                augments[i] = m_augmentSlots[i];
            }

            m_augmentSlots = augments;
        }

        else
        {
            Debug.LogError ("Attempt to resize " + name + ".BaseEquipment.m_augmentSlots with idealLength of: " + idealLength);
        }
    }



    protected abstract void CalculateCurrentStats();
    protected abstract void ResetToBaseStats();
    
    protected abstract void ElementResponseFire(int tier);
    protected abstract void ElementResponseIce(int tier);
    protected abstract void ElementResponseEarth(int tier);
    protected abstract void ElementResponseLightning(int tier);
    protected abstract void ElementResponseLight(int tier);
    protected abstract void ElementResponseDark(int tier);
    protected abstract void ElementResponseSpirit(int tier);
    protected abstract void ElementResponseGravity(int tier);
    protected abstract void ElementResponseAir(int tier);
    protected abstract void ElementResponseOrganic(int tier);
    #endregion
}

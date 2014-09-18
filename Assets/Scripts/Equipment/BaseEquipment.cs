using UnityEngine;
using System.Collections;

public abstract class BaseEquipment : MonoBehaviour
{    
    [SerializeField, Range (0, 20)]     protected   int             m_numAugments = 1;
    [SerializeField]                    protected   Augment[]       m_augmentSlots = null;
    [SerializeField]                    protected   ItemWrapper[]   m_augmentItemSlots = null;
    
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
    public ItemWrapper GetItemWrapperInSlot(int index)
    {
        try
        {
            return m_augmentItemSlots[index];
        }
        catch (System.Exception ex) 
        {
            Debug.Log ("Exception caught: " + ex.Message);
        }
        
        Debug.Log ("Couldn't access item at index " + index);
        return null;
    }

    public bool SetAugmentItemIntoSlot(int index, ItemWrapper reference)
    {
        try
        {
            m_augmentItemSlots[index] = reference;
            SetAugmentIntoSlot(index, reference.GetItemPrefab().GetComponent<Augment>());
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.Log ("Exception caught: " + ex.Message);
        }
        
        return false;
    }
    void SetAugmentIntoSlot(int index, Augment reference)
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
    
    public bool RemoveAugmentItemFromSlot (int index, ItemWrapper reference)
    {
        try
        {
            if (m_augmentItemSlots[index] == reference)
            {
                m_augmentItemSlots[index] = null;
                m_augmentSlots[index] = null;
            }            
            
            else
            {
                throw new System.Exception ("Item at " + index + " was not expected item '" + reference.GetItemName());
            }
        }

        catch (System.Exception error)
        {
            Debug.LogError (error.Message);
            return false;
        }

        ResetToBaseStats();
        CalculateCurrentStats();
        return true;
    }

    #endregion

    #region Behaviour Functions

    protected virtual void Awake()
    {
        if (m_augmentSlots.Length != m_numAugments)
        {
            ResizeAugments (m_numAugments);
        }
        
        m_augmentItemSlots = new ItemWrapper[m_numAugments];

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
    
    protected abstract void ElementResponseFire (float scalar);
    protected abstract void ElementResponseIce (float scalar);
    protected abstract void ElementResponseEarth (float scalar);
    protected abstract void ElementResponseLightning (float scalar);
    protected abstract void ElementResponseLight (float scalar);
    protected abstract void ElementResponseDark (float scalar);
    protected abstract void ElementResponseSpirit (float scalar);
    protected abstract void ElementResponseGravity (float scalar);
    protected abstract void ElementResponseAir (float scalar);
    protected abstract void ElementResponseOrganic (float scalar);

    #endregion
}

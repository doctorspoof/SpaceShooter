using UnityEngine;
using System.Collections;

public abstract class BaseEquipment : MonoBehaviour
{
    [SerializeField]    protected Augment[] m_augmentSlots;
    
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
        catch (System.Exception ex) {
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
        catch (System.Exception ex) {
            Debug.Log ("Exception caught: " + ex.Message);
        }
        
        ResetToBaseStats();
        CalculateCurrentStats();
    }
    #endregion
    
    #region Custom Functions
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

using UnityEngine;



/// <summary>
/// Each augment has an element attached to it which adds different effects dependant on what it is attached to.
/// </summary>
public enum Element
{
    Fire = 1,
    Ice = 2,
    Earth = 3,
    Lightning = 4,
    Light = 5,
    Dark = 6,
    Spirit = 7,
    Gravity = 8,
    Air = 9,
    Organic = 10
}



/// <summary>
/// An augment is a type of modifier that gets attached to equipment and ships. It contains and element and a tier, depending on
/// these values it can dramatically change what it is attached to.
/// </summary>
[System.Serializable]
public sealed class Augment
{
    #region Unity modifiable variables

    [SerializeField, Range (1, 3)]  int m_tier = 1;                     // The tier of the augment, this effects how strong of an effect it will have
    [SerializeField]                Element m_element = Element.Fire;   // The elemental affinity of the augment, changes what effects should be applied
    
    #endregion


    #region Getters & setters
    
    // Getters
    public int GetTier()
    {
        return m_tier;
    }
    
    
    public Element GetElement()
    {
        return m_element;
    }
    
    #endregion


    #region Constructors and destructor

    /// <summary>
    /// Initializes a new instance of the <see cref="Augment"/> class.
    /// </summary>
    /// <param name="tier">The tier must range from 1 - 3, otherwise the constructor will fail.</param>
    /// <param name="element">Specify the element type you wish this to be.</param>
    Augment (int tier = 1, Element element = Element.Fire)
    {
        // Ensure valid tier values
        if (tier > 0 && tier < 4)
        {
            m_tier = tier;
            m_element = element;
        }

        // Use default values if problems arise
        else
        {
            m_tier = 1;
            m_element = Element.Fire;
            
            Debug.LogError ("Attempt to create Augment with invalid values: tier == " + tier + " && element == " + element);
        }
    }

    #endregion
}

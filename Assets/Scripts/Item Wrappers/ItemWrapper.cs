using UnityEngine;



/// <summary>
/// Used to indicate what type of item an ItemWrapper represents.
/// </summary>
[System.Serializable] public enum ItemType
{
    Weapon = 0,
    Shield = 1,
    Engine = 2,
    Plating = 3,
    CapitalWeapon = 4
}



/// <summary>
/// An ItemWrapper is used to hold all the information about an equippable item which the game needs to figure out whether it can be equipped,
/// how much it should cost to purchase, what tier it is as well as all the descriptive information required for the UI. ItemWrapper is used
/// to seperate the information of a weapon and the actual workings of a weapon.
/// </summary>
public class ItemWrapper : MonoBehaviour
{
    #region Unity modifiable variables

    [SerializeField]                    ItemType m_itemType = ItemType.Weapon;  //!< Indicates which ItemType the wrapper represents.
    
    [SerializeField, Range (0, 129)]    int m_itemID = 0;                       //!< The ID which can be used to reconstruct the ItemWrapper. Allows for sending ItemWrapper
                                                                                //!< information over the network.
    [SerializeField, Range (1, 3)]      int m_itemTierID = 1;                   //!< The actual tier of the item, useful in shop inventory algorithms.
    [SerializeField, Range (0, 10000)]  int m_baseCost = 0;                     //!< The base cost which is used to calculate pricing in shops.
    
    [SerializeField]                    string m_itemName = null;               //!< The name of the item.
    [SerializeField]                    Texture m_itemIcon = null;              //!< The icon used in the GUI of the item.
    [SerializeField]                    GameObject m_itemPrefab = null;         //!< A reference to the prefab of the item.

    #endregion Unity modifiable variables


    #region Internal data

    string m_itemDescription = null;

    #endregion Internal data


    #region getset

    public ItemType GetItemType()
    {
        return m_itemType;
    }


    public int GetItemID()
    {
        return m_itemID;
    }


    public int GetItemTierID()
    {
        return m_itemTierID;
    }


    public int GetBaseCost()
    {
        return m_baseCost;
    }


    public string GetItemName()
    {
        return m_itemName;
    }


    public Texture GetIcon()
    {
        return m_itemIcon;
    }


    public GameObject GetItemPrefab()
    {
        return m_itemPrefab;
    }


    /// <summary>
    /// Gets the text which should be shown when hovering over the item.
    /// </summary>
    /// <returns>The hover text with no price.</returns>
    public string GetHoverText()
    {
        if (m_itemDescription == null)
        {
            CollectDescription();
        }

        return (m_itemName + System.Environment.NewLine + m_itemDescription);
    }


    /// <summary>
    /// Gets the text which should be shown when hovering over the item.
    /// </summary>
    /// <returns>The hover text with the price included.</returns>
    /// <param name="price">The price which should be displayed.</param>
    public string GetHoverText (int price)
    {
        // Lazy load the item description if necessary
        if (m_itemDescription == null)
        {
            CollectDescription();
        }

        return (m_itemName + System.Environment.NewLine + "Cost: $" + price.ToString() + System.Environment.NewLine + m_itemDescription);
    }

    #endregion Getters & setters


    #region Initialisation

    /// <summary>
    /// Causes the ItemWrapper to contact the ItemDescriptionHolder to retrieve its item description.
    /// </summary>
    public void CollectDescription()
    {
        m_itemDescription = ItemDescriptionHolder.GetDescriptionFromID (m_itemID);
    }

    #endregion Initialisation
}

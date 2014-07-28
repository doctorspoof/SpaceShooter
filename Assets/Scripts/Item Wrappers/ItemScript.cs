using UnityEngine;
using System.Collections;

[System.Serializable]
public enum ItemType
{
    Weapon = 0,
    Shield = 1,
    Engine = 2,
    Plating = 3,
    CapitalWeapon = 4
}

public class ItemScript : MonoBehaviour
{
    [SerializeField] ItemType m_typeOfItem;

    [SerializeField] int m_ItemTierID = 1;

    [SerializeField] Texture m_equipmentIcon;

    [SerializeField] string m_equipmentName;
    [SerializeField] int m_descriptionID;

    [SerializeField] GameObject m_equipmentReference;

    string m_equipmentDescription = null;

    public int m_cost;

    public int m_equipmentID;

    #region getset

    public ItemType GetTypeOfItem()
    {
        return m_typeOfItem;
    }

    public void SetTypeOfItem(ItemType typeOfItem_)
    {
        m_typeOfItem = typeOfItem_;
    }

    public int GetItemTierID()
    {
        return m_ItemTierID;
    }

    public void SetItemTierID(int itemTierID_)
    {
        m_ItemTierID = itemTierID_;
    }

    public Texture GetIcon()
    {
        return m_equipmentIcon;
    }

    public GameObject GetEquipmentReference()
    {
        return m_equipmentReference;
    }

    public string GetItemName()
    {
        return m_equipmentName;
    }

    public string GetShopText()
    {
        /*string output = "";
        output += m_equipmentName;
        output += System.Environment.NewLine + m_equipmentDescription;
        return output;*/

        //m_equipmentDescription = ItemDescriptionHolder.GetDescriptionFromID(m_equipmentID);

        if (m_equipmentDescription == null)
        {
            m_equipmentDescription = ItemDescriptionHolder.GetDescriptionFromID(m_equipmentID);
        }

        return (m_equipmentName + System.Environment.NewLine + m_equipmentDescription);
    }

    public string GetShopText(int price)
    {
        if (m_equipmentDescription == null)
        {
            m_equipmentDescription = ItemDescriptionHolder.GetDescriptionFromID(m_equipmentID);
        }

        return (m_equipmentName + System.Environment.NewLine + "Cost: $" + price.ToString() + System.Environment.NewLine + m_equipmentDescription);
    }

    #endregion getset

    public void CollectDescription()
    {
        m_equipmentDescription = ItemDescriptionHolder.GetDescriptionFromID(m_equipmentID);
    }
    
}

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
	public ItemType m_typeOfItem;
    public int m_ItemTierID = 1;

	[SerializeField]
	Texture m_equipmentIcon;

	public Texture GetIcon()
	{
		return m_equipmentIcon;
	}

	[SerializeField]
	string m_equipmentName;
	[SerializeField]
	//string m_equipmentDescription;
	int m_descriptionID;
	string m_equipmentDescription = null;
	
	public int m_cost;
	
	public int m_equipmentID;

	[SerializeField]
	GameObject m_equipmentReference;
	public GameObject GetEquipmentReference()
	{
		return m_equipmentReference;
	}

	// Use this for initialization
	void Start () 
	{

	}
	
	// Update is called once per frame
	void Update () 
	{
		
	}

	public void CollectDescription()
	{
		m_equipmentDescription = ItemDescriptionHolder.GetDescriptionFromID(m_equipmentID);
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

		if(m_equipmentDescription == null)
		{
			m_equipmentDescription = ItemDescriptionHolder.GetDescriptionFromID(m_equipmentID);
		}

		return (m_equipmentName + System.Environment.NewLine + m_equipmentDescription);
	}
    public string GetShopText(int price)
    {
        if(m_equipmentDescription == null)
        {
            m_equipmentDescription = ItemDescriptionHolder.GetDescriptionFromID(m_equipmentID);
        }
        
        return (m_equipmentName + System.Environment.NewLine + "Cost: $" + price.ToString() + System.Environment.NewLine + m_equipmentDescription);
    }
}

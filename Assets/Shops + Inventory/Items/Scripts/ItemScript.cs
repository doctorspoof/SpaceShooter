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

	[SerializeField]
	Texture m_equipmentIcon;

	public Texture GetIcon()
	{
		return m_equipmentIcon;
	}

	[SerializeField]
	string m_equipmentName;
	[SerializeField]
	string m_equipmentDescription;
	
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

	public string GetItemName()
	{
		return m_equipmentName;
	}
	public string GetShopText()
	{
		string output = "";
		output += m_equipmentName;
		output += System.Environment.NewLine + m_equipmentDescription;
		return output;
	}
}

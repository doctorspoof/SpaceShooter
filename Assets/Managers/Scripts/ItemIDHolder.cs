using UnityEngine;
using System.Collections;

[System.Serializable]
public class Item
{
	public GameObject itemObject;
	public int itemID;
}

public class ItemIDHolder : MonoBehaviour 
{
	public Item[] ItemList;

	// Use this for initialization
	void Start () 
	{
		for(int i = 0; i < ItemList.Length; i++)
		{
			if(ItemList[i] != null && ItemList[i].itemObject != null)
				ItemList[i].itemObject.GetComponent<ItemScript>().CollectDescription();
		}
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	public GameObject GetItemWithID(int id)
	{
		foreach(Item weapon in ItemList)
		{
			if(weapon.itemID == id)
				return weapon.itemObject;
		}

		Debug.Log ("Couldn't find equipment with id #" + id + ".");
		return null;
	}
}

using UnityEngine;
using System.Collections;

[System.Serializable]
public class Item
{
	public ItemScript itemObject;
	public int itemID;
}

public class ItemIDHolder : MonoBehaviour 
{
	public Item[] ItemList;// An array of every single available item in the game. Ideally the .itemID of each element will correspond to the index

	// Use this for initialization
	void Start () 
	{
		for(int i = 0; i < ItemList.Length; i++)
		{
			if(ItemList[i] != null && ItemList[i].itemObject != null)
				ItemList[i].itemObject.CollectDescription();
		}
	}
	
	/// <summary>
	/// Searches through an array of every item in the game looking for the desired itemID.
	/// </summary>
	/// <returns>The item with id, otherwise null.</returns>
	/// <param name="id">ID.</param>
	public ItemScript GetItemWithID (int id)
	{
		// Check if the id corresponds to the index of ItemList then check if the item is the desired item
		if (id >= 0 && id < ItemList.Length && 
		    ItemList[id] != null && ItemList[id].itemID == id)
		{
			if (ItemList[id].itemObject == null)
			{
				Debug.LogError ("An item was found in " + name + ".ItemIDHolder.ItemList with ID #" + id + " without a valid .itemObject");
			}

			return ItemList[id].itemObject;
		}
		else
		{
			foreach (Item item in ItemList)
			{
				if (item.itemID == id)
				{
					if (item.itemObject == null)
					{
						Debug.LogError ("An item was found in " + name + ".ItemIDHolder.ItemList with ID #" + id + " without a valid .itemObject");
					}

					return item.itemObject;
				}
			}
		}
		
		// Hopefully this will never happen as it could be very costly for performance
		Debug.Log ("Couldn't find equipment with id #" + id + ".");
		return null;
	}
}

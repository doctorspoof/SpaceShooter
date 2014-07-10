using UnityEngine;
using System.Collections;

[System.Serializable]
public class Item
{
	public GameObject itemObject;
	public int itemID;
	
	
	// Simply tests if the pointer is valid
	public static implicit operator bool (Item item)
	{
		return item != null;
	}
}

public class ItemIDHolder : MonoBehaviour 
{
	public Item[] ItemList;	// An array of every single available item in the game. Ideally the .itemID of each element will correspond to the index
	
	
	/// <summary>
	/// Searches through an array of every item in the game looking for the desired itemID.
	/// </summary>
	/// <returns>The item with id, otherwise null.</returns>
	/// <param name="id">ID.</param>
	public GameObject GetItemWithID (int id)
	{
		// Check if the id corresponds to the index of ItemList then check if the item is the desired item
		if (id >= 0 && id < ItemList.Length && 
		    ItemList[id] && ItemList[id].itemID == id)
		{
			if (!ItemList[id].itemObject)
			{
				Debug.LogError ("An item was found in " + name + ".ItemIDHolder.ItemList with ID #" + id + " without a valid .itemObject");
			}

			return ItemList[id].itemObject;
		}
		
		// Brute force it bro!
		else
		{
			foreach (Item item in ItemList)
			{
				if (item.itemID == id)
				{
					if (!item.itemObject)
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

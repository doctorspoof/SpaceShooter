using UnityEngine;



/// <summary>
/// A simple container struct-like class which features a reference to an item prefab and its corresponding itemID number.
/// </summary>
[System.Serializable] public sealed class Item
{
	public ItemWrapper itemObject;
	public int itemID;
}



/// <summary>
/// The entry point for classes which need to convert item ID numbers into real prefab refreshes. This is useful as it allows for RPC's to send information
/// about items which can then be reconstructed on the receivers end through the usage of the ItemIDHolder. It also maintains an array of every item in the game.
/// </summary>
public sealed class ItemIDHolder : MonoBehaviour 
{
	[SerializeField] Item[] m_itemList; //<! An array of every single available item in the game. Ideally the .itemID of each element will correspond to the index


    /// <summary>
    /// Allows for external retreiving of the item list.
    /// </summary>
    /// <returns>The central item list array.</returns>
    public Item[] GetItemList()
    {
        return m_itemList;
    }


	/// <summary>
    /// Ensures that each item has the correct description attached to it.
    /// </summary>
	void Awake() 
	{
		for (int i = 0; i < m_itemList.Length; ++i)
		{
			if (m_itemList[i] != null && m_itemList[i].itemObject != null)
            {
			    m_itemList[i].itemObject.CollectDescription();
            }
		}
	}


	
	/// <summary>
	/// Searches through an array of every item in the game looking for the desired itemID. Can return null if the item isn't found.
	/// </summary>
	/// <returns>The prefab reference of the item with the corresponding ID.</returns>
	/// <param name="id">The item to look for.</param>
	public ItemWrapper GetItemWithID (int id)
	{
		// Check if the id corresponds to the index of ItemList then check if the item is the desired item
		if (id >= 0 && id < m_itemList.Length && 
		    m_itemList[id] != null && m_itemList[id].itemID == id)
		{
			if (m_itemList[id].itemObject == null)
			{
				Debug.LogError ("An item was found in " + name + ".ItemIDHolder.ItemList with ID #" + id + " without a valid .itemObject");
			}

			return m_itemList[id].itemObject;
		}

        // Just do a brute force search for the item  
		else
		{
			foreach (Item item in m_itemList)
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

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;


public class ShopScript : MonoBehaviour 
{
	// Use ShopType to indicate whether the shop is a Shipyard or a standard shop
	public enum ShopType
	{
		Basic = 0,
		Shipyard = 1
	}



	/// Unity modifiable variables
	// Shop attributes
	[SerializeField] float m_shopRotationSpeed = 5f;				// How quickly the shop should rotate on the Z axis
	[SerializeField, Range (0, 20)] int m_shopCapacity = 0;			// How many items the shop can store
	[SerializeField, Range (0f, 10f)] float m_priceMultiplier = 1f;	// How much the item cost should be scaled by the shop

	[SerializeField] ShopType m_shopType = ShopType.Basic;			// The ShopType determines the functionality available

	[SerializeField] GameObject m_dockPoint = null;					// Where on the ship the player docks to access the shop

	// Weapons
	[SerializeField] bool m_canStockWeapons = false;				// Can the shop stock weapons
	[SerializeField] float m_weaponRarityMod = 1.0f;				// The higher the rarity mod the higher chance of good items spawning

	// Shields
	[SerializeField] bool m_canStockShields = false;				// See above
	[SerializeField] float m_shieldRarityMod = 1.0f;

	// Engines
	[SerializeField] bool m_canStockEngines = false;				// See above
	[SerializeField] float m_engineRarityMod = 1.0f;

	// Plating
	[SerializeField] bool m_canStockPlating = false;				// See above
	[SerializeField] float m_platingRarityMod = 1.0f;

	// CShip Weapons
	[SerializeField] bool m_canStockCWeapons = false;				// See above
	[SerializeField] float m_cWeaponRarityMod = 1.0f;



	/// Internal data
	bool m_hadRequestResponse = false;		// Used in determining whether the host has responded to the current client
	bool m_itemRequestResponse = false;		// Indicates the actual response from the server

	bool[] m_stockFlags = null;				// Simply contains each serialized stock flag
	bool[] m_requestedItems = null;			// Keeps a reference to which items have been requested from the shop
	
	float[] m_rarityMods = null;			// Contains the rarity mod values of each item type
	
	GameObject[] m_shopInventory = null;	// The current shop inventory

	ItemIDHolder m_itemIDs = null;			// Used for caching the ItemIDHolder
	LootTableScript m_lootTable = null;		// Used for caching the LootTableScript
	


	/// Getters, setters and properties
	public ShopType GetShopType()
	{
		return m_shopType;
	}


	public Vector3 GetDockPoint()
	{
		if (m_dockPoint)
		{
			return m_dockPoint.transform.position;
		}
		
		return transform.position;
	}


	public GameObject[] GetShopInventory()
	{
		return m_shopInventory;
	}


	// Calculates the cost of a particular item taking into account the price multiplier of the shop
	public int GetItemCost (int itemIndex)
	{
		if (itemIndex >= 0 && itemIndex < m_shopInventory.Length)
		{
			return (int) (m_shopInventory[itemIndex].GetComponent<ItemScript>().m_cost * m_priceMultiplier);
		}

		else
		{
			Debug.LogError ("Attempt to calculate item cost of item " + itemIndex + " when .Length == " + m_shopInventory.Length);
		}

		return int.MaxValue;
	}


	// Allows clients to assess whether the host has reponded to their item request or not
	public bool HasServerResponded (out bool response)
	{
		response = m_itemRequestResponse;
		return m_hadRequestResponse;
	}

	
	
	/// Behaviour functions
	// Initialise references to external scripts
	void Start() 
	{
		InitialiseItemIDs();
		InitialiseLootTable();
		
		// Compile stockFlags
		m_stockFlags = new bool[5] { m_canStockWeapons, m_canStockShields, m_canStockEngines, m_canStockPlating, m_canStockCWeapons };
		
		// Compile rarityMods
		m_rarityMods = new float[5] { m_weaponRarityMod, m_shieldRarityMod, m_engineRarityMod, m_platingRarityMod, m_cWeaponRarityMod };

		// Get the inventory ready
		m_shopInventory = new GameObject[m_shopCapacity];
		m_requestedItems = Enumerable.Repeat (false, m_shopCapacity).ToArray();
	}
	
	
	// Rotate the shop
	void FixedUpdate() 
	{
		RotateShop();
	}
	
	
	// Update the rotation over the network
	void OnSerializeNetworkView (BitStream stream, NetworkMessageInfo info)
	{
		float rotZ = transform.rotation.eulerAngles.z;
		
		stream.Serialize (ref rotZ);
		
		if (!stream.isWriting)
		{
			transform.rotation = Quaternion.Euler(0, 0, rotZ);
		}
	}
	
	
	
	/// Private functions
	// Obtain a reference to the ItemIDHolder script
	void InitialiseItemIDs()
	{
		// Find Item Manager
		GameObject itemManager = GameObject.FindGameObjectWithTag ("ItemManager");
		
		if (itemManager)
		{
			m_itemIDs = itemManager.GetComponent<ItemIDHolder>();
			
			if (!m_itemIDs)
			{
				Debug.LogError ("ItemManager object does not contain an ItemIDHolder component.");
			}
		}
		
		else
		{
			Debug.LogError ("Unable to find object with tag: ItemManager");
		}
	}
	
	
	// Obtain a reference to the LootTableScript component
	void InitialiseLootTable()
	{
		// Find Loot Table
		GameObject lootTable = GameObject.FindGameObjectWithTag ("LootTable");
		
		if (lootTable)
		{
			m_lootTable = lootTable.GetComponent<LootTableScript>();
			
			if (!m_itemIDs)
			{
				Debug.LogError ("LootTable object does not contain a LootTableScript component.");
			}
		}
		
		else
		{
			Debug.LogError ("Unable to find object with tag: LootTable");
		}
	}
	
	
	// Keeps the shop rotating in the desired fashion
	void RotateShop()
	{
		transform.rotation = Quaternion.Euler (new Vector3 (0, 0, transform.rotation.eulerAngles.z + (m_shopRotationSpeed * Time.deltaTime)));
	}


	// Resets the response booleans to default values so another request can be made
	void ResetResponse (bool fakeDecline = false)
	{
		m_hadRequestResponse = fakeDecline;
		m_itemRequestResponse = false;
	}


	// Determines the correct index for an item in the inventory, checking the preferred slot first
	int GetDesiredIndex (int itemID, int preferredIndex = -1, bool unrequestedOnly = false)
	{
		ItemScript script;

		// PreferredIndex has a higher priority than the itemID
		if (preferredIndex >= 0 && preferredIndex < m_shopInventory.Length)
		{
			script = m_shopInventory[preferredIndex].GetComponent<ItemScript>();

			// Check that the itemID is correct
			if (script && script.m_equipmentID == itemID)
			{
				// Check whether it matters if it has been requested or not
				if (!unrequestedOnly || !m_requestedItems[preferredIndex])
				{
					return preferredIndex;
				}
			}
		}


		// Since the preferred index method didn't work we must search each GameObject
		for (int i = 0; i < m_shopInventory.Length; ++i)
		{
			script = m_shopInventory[i].GetComponent<ItemScript>();

			if (script && script.m_equipmentID == itemID && (!unrequestedOnly || !m_requestedItems[i]))
			{
				return i;
			}
		}

		// Didn't find any so just output -1
		return -1;
	}


	
	/// Network functions
	// Allows an item slot to be synchronised over the network
	[RPC] void PropagateItemInSlot (int itemID, int slot)
	{
		// Turn it into an object and let it sit in the shop
		m_shopInventory[slot] = m_itemIDs.GetItemWithID (itemID);
		m_requestedItems[slot] = false;
	}
	
	
	// Allows an item removal to be synchronised over the network
	[RPC] void PropagateInventoryRemoval (int slot)
	{
		// Null the pointer instead of deleting it so that it will show an empty space in the GUI
		m_shopInventory[slot] = null;
		m_requestedItems[slot] = false;
	}


	// Used by clients to request an item from the server
	[RPC] void RequestItem (int itemID, int preferredIndex, NetworkMessageInfo message)
	{
		bool reply = false;
		int index = GetDesiredIndex (itemID, preferredIndex, true);

		if (index != -1)
		{

		}

		else
		{
			reply = false;
		}

		// This is the only way I've found to check if the message is blank, an alternative method would be preferable
		if (message.sender == (new NetworkPlayer()))
		{
			RespondToRequest (reply);
		}

		else
		{
			networkView.RPC ("RespondToRequest", message.sender, reply);
		}
	}


	// Used to tell the client if they are allowed to take the item they requested or not
	[RPC] void RespondToRequest (bool response)
	{
		m_hadRequestResponse = true;
		m_itemRequestResponse = response;
	}


	
	/// Public functions
	// Used by the GUIManager to receive an item from the shop
	public void RequestNewInventory (float elapsedTime)
	{
		Debug.Log ("Recieved request to reset inventory!");

		// Obtain IDs
		int[] returnedIDs = m_lootTable.RequestItemByApproximateValue ((elapsedTime + 150f), m_stockFlags, m_rarityMods, m_shopCapacity);
		
		// Now turn the IDs back into objects
		for (int i = 0; i < returnedIDs.Length; i++)
		{
			m_shopInventory[i] = m_itemIDs.GetItemWithID (returnedIDs[i]);
			m_requestedItems[i] = false;
		}

		// Propagate our results to the clients
		for(int i = 0; i < m_shopInventory.Length; i++)
		{
			networkView.RPC ("PropagateItemInSlot", RPCMode.Others, returnedIDs[i], i);
		}
	}


	// Allows clients to request an item to purchase from the server
	public void RequestItemFromServer (int itemID, int preferredIndex = -1)
	{
		// Have client do the legwork to reduce strain on the host
		int index = GetDesiredIndex (itemID, preferredIndex);

		// -1 is returned on failure
		if (index != -1)
		{
			// Silly Unity requires a workaround for the server
			if (Network.isServer)
			{
				RequestItem (itemID, index, new NetworkMessageInfo());
			}

			else
			{
				networkView.RPC ("RequestItem", RPCMode.Server, itemID, preferredIndex);
			}
		}

		// Pretend the server declined the request
		else
		{
			ResetResponse (true);
		}
	}


	public void RemoveItemFromShopInventory (int slot)
	{
		m_shopInventory[slot] = null;
		m_requestedItems[slot] = false;
		networkView.RPC ("PropagateInventoryRemoval", RPCMode.Others, slot);
	}



}
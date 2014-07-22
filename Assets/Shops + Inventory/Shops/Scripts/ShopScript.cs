﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;


[RequireComponent (typeof (NetworkInventory))]
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
	bool[] m_stockFlags = null;					// Simply contains each serialized stock flag
	
	float[] m_rarityMods = null;				// Contains the rarity mod values of each item type

	int m_inventoryAdminKey = 0;				// Used in overwriting requested items

	NetworkInventory m_shopInventory = null;	// A reference to the NetworkInventory which should contain the items generated by RequestNewInventory()

	/// External references
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


	public NetworkInventory GetShopInventory()
	{
		return m_shopInventory;
	}


	// Calculates the cost of a particular item taking into account the price multiplier of the shop
	public int GetItemCost (int itemIndex)
	{
		try
		{
			return (int) (m_shopInventory[itemIndex].m_cost * m_priceMultiplier);
		}

		catch (System.Exception error)
		{
			Debug.LogError ("Exception occurred in " + name + ".ShopScript: " + error.Message);
			return int.MaxValue;
		}
	}

	public int GetIDIfItemPresent(ItemScript item)
    {
        for(int i = 0; i < m_shopInventory.GetCount(); i++)
        {
            if(m_shopInventory[i] == item)
                return i;
        }

        Debug.LogWarning ("Couldn't find item: " + item);
        return -1;
    }
	
	/// Behaviour functions
	// Initialise arrays during load
	void Awake()
	{
		// NetworkInventory is guaranteed by RequireComponent()
		m_shopInventory = GetComponent<NetworkInventory>();

		// Compile stockFlags
		m_stockFlags = new bool[5] { m_canStockWeapons, m_canStockShields, m_canStockEngines, m_canStockPlating, m_canStockCWeapons };
		
		// Compile rarityMods
		m_rarityMods = new float[5] { m_weaponRarityMod, m_shieldRarityMod, m_engineRarityMod, m_platingRarityMod, m_cWeaponRarityMod };
	}


	// Initialise references to external scripts
	void Start() 
	{
		// Obtain the admin key before anything else does
		m_inventoryAdminKey = m_shopInventory.GetAdminKey();

		InitialiseItemIDs();
		InitialiseLootTable();
	}
	
	
	// Rotate the shop
	void FixedUpdate() 
	{
		RotateShop();
	}

	public bool RESETINVENTORY = false;
	void Update()
	{
		if (RESETINVENTORY)
		{
			RESETINVENTORY = false;
			RequestNewInventory (Time.timeSinceLevelLoad);
		}
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
	
	
	
	/// Core functionality
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
			
			if (!m_lootTable)
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


	// Takes the inputted ID numbers and calls GetItemWithID on each, returns a list of ItemScripts
	List<ItemScript> ConvertToItemScripts (int[] idNumbers)
	{
		// Initialise a blank list
		List<ItemScript> scripts = new List<ItemScript>(idNumbers.Length);

		// Avoid the creation of temporary variables
		GameObject currentItem;
		ItemScript currentScript;

		// Iterate through the array obtaining valid ItemScripts
		for (int i = 0; i < idNumbers.Length; ++i)
		{
			// Assign and check if currentItem is a valid pointer
			if (currentItem = m_itemIDs.GetItemWithID (idNumbers[i]))
			{
				// Assign and check if currentScript is a valid pointer
				if (currentScript = currentItem.GetComponent<ItemScript>())
				{
					scripts.Add (currentScript);
				}

				else
				{
					Debug.LogError ("Couldn't find an ItemScript component on: " + currentItem.name);
				}
			}
		}

		return scripts;
	}

	// Performs all the necessary work to add an item to the server
	void RefillInventory (List<ItemScript> items)
	{
		if (Network.isServer)
		{
			if (items.Count != m_shopInventory.GetCapacity())
			{
				Debug.LogError ("The number of generated items (" + items.Count + ") in " + name + ".ShopScript does not match the NetworkInventory capacity (" + m_shopInventory.GetCapacity() + ").");
			}

			for (int i = 0; i < items.Count; ++i)
			{
				// We know that server requests have no latency so don't bother waiting for a response.
				m_shopInventory.RequestServerAdd (items[i], i, m_inventoryAdminKey);

				if (m_shopInventory.HasServerResponded())
				{
					m_shopInventory.AddItemToServer (m_shopInventory.GetItemAddResponse());
				}

				else
				{
					Debug.LogError ("Somehow the server hasn't responded in " + name + ".ShopScript.RefillInventory()");
				}
			}
		}
	}

	
	/// Public functions
	// Used by the GUIManager to receive an item from the shop
	public void RequestNewInventory (float elapsedTime)
	{
		if (Network.isServer)
		{
			//Debug.Log ("Recieved request to reset inventory! Elapsed time: " + elapsedTime);
			
			// Obtain IDs
			//int[] returnedIDs = m_lootTable.RequestItemByApproximateValue ((elapsedTime + 150f), m_stockFlags, m_rarityMods, m_shopInventory.GetCapacity());
            int[] returnedIDs = m_lootTable.RequestItemListByTime(elapsedTime, m_stockFlags, m_shopInventory.GetCapacity());
			List<ItemScript> items = ConvertToItemScripts (returnedIDs);

			// Clear the inventory for convenience
			m_shopInventory.AdminResetInventory (m_inventoryAdminKey);

			// Finally refill it
			RefillInventory (items);
		}
	}
}
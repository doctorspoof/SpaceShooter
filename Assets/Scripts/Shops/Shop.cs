﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;



/// <summary>
///  Use ShopType to indicate whether the shop is a Shipyard or a standard shop.
/// </summary>
public enum ShopType
{
    Basic = 0,
    Shipyard = 1
}



/// <summary>
/// Shop defines all the functionality required by a shop. It contains information about the functionality provided by the shop, such as whether it
/// allows for the equipping of items, how much it costs to purchase an item and what the shop can actually stock.
/// </summary>
[RequireComponent (typeof (NetworkInventory))]
public sealed class Shop : MonoBehaviour
{
    #region Unity modifiable variables

    [SerializeField]                    ShopType m_shopType = ShopType.Basic;               //!< The ShopType determines the functionality available.

    [SerializeField]                    float m_shopRotationSpeed = 5f;			            //!< How quickly the shop should rotate on the Z axis.
    [SerializeField, Range(0f, 10f)]    float m_priceMultiplier = 1f;	                    //!< How much the item cost should be scaled by the shop.
    
    [SerializeField]                    Transform m_dockPoint = null;                       //!< Where on the ship the player docks to access the shop.
    
    [SerializeField]                    List<Element> m_canStock = new List<Element>(0);    //!< What augments can the shop actually stock?

    [SerializeField]                    bool DebugResetInventory = false;                   //!< TO DELETE!

    #endregion Unity modifiable variables


    #region Internal data

    int m_inventoryAdminKey = 0;				//!< Used in overwriting requested items.

    NetworkInventory m_shopInventory = null;	//!< A reference to the NetworkInventory which should contain the items generated by RequestNewInventory().

    // External references
    ItemIDHolder m_itemIDs = null;			    //!< Used for caching the ItemIDHolder.
    LootTableScript m_lootTable = null;		    //!< Used for caching the LootTableScript.

    #endregion


    #region Getters & setters

    public ShopType GetShopType()
    {
        return m_shopType;
    }


    public Vector3 GetDockPoint()
    {
        return m_dockPoint.position;
    }


    public NetworkInventory GetShopInventory()
    {
        return m_shopInventory;
    }


    /// <summary>
    /// Calculates the cost of a particular item taking into account the price multiplier of the shop.
    /// </summary>
    /// <returns>The total cost.</returns>
    /// <param name="itemIndex">The index of the item to obtain the price of.</param>
    public int GetItemCost (int itemIndex)
    {
        try
        {
            return (int) (m_shopInventory[itemIndex].GetBaseCost() * m_priceMultiplier);
        }

        catch (System.Exception error)
        {
            Debug.LogError ("Exception occurred in " + name + ".Shop: " + error.Message);
            return int.MaxValue;
        }
    }


    /// <summary>
    /// Iterates through the shops inventory and attempts to find the index of the ItemWrapper passed to the function.
    /// </summary>
    /// <returns>The index of the item, -1 if not found.</returns>
    /// <param name="item">The item to look for.</param>
    public int GetIndexOf (ItemWrapper item)
    {
        for (int i = 0; i < m_shopInventory.GetCount(); i++)
        {
            if (m_shopInventory[i] == item)
            {
                return i;
            }
        }

        Debug.LogWarning ("Couldn't find item: " + item + " in " + name + ".Shop.m_shopInventory");
        return -1;
    }

    #endregion Getters & Setters


    #region Behaviour functions

    /// <summary>
    /// Obtains a reference to the GameObjects NetworkInventory and ensures there are no duplicates in the Augment array.
    /// </summary>
    void Awake()
    {
        // NetworkInventory is guaranteed by RequireComponent()
        m_shopInventory = GetComponent<NetworkInventory>();

        CleanStockFlags();

        if (m_dockPoint == null)
        {
            m_dockPoint = transform;
        }
    }


    /// <summary>
    /// Initialise references to external scripts.
    /// </summary>
    void Start()
    {
        // Obtain the admin key before anything else does
        m_inventoryAdminKey = m_shopInventory.GetAdminKey();

        InitialiseItemIDs();
        InitialiseLootTable();
    }


    /// <summary>
    /// Rotate the shop.
    /// </summary>
    void FixedUpdate()
    {
        RotateShop();
    }


    /// <summary>
    /// DEBUG: DELETE!
    /// </summary>
    void Update()
    {
        if (DebugResetInventory)
        {
            DebugResetInventory = false;
            RequestNewInventory (Time.timeSinceLevelLoad);
        }
    }

    void OnDestroy()
    {
        Debug.Log ("Shop was told to destroy.");
    }

    /// <summary>
    /// Update the rotation over the network.
    /// </summary>
    /// <param name="stream">Stream.</param>
    /// <param name="info">Info.</param>
    void OnSerializeNetworkView (BitStream stream, NetworkMessageInfo info)
    {
        float rotZ = transform.rotation.eulerAngles.z;

        stream.Serialize (ref rotZ);

        if (!stream.isWriting)
        {
            transform.rotation = Quaternion.Euler (0f, 0f, rotZ);
        }
    }

    #endregion Behaviour functions


    #region Setup

    /// <summary>
    /// Rids the stock flags of duplicate elements to ease the generation of items.
    /// </summary>
    void CleanStockFlags()
    {
        if (m_canStock.Count != 0)
        {
            // Create a clean list
            List<Element> cleanList = new List<Element>(0);

            foreach (Element element in m_canStock)
            {
                if (!cleanList.Contains (element))
                {
                    cleanList.Add (element);
                }
            }

            m_canStock = cleanList;
        }
    }


    /// <summary>
    /// Obtain a reference to the ItemIDHolder script.
    /// </summary>
    void InitialiseItemIDs()
    {
        // Find Item Manager
        GameObject itemManager = GameObject.FindGameObjectWithTag ("ItemManager");

        if (itemManager != null)
        {
            m_itemIDs = itemManager.GetComponent<ItemIDHolder>();

            if (m_itemIDs == null)
            {
                Debug.LogError ("ItemManager object does not contain an ItemIDHolder component.");
            }
        }

        else
        {
            Debug.LogError ("Unable to find object with tag: ItemManager");
        }
    }


    /// <summary>
    /// Obtain a reference to the LootTableScript component.
    /// </summary>
    void InitialiseLootTable()
    {
        // Find Loot Table
        GameObject lootTable = GameObject.FindGameObjectWithTag ("LootTable");

        if (lootTable != null)
        {
            m_lootTable = lootTable.GetComponent<LootTableScript>();

            if (m_lootTable == null)
            {
                Debug.LogError ("LootTable object does not contain a LootTableScript component.");
            }
        }
        else
        {
            Debug.LogError ("Unable to find object with tag: LootTable");
        }
    }

    #endregion Setup


    #region Shop functionality

    /// <summary>
    /// Keeps the shop rotating on the z axis in the desired fashion.
    /// </summary>
    void RotateShop()
    {
        transform.rotation = Quaternion.Euler (new Vector3 (0f, 0f, 
                                                            transform.rotation.eulerAngles.z + (m_shopRotationSpeed * Time.deltaTime)));
    }


    /// <summary>
    /// Requests that the shop generate a completely new inventory.
    /// </summary>
    /// <param name="elapsedTime">The time value to use in the generation of items (effects rarity).</param>
    public void RequestNewInventory (float elapsedTime)
    {
        if (Network.isServer)
        {
            // Obtain IDs
            List<ItemWrapper> items = m_lootTable.RequestItemListByTime (elapsedTime, m_canStock, m_shopInventory.GetCapacity());

            // Clear the inventory for convenience
            m_shopInventory.AdminResetInventory (m_inventoryAdminKey);
            
            // Finally refill it
            RefillInventory (items);
        }
    }


    /// <summary>
    /// Takes the inputted ID numbers and calls GetItemWithID on each, returns a List<ItemWrapper>.
    /// </summary>
    /// <returns>The converted ItemWrappers.</returns>
    /// <param name="idNumbers">The ItemWrapper.m_itemID numbers to convert.</param>
    List<ItemWrapper> ConvertToItemWrapper (int[] idNumbers)
    {
        // Initialise a blank list
        List<ItemWrapper> scripts = new List<ItemWrapper> (idNumbers.Length);

        // Avoid the creation of temporary variables
        ItemWrapper currentScript;

        // Iterate through the array obtaining valid ItemScripts
        for (int i = 0; i < idNumbers.Length; ++i)
        {
            // Assign and check if currentScript is a valid pointer
            if ((currentScript = m_itemIDs.GetItemWithID (idNumbers[i])) != null)
            {
                scripts.Add(currentScript);
            }

            else
            {
                Debug.LogError("Couldn't find an ItemScript component when converting to ItemScript objects");
            }

        }

        return scripts;
    }


    /// <summary>
    /// Fills the NetworkInventory with items passed to the function.
    /// </summary>
    /// <param name="items">An ItemWrapper list which is used to fill the inventory.</param>
    void RefillInventory (List<ItemWrapper> items)
    {
        if (Network.isServer)
        {
            if (items.Count != m_shopInventory.GetCapacity())
            {
                Debug.LogError ("The number of generated items (" + items.Count + ") in " +
                                name + ".Shop does not match the NetworkInventory capacity (" + m_shopInventory.GetCapacity() + ").");
            }

            for (int i = 0; i < items.Count; ++i)
            {
                if (items[i] != null)
                {
                    // We know that server requests have no latency so don't bother waiting for a response.
                    m_shopInventory.RequestServerAdd (items[i], i, m_inventoryAdminKey);
                    
                    if (m_shopInventory.HasServerResponded())
                    {
                        m_shopInventory.AddItemToServer (m_shopInventory.GetItemAddResponse());
                    }
                    
                    else
                    {
                        Debug.LogError ("Somehow the server hasn't responded in " + name + ".Shop.RefillInventory()");
                    }
                }
            }
        }
    }

    #endregion Shop functionality
}
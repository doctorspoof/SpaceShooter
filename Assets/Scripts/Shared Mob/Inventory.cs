using UnityEngine;
using System.Collections;

public class Inventory : MonoBehaviour 
{
    #region Serializable Vars
    [SerializeField, Range(0, 50)]      int             m_maxInventorySize = 5;
    [SerializeField]                    int             m_maxHeldMass = 100;
    [SerializeField]                    int             m_maxHeldFuel = 100;
    [SerializeField]                    int             m_maxHeldWater = 100;
    [SerializeField]                    int             m_maxHeldPopulation = 100;
    [SerializeField]                    int             m_cash = 100;
    #endregion
    
    #region Private Vars
    [SerializeField]                    ItemWrapper[]   m_inventory;
                                        int             m_currentHeldMass;
                                        int             m_currentHeldFuel;
                                        int             m_currentHeldWater;
                                        int             m_currentHeldPopulation;
    #endregion

    #region Getters/Setters
    // Inventory
    public ItemWrapper[] GetFullInventory()
    {
        return m_inventory;
    }
    public ItemWrapper GetInventoryAtIndex(int index)
    {
        if(index > m_maxInventorySize)
        {
            Debug.LogWarning("Attempting to access inventory on mob '" + this.gameObject.name + "' out of max inventory range.");
            return null;
        }
        
        return m_inventory[index];
    }
    public bool IsItemInInventorySlot(int index)
    {
        return (m_inventory[index] != null);
    }
    public bool IsInventoryFull()
    {
        for(int i = 0; i < m_inventory.Length; i++)
        {
            if(m_inventory[i] == null)
                return false;
        }
        
        Debug.Log ("Inventory is full.");
        return true;
    }
    public bool SetItemIntoInventoryAtSlot(int index, ItemWrapper item)
    {
        if(!IsItemInInventorySlot(index))
        {
            Debug.LogWarning ("Inventory slot " + index + " is already filled!");
            return false;
        }
        
        m_inventory[index] = item;
        return true;
    }
    public bool SetItemIntoInventory(ItemWrapper item)
    {
        for(int i = 0; i < m_inventory.Length; i++)
        {
            if(m_inventory[i] == null)
            {
                m_inventory[i] = item;
                Debug.Log ("Successfully placed object at index " + i);
                return true;
            }
        }
        
        Debug.LogError ("Inventory was full! Cannot place item.");
        return false;
    }
    public bool RemoveItemFromInventory(ItemWrapper item)
    {
        for(int i = 0; i < m_inventory.Length; i++)
        {
            if(m_inventory[i] == item)
            {
                m_inventory[i] = null;
                Debug.Log ("Removed item " + item.GetItemName());
                return true;
            }
        }
        
        Debug.Log ("Couldn't find item " + item.GetItemName() + " in inventory.");
        return false;
    }
    
    // Resources
    public int GetCurrentMass()
    {
        return m_currentHeldMass;
    }
    public bool IsMassStorageFull()
    {
        return (m_currentHeldMass >= m_maxHeldMass);
    }
    public void SetCurrentMass(int newMass)
    {
        m_currentHeldMass = newMass;
        if(m_currentHeldMass > m_maxHeldMass)
            m_currentHeldMass = m_maxHeldMass;
        else if(m_currentHeldMass < 0)
            m_currentHeldMass = 0;
    }
    
    public int GetCurrentFuel()
    {
        return m_currentHeldFuel;
    }
    public bool IsFuelStorageFull()
    {
        return (m_currentHeldFuel >= m_maxHeldFuel);
    }
    public void SetCurrentFuel(int newFuel)
    {
        m_currentHeldFuel = newFuel;
        if(m_currentHeldFuel > m_maxHeldFuel)
            m_currentHeldFuel = m_maxHeldFuel;
        else if(m_currentHeldFuel < 0)
            m_currentHeldFuel = 0;
    }
    
    public int GetCurrentWater()
    {
        return m_currentHeldWater;
    }
    public bool IsWaterStorageFull()
    {
        return (m_currentHeldWater >= m_maxHeldWater);
    }
    public void SetCurrentWater(int newWater)
    {
        m_currentHeldWater = newWater;
        if(m_currentHeldWater > m_maxHeldWater)
            m_currentHeldWater = m_maxHeldWater;
        else if(m_currentHeldWater < 0)
            m_currentHeldWater = 0;
    }
    
    public int GetCurrentPopulation()
    {
        return m_currentHeldPopulation;
    }
    public bool IsPopulationStorageFull()
    {
        return (m_currentHeldPopulation >= m_maxHeldPopulation);
    }
    public void SetCurrentPopulation(int newPopulation)
    {
        m_currentHeldPopulation = newPopulation;
        if(m_currentHeldPopulation > m_maxHeldPopulation)
            m_currentHeldPopulation = m_maxHeldPopulation;
        else if(m_currentHeldPopulation < 0)
            m_currentHeldPopulation = 0;
    }
    
    public int GetCurrentCash()
    {
        return m_cash;
    }
    public bool CanAffordAmount(int amount)
    {
        return (m_cash >= amount);
    }
    public void SetCash(int newCash)
    {
        m_cash = newCash;
        networkView.RPC("PropagateCashAmount", RPCMode.Others, newCash);
    }
    [RPC] void PropagateCashAmount(int amount)
    {
        m_cash = amount;
    }
    #endregion
    
    #region Unity Functions
    void Start ()
    {
        //TODO: Add this back in
        //m_inventory = new ItemWrapper[m_maxInventorySize];
    }
    #endregion
}

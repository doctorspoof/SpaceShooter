using UnityEngine;
using System.Collections;

public class ItemPickup : MonoBehaviour 
{
    ItemWrapper m_itemToBePickedUp;
    
    #region Get/Set
    public void SetItem(ItemWrapper item)
    {
        m_itemToBePickedUp = item;
        this.renderer.material = item.GetItemMaterial();
        
        int itemID = item.GetItemID();
        if(Network.isServer)
            networkView.RPC ("PropagateItem", RPCMode.Others, itemID);
    }
    #endregion
    
    void OnTriggerEnter(Collider other)
    {
        if(Network.isServer && other.gameObject.tag == "Player")
        {
            if(other.gameObject.GetComponent<Inventory>().SetItemIntoInventory(m_itemToBePickedUp))
            {
                Network.Destroy(this.gameObject);
            }
        }
    }
    
    [RPC] void PropagateItem(int id)
    {
        ItemWrapper item = GameObject.FindGameObjectWithTag("ItemManager").GetComponent<ItemIDHolder>().GetItemWithID(id);
        SetItem(item);
    }
}

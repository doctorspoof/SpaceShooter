using UnityEngine;
using System.Collections;

public static class CraftingLookup
{
    public static ItemWrapper QueryCraftingResult(ItemWrapper left, ItemWrapper right)
    {if(left != null && right != null)
        {
            if(left.GetAugmentElementID() == right.GetAugmentElementID())
            {
                if(left.GetAugmentTierID() == right.GetAugmentTierID() && left.GetAugmentTierID() < 5)
                {
                    //Get the object from item manager who's ID is 10 greater than the item passed (ie, same element, next tier)
                    ItemIDHolder holder = GameObject.FindGameObjectWithTag("ItemManager").GetComponent<ItemIDHolder>();
                    ItemWrapper newItem = holder.GetItemWithID(left.GetItemID() + 10);
                    return newItem;
                }
            }
        }
        
        return null;
    }
}

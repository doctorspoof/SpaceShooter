using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GUIShopDockScreen : BaseGUIScreen 
{
    /* Serializable Members */
    [SerializeField]        Texture m_menuBackground;
    [SerializeField]        Texture m_shopBaseTexture;
    [SerializeField]        Texture m_smallShopTexture;
    [SerializeField]        Texture m_dockInventoryBorder;
    [SerializeField]        Texture m_dockPlayerImage;
    
    /* Internal Members */
    Vector2                 m_playerScrollPosition                  = Vector2.zero;
    Vector2                 m_shopScrollPosition                    = Vector2.zero;
    bool                    m_isRequestingItem                      = false;
    bool                    m_transferFailed                        = false;
    string                  m_transferFailedMessage                 = "";
    
    // Dragged Item Members
    ItemWrapper              m_currentDraggedItem                    = null;
    int                     m_currentDraggedItemInventoryId         = -1;
    bool                    m_currentDraggedItemIsFromPlayerInv     = false;
    ItemTicket              m_currentTicket                         = null;
    bool                    m_shopConfirmBuy                        = false;
    ItemWrapper              m_confirmBuyItem                        = null;
    
    // Drawn Rect Dictionaries
    Dictionary<Rect, ItemWrapper> m_drawnItems = new Dictionary<Rect, ItemWrapper>();
    Dictionary<Rect, ItemWrapper> m_drawnItemsSecondary = new Dictionary<Rect, ItemWrapper>();
    
    // Cached Members
    GameStateController     m_gscCache                              = null;
    PlayerControlScript     m_playerCache                           = null;
    Shop              m_shopCache                             = null;
    
    #region Setters
    public void SetShopCache(GameObject shop)
    {
        m_shopCache = shop.GetComponent<Shop>();
    }
    public void SetPlayerReference(GameObject ship)
    {
        m_playerCache = ship.GetComponent<PlayerControlScript>();
    }
    #endregion
    
    /* Unity Functions */
    void Start () 
    {
        m_priorityValue = 3;
        m_gscCache = GameStateController.Instance();
    }
    
    /* Custom Functions */
    #region Draw Functions
    public override void ManualGUICall (bool shouldRecieveInput)
    {
        if (m_shopCache != null)
        {
            //Grab useful things
            Event currentEvent = Event.current;
            Vector3 mousePos = currentEvent.mousePosition;
            m_drawnItems.Clear();
            m_drawnItemsSecondary.Clear();
            
            //Undertex
            GUI.DrawTexture(new Rect(396, 86, 807, 727), m_shopBaseTexture);
            
            if(m_transferFailed)
            {
                GUI.Label(new Rect(600, 180, 400, 50), m_transferFailedMessage, "No Box");
            }
            
            //Player - left
            GUI.Label(new Rect(816, 270, 164, 40), "Player:", "No Box");
            ItemWrapper[] playerInv = m_playerCache.GetComponent<Inventory>().GetFullInventory();
            Rect scrollAreaRectPl = new Rect(816, 330, 180, 320);
            m_playerScrollPosition = GUI.BeginScrollView(new Rect(816, 330, 180, 320), m_playerScrollPosition, new Rect(0, 0, 150, 52 * playerInv.Length));
            for (int i = 0; i < playerInv.Length; i++)
            {
                GUI.Label(new Rect(0, 5 + (i * 50), 50, 50), playerInv[i].GetComponent<ItemWrapper>().GetIcon());
                Rect lastR = new Rect(60, 10 + (i * 50), 114, 40);
                GUI.Label(lastR, playerInv[i].GetComponent<ItemWrapper>().GetItemName(), "Small No Box");
                Rect modR = new Rect(lastR.x + scrollAreaRectPl.x, lastR.y + scrollAreaRectPl.y - m_playerScrollPosition.y, lastR.width, lastR.height);
                Rect finalRect = new Rect(modR.x - 50, modR.y, modR.width + 50, modR.height);
                
                if (scrollAreaRectPl.Contains(new Vector2(modR.x, modR.y)) && scrollAreaRectPl.Contains(new Vector2(modR.x + modR.width, modR.y + modR.height)))
                    m_drawnItemsSecondary.Add(finalRect, playerInv[i].GetComponent<ItemWrapper>());
                
                if (shouldRecieveInput && currentEvent.type == EventType.MouseDown && m_shopCache.GetShopType() == ShopType.Shipyard)
                {
                    bool insideFinalRect = finalRect.Contains(mousePos);
                    if (!m_shopConfirmBuy && insideFinalRect && !m_isRequestingItem)
                    {
                        //Begin drag & drop
                        m_currentDraggedItem = playerInv[i].GetComponent<ItemWrapper>();
                        m_currentDraggedItemInventoryId = i;
                        m_currentDraggedItemIsFromPlayerInv = true;
                    }
                }
            }
            GUI.EndScrollView();
            
            //Shop - right
            GUI.Label(new Rect(1020, 270, 164, 40), "Shop:", "No Box");
            NetworkInventory shopInv = m_shopCache.GetComponent<NetworkInventory>();
            Rect scrollAreaRect = new Rect(1020, 330, 180, 320);
            m_shopScrollPosition = GUI.BeginScrollView(scrollAreaRect, m_shopScrollPosition, new Rect(0, 0, 150, 52 * shopInv.GetCount()));
            for (int i = 0; i < shopInv.GetCount(); i++)
            {
                if(shopInv[i] != null)
                {
                    GUI.Label(new Rect(0, 5 + (i * 50), 50, 50), shopInv[i].GetIcon());
                    Rect lastR = new Rect(60, 10 + (i * 50), 114, 40);
                    GUI.Label(lastR, shopInv[i].GetComponent<ItemWrapper>().GetItemName(), "Small No Box");
                    Rect modR = new Rect(lastR.x + scrollAreaRect.x, lastR.y + scrollAreaRect.y - m_shopScrollPosition.y, lastR.width, lastR.height);
                    Rect finalRect = new Rect(modR.x - 50, modR.y, modR.width + 50, modR.height);
                    
                    if (scrollAreaRect.Contains(new Vector2(modR.x, modR.y)) && scrollAreaRect.Contains(new Vector2(modR.x + modR.width, modR.y + modR.height)))
                        m_drawnItems.Add(finalRect, shopInv[i]);
                    
                    if (shouldRecieveInput && currentEvent.type == EventType.MouseDown)
                    {
                        bool insideFinalRect = finalRect.Contains(mousePos);
                        if (!m_shopConfirmBuy && insideFinalRect && !m_isRequestingItem)
                        {
                            if(m_playerCache.GetComponent<Inventory>().CanAffordAmount(m_shopCache.GetItemCost(i)))
                            {
                                //Since we're a shop, on mouseDown, open the item confirmation box
                                shopInv.RequestServerCancel(m_currentTicket);
                                shopInv.RequestServerItem(shopInv[i].GetItemID(), i);
                                StartCoroutine(AwaitTicketRequestResponse(shopInv, RequestType.ItemTake, ItemOwner.NetworkInventory, ItemOwner.PlayerInventory, true));
                            }
                            else
                                StartCoroutine(CountdownTransferFailedPopup(false));
                        }
                    }
                }
            }
            GUI.EndScrollView();
            
            Rect weaponTemp = new Rect(605, 301, 63, 63);
            Rect shieldTemp = new Rect(605, 367, 63, 63);
            Rect platingTemp = new Rect(605, 442, 63, 63);
            Rect engineTemp = new Rect(605, 588, 63, 63);
            
            //Do shop type specific stuff
            if(m_shopCache.GetShopType() == ShopType.Basic)
            {
                GUI.DrawTexture(new Rect(396, 221, 403, 460), m_smallShopTexture);
                int hpPercent = (int)(m_playerCache.GetComponent<HealthScript>().GetHPPercentage() * 100.0f);
                GUI.Label (new Rect(695, 440, 90, 40), hpPercent.ToString() + "%", "No Box");
            }
            else
            {
                //float playerSourceWidth = m_playerPanelXWidth / 408.0f;
                GUI.DrawTexture(new Rect(396, 221, 403, 460), m_dockInventoryBorder);
                GUI.DrawTexture(new Rect(396, 221, 403, 460), m_dockPlayerImage);
                
                //Equipped icons:
                float iconLeftX = 602.0f;
                if (iconLeftX >= 324.0f)
                {
                    
                    /*m_drawnItemsSecondary.Add(weaponTemp, m_playerCache.GetEquipedWeaponItem().GetComponent<ItemWrapper>());
                    m_drawnItemsSecondary.Add(shieldTemp, m_playerCache.GetEquipedShieldItem().GetComponent<ItemWrapper>());
                    m_drawnItemsSecondary.Add(platingTemp, m_playerCache.GetEquipedPlatingItem().GetComponent<ItemWrapper>());
                    m_drawnItemsSecondary.Add(engineTemp, m_playerCache.GetEquipedEngineItem().GetComponent<ItemWrapper>());
                    
                    GUI.DrawTexture(weaponTemp, m_playerCache.GetEquipedWeaponItem().GetComponent<ItemWrapper>().GetIcon());
                    GUI.DrawTexture(shieldTemp, m_playerCache.GetEquipedShieldItem().GetComponent<ItemWrapper>().GetIcon());
                    GUI.DrawTexture(platingTemp, m_playerCache.GetEquipedPlatingItem().GetComponent<ItemWrapper>().GetIcon());
                    GUI.DrawTexture(engineTemp, m_playerCache.GetEquipedEngineItem().GetComponent<ItemWrapper>().GetIcon());*/
                }
            }
            
            //Hover text
            if (shouldRecieveInput && m_currentDraggedItem == null)
            {
                foreach (Rect key in m_drawnItems.Keys)
                {
                    if (key.Contains(mousePos))
                    {
                        //TODO: Find out what pete renamed this function to
                        //int id = m_shopCache.GetIDIfItemPresent(m_drawnItems[key]);
                        //string text = m_drawnItems[key].GetHoverText(m_shopCache.GetItemCost(id));
                        //DrawHoverText(text, mousePos);
                    }
                }
                
                foreach (Rect key in m_drawnItemsSecondary.Keys)
                {
                    if (key.Contains(mousePos))
                    {
                        string text = m_drawnItemsSecondary[key].GetHoverText();
                        DrawHoverText(text, mousePos);  
                    }
                }
            }
            else if(shouldRecieveInput)
            {
                GUI.Label(new Rect(mousePos.x - 20, mousePos.y - 20, 40, 40), m_currentDraggedItem.GetComponent<ItemWrapper>().GetIcon());
                
                if(!Input.GetMouseButton(0))
                {
                    //Drop item whereever you are
                    if(weaponTemp.Contains(mousePos))
                    {
                        /*if(m_currentDraggedItem.GetItemType() == ItemType.Weapon)
                        {
                            //m_playerCache.EquipItemInSlot(m_currentDraggedItemInventoryId);
                            m_currentDraggedItem = null;
                            m_currentDraggedItemInventoryId = -1;
                            m_currentDraggedItemIsFromPlayerInv = false;
                        }
                        else
                        {
                            m_currentDraggedItem = null;
                            m_currentDraggedItemInventoryId = -1;
                            m_currentDraggedItemIsFromPlayerInv = false;  
                        }*/
                    }
                    else if(shieldTemp.Contains(mousePos))
                    {
                        /*if(m_currentDraggedItem.GetItemType() == ItemType.Shield)
                        {
                            //m_playerCache.EquipItemInSlot(m_currentDraggedItemInventoryId);
                            m_currentDraggedItem = null;
                            m_currentDraggedItemInventoryId = -1;
                            m_currentDraggedItemIsFromPlayerInv = false;
                        }
                        else
                        {
                            m_currentDraggedItem = null;
                            m_currentDraggedItemInventoryId = -1;
                            m_currentDraggedItemIsFromPlayerInv = false;  
                        }*/
                    }
                    else if(platingTemp.Contains(mousePos))
                    {
                        /*if(m_currentDraggedItem.GetItemType() == ItemType.Plating)
                        {
                            //m_playerCache.EquipItemInSlot(m_currentDraggedItemInventoryId);
                            m_currentDraggedItem = null;
                            m_currentDraggedItemInventoryId = -1;
                            m_currentDraggedItemIsFromPlayerInv = false;
                        }
                        else
                        {
                            m_currentDraggedItem = null;
                            m_currentDraggedItemInventoryId = -1;
                            m_currentDraggedItemIsFromPlayerInv = false;  
                        }*/
                    }
                    else if(engineTemp.Contains(mousePos))
                    {
                        /*if(m_currentDraggedItem.GetItemType() == ItemType.Engine)
                        {
                            //m_playerCache.EquipItemInSlot(m_currentDraggedItemInventoryId);
                            m_currentDraggedItem = null;
                            m_currentDraggedItemInventoryId = -1;
                            m_currentDraggedItemIsFromPlayerInv = false;
                        }
                        else
                        {
                            m_currentDraggedItem = null;
                            m_currentDraggedItemInventoryId = -1;
                            m_currentDraggedItemIsFromPlayerInv = false;  
                        }*/
                    }
                    else
                    {
                        m_currentDraggedItem = null;
                        m_currentDraggedItemInventoryId = -1;
                        m_currentDraggedItemIsFromPlayerInv = false;
                    }
                }
            }
            
            //Do confirm box if appropriate
            if(m_shopConfirmBuy)
            {
                GUI.DrawTexture(new Rect(632, 328, 337, 200), m_menuBackground);
                GUI.Label(new Rect(662, 350, 277, 50), "Buy '" + m_confirmBuyItem.GetItemName() + "' for $" + m_confirmBuyItem.GetBaseCost() + "?", "No Box");
                
                if(GUI.Button (new Rect(700, 440, 70, 40), "Confirm"))
                {
                    //int cost = m_shopDockedAt.GetComponent<Shop>().GetItemCost(m_currentTicket.itemIndex);
                    shopInv.RequestTicketValidityCheck(m_currentTicket);
                    Debug.Log ("Requested confirm buy of item, using ticket: " + m_currentTicket.ToString());
                    StartCoroutine(AwaitTicketRequestResponse(shopInv, RequestType.TicketValidity, ItemOwner.NetworkInventory, ItemOwner.PlayerInventory, true));
                }
                
                if(GUI.Button (new Rect(830, 440, 70, 40), "Cancel"))
                {
                    shopInv.RequestServerCancel(m_currentTicket);
                    m_shopConfirmBuy = false;
                    m_confirmBuyItem = null;
                }
            }
            
            //Finally, Leave button
            if (shouldRecieveInput && !m_shopConfirmBuy && GUI.Button(new Rect(512, 687, 176, 110), "", "label"))
            {
                m_gscCache.SwitchToInGame();
                
                //m_PlayerHasDockedAtShop = false;
                //m_shopDockedAt = null;
                //m_shipyardScreen = true;
                
                m_playerCache.gameObject.GetComponent<PlayerControlScript>().SetNearbyShop(null);
                m_playerCache.transform.parent = null;
                m_playerCache.gameObject.GetComponent<PlayerControlScript>().TellShipStartRecievingInput();
                m_playerCache.rigidbody.isKinematic = false;
                Screen.showCursor = false;
            }
        }
    }
    
    void DrawHoverText(string text, Vector2 mousePos)
    {
        float width = 200;
        float height = GUI.skin.GetStyle("Hover").CalcHeight(new GUIContent(text), 200);
        GUI.Label(new Rect(mousePos.x + 10, mousePos.y - 5, width, height), text, GUI.skin.GetStyle("Hover"));
    }
    #endregion
    
    #region TicketHandling
    IEnumerator AwaitTicketRequestResponse(NetworkInventory inventory, RequestType reqType, ItemOwner from, ItemOwner to = ItemOwner.NetworkInventory, bool fromShop = false, int equipmentSlot = -1)
    {
        m_isRequestingItem = true;
        
        while (!inventory.HasServerResponded())
        {
            Debug.Log("Server has not yet responded <color=yellow>:(</color>");
            yield return null;
        }
        
        switch (reqType)
        {
        case RequestType.ItemAdd:
        {
            m_currentTicket = inventory.GetItemAddResponse();
            int itemID = m_currentTicket.itemID;
            if (inventory.AddItemToServer(m_currentTicket))
            {    
                if (from == ItemOwner.PlayerInventory)
                {
                    //m_playerCache.RemoveItemFromInventory(GameObject.FindGameObjectWithTag("ItemManager").GetComponent<ItemIDHolder>().GetItemWithID(itemID));
                }
            }
            else
                StartCoroutine(CountdownTransferFailedPopup(true));           
            
            break;
        }
        case RequestType.ItemTake:
        {
            m_currentTicket = inventory.GetItemRequestResponse();
            if(m_currentTicket.IsValid())
            {
                if(fromShop)
                {
                    m_shopConfirmBuy = true;
                    m_confirmBuyItem = inventory[m_currentTicket.itemIndex];
                }
            }
            else
                StartCoroutine(CountdownTransferFailedPopup(true));
            
            break;
        }
        case RequestType.TicketValidity:
        {
            //Debug.Log("Is the ticket valid?");
            if (inventory.GetTicketValidityResponse())
            {
                //Debug.Log("Yes!");
                switch (from)
                {
                case ItemOwner.NetworkInventory:
                {
                    ItemWrapper item = GameObject.FindGameObjectWithTag ("ItemManager").GetComponent<ItemIDHolder>().GetItemWithID (m_currentTicket.itemID).GetComponent<ItemWrapper>();
                    switch (to)
                    {
                    case ItemOwner.PlayerInventory:
                    {
                        int costIfShop = fromShop ? inventory.GetComponent<Shop>().GetItemCost(m_currentTicket.itemIndex) : -1;
                        
                        if (inventory.RemoveItemFromServer (m_currentTicket))
                        {
                            //m_playerCache.AddItemToInventory (item);
                            
                            if(fromShop)
                            {
                                m_shopConfirmBuy = false;
                                m_confirmBuyItem = null;
                                Inventory playerInv = m_playerCache.GetComponent<Inventory>();
                                playerInv.SetCash(playerInv.GetCurrentCash() - costIfShop);
                            }
                        }
                        
                        else
                        {
                            Debug.LogError("<color=blue>Ticket mismatch!</color>");
                        }
                        
                        break;
                    }
                    case ItemOwner.PlayerEquipment:
                    {   
                        /*if (item.GetItemType() != ItemType.CapitalWeapon && inventory.RemoveItemFromServer (m_currentTicket))
                        {
                            //ItemWrapper oldEquipment = m_playerCache.GetEquipmentFromSlot (equipmentSlot);
                            
                            if (m_playerCache.GetComponent<Inventory>().IsInventoryFull())
                            {
                                //ItemWrapper lastItem = m_playerCache.GetPlayerInventory()[m_playerCache.GetPlayerInventory().Count - 1];
                                
                                // This little wonder makes me want to vomit and should only be used until the demo night
                                // Remove the last item from the players inventory then equip the new item from that
                                //m_playerCache.RemoveItemFromInventory (lastItem);
                                //m_playerCache.AddItemToInventory (item);
                                //m_playerCache.EquipItemInSlot (m_playerCache.GetPlayerInventory().Count - 1);
                                //m_playerCache.RemoveItemFromInventory (oldEquipment);
                                //m_playerCache.AddItemToInventory (lastItem);
                                
                                // Move the item to the CShip inventory since the players inventory is full
                                //inventory.RequestServerAdd (oldEquipment);
                                StartCoroutine (AwaitTicketRequestResponse (inventory, RequestType.ItemAdd, to, from));
                            }
                            else
                            {
                                //m_playerCache.AddItemToInventory (item);
                                //m_playerCache.EquipItemInSlot (m_playerCache.GetPlayerInventory().Count - 1);
                            }
                        }*/
                        break;
                    }
                        
                    default:
                        inventory.RequestServerCancel (m_currentTicket);
                        break;
                    }
                    break;
                }   
                default:
                    Debug.LogError ("THIS SHOULD NEVER HAPPEN!");
                    break;
                }
                
            }
            else
                StartCoroutine(CountdownTransferFailedPopup(true));
            break;
        }
        }
        
        m_isRequestingItem = false;
        
        if (!m_currentTicket.IsValid())
        {
            m_currentDraggedItem = null;
            m_currentDraggedItemInventoryId = -1;
            m_currentDraggedItemIsFromPlayerInv = false;
        }
    }
    #endregion
    
    #region Responses
    IEnumerator CountdownTransferFailedPopup(bool transfer)
    {
        if(transfer)
            m_transferFailedMessage = "Transfer failed - Item Requested Elsewhere";
        else
            m_transferFailedMessage = "Insufficient funds";
        m_transferFailed = true;
        float timer = 1.5f;
        
        while(timer > 0.0f)
        {
            timer -= Time.deltaTime;
            m_transferFailed = true;
            yield return 0;
        }
        
        m_transferFailed = false;
    }
    #endregion
}

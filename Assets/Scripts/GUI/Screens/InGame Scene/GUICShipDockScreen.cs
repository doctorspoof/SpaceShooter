using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/* * * TODO * * */
// Pass the active player object into the GUI
// Pass the CShip object into the GUI

public class GUICShipDockScreen : BaseGUIScreen 
{
    /* Serializable Members */
    [SerializeField]        Texture m_dockBackground;
    [SerializeField]        Texture m_dockInventoryBorder;
    [SerializeField]        Texture m_dockPlayerImage;
    [SerializeField]        Texture m_dockCShipImage;

    /* Internal Members */
    bool        m_isRequestingItem                      = false;
    bool        m_transferFailed                        = false;
    bool        m_previousMouseZero                     = false;
    bool        m_mouseZero                             = false;
    string      m_transferFailedMessage                 = "";
    CShipScreen m_currentCShipPanel                     = CShipScreen.DualPanel;
    Vector2     m_playerScrollPosition                  = Vector2.zero;
    Vector2     m_cShipScrollPosition                   = Vector2.zero;
    
    // Animatables
    int         m_playerPanelXWidth                     = 408;
    int         m_cShipPanelXPos                        = 796;
    
    // Dragged Item Members
    ItemScript  m_currentDraggedItem                    = null;
    int         m_currentDraggedItemInventoryId         = -1;
    bool        m_currentDraggedItemIsFromPlayerInv     = false;
    ItemTicket  m_currentTicket                         = null;
    bool        m_shopConfirmBuy                        = false;
    ItemScript  m_confirmBuyItem                        = null;
    
    // Fixed Rects
    Rect m_LeftPanelPlayerRect = new Rect(810, 315, 180, 335);
    Rect m_LeftPanelCShipRect = new Rect(1010, 315, 180, 335);
    Rect m_LeftPanelWeaponRect = new Rect(602, 315, 70, 60);
    Rect m_LeftPanelEngineRect = new Rect(602, 567, 70, 60);
    Rect m_LeftPanelShieldRect = new Rect(602, 375, 70, 60);
    Rect m_LeftPanelPlatingRect = new Rect(602, 440, 70, 60);
    Rect m_RightPanelPlayerRect = new Rect(407, 315, 180, 335);
    Rect m_RightPanelCShipRect = new Rect(607, 315, 180, 335);
    Rect m_RightPanelWeapon1Rect = new Rect(915, 318, 78, 58);
    Rect m_RightPanelWeapon2Rect = new Rect(915, 378, 78, 58);
    Rect m_RightPanelWeapon3Rect = new Rect(915, 439, 78, 58);
    Rect m_RightPanelWeapon4Rect = new Rect(915, 568, 78, 58);
    
    // Drawn Rect Dictionaries
    Dictionary<Rect, ItemScript> m_drawnItems = new Dictionary<Rect, ItemScript>();
    Dictionary<Rect, ItemScript> m_drawnItemsSecondary = new Dictionary<Rect, ItemScript>();
    
    /* Cached Members */
    CapitalShipScript   m_cshipCache;
    PlayerControlScript m_playerCache;
    GameStateController m_gscCache;

	/* Unity Functions */
	void Start () 
    {
	    m_priorityValue = 3;
        m_gscCache = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameStateController>();
	}
	
	/* Custom Functions */
    #region DrawFunctions
    public override void ManualGUICall (bool shouldRecieveInput)
    {
        Event currentEvent = Event.current;
        Vector3 mousePos = currentEvent.mousePosition;
        m_previousMouseZero = m_mouseZero;
        m_mouseZero = Input.GetMouseButton(0);
        
        GUI.DrawTexture(new Rect(396, 86, 807, 727), m_dockBackground);
        
        if(m_transferFailed)
        {
            GUI.Label(new Rect(600, 180, 400, 50), m_transferFailedMessage, "No Box");
        }
        
        //Show bank status
        GUI.Label(new Rect(1012, 140, 134, 40), "$" + m_cshipCache.GetBankedCash(), "No Box");
        
        //Desposit moneys
        if(shouldRecieveInput)
        {
            if (GUI.Button(new Rect(1038, 180, 84, 33), "", "label"))
            {
                int cashAmount = m_playerCache.GetCash();
                m_playerCache.RemoveCash(cashAmount);
                m_cshipCache.DepositCashToCShip(cashAmount);
            }
        }
        
        m_drawnItems.Clear();
        //Do screen specific stuff here:
        switch (m_currentCShipPanel)
        {
            case CShipScreen.DualPanel:
            {
                if(shouldRecieveInput)
                {
                    if (GUI.Button(new Rect(394, 250, m_playerPanelXWidth, 400), ""))
                    {
                        //If player is selected, CShip should animate away
                        StartCoroutine(AnimateCShipPanel(1204));
                        m_currentCShipPanel = CShipScreen.PanelsAnimating;
                    }
                    
                    if (GUI.Button(new Rect(m_cShipPanelXPos, 250, (1204 - m_cShipPanelXPos), 400), ""))
                    {
                        //If CShip is selected, player should animate away
                        StartCoroutine(AnimatePlayerPanel(0));
                        m_currentCShipPanel = CShipScreen.PanelsAnimating;
                    }
                }
                
                DrawLeftPanel();
                DrawRightPanel();
                
                break;
            }
            case CShipScreen.RightPanelActive:
            {
                DrawRightPanel();
                
                GUI.Label(new Rect(408, 270, 164, 40), "Player:", "No Box");
                List<GameObject> playerInv = m_playerCache.GetPlayerInventory();
                Rect scrollAreaRectPl = new Rect(408, 330, 180, 320);
                m_playerScrollPosition = GUI.BeginScrollView(scrollAreaRectPl, m_playerScrollPosition, new Rect(0, 0, 150, 52 * playerInv.Count));
                for (int i = 0; i < playerInv.Count; i++)
                {
                    GUI.Label(new Rect(0, 5 + (i * 50), 50, 50), playerInv[i].GetComponent<ItemScript>().GetIcon());
                    Rect lastR = new Rect(60, 10 + (i * 50), 114, 40);
                    GUI.Label(lastR, playerInv[i].GetComponent<ItemScript>().GetItemName(), "Small No Box");
                    Rect modR = new Rect(lastR.x + scrollAreaRectPl.x, lastR.y + scrollAreaRectPl.y - m_playerScrollPosition.y, lastR.width, lastR.height);
                    Rect finalRect = new Rect(modR.x - 50, modR.y, modR.width + 50, modR.height);
                    
                    if (scrollAreaRectPl.Contains(new Vector2(modR.x, modR.y)) && scrollAreaRectPl.Contains(new Vector2(modR.x + modR.width, modR.y + modR.height)))
                        m_drawnItems.Add(finalRect, playerInv[i].GetComponent<ItemScript>());
                    
                    if (shouldRecieveInput && currentEvent.type == EventType.MouseDown)
                    {
                        if (finalRect.Contains(mousePos) && !m_isRequestingItem)
                        {
                            //Begin drag & drop
                            m_currentDraggedItem = playerInv[i].GetComponent<ItemScript>();
                            m_currentDraggedItemInventoryId = i;
                            m_currentDraggedItemIsFromPlayerInv = true;
                        }
                    }
                }
                GUI.EndScrollView();
                
                GUI.Label(new Rect(612, 270, 164, 40), "Capital:", "No Box");
                NetworkInventory cshipInv = m_cshipCache.GetComponent<NetworkInventory>();
                Rect scrollAreaRect = new Rect(612, 330, 180, 320);
                m_cShipScrollPosition = GUI.BeginScrollView(scrollAreaRect, m_cShipScrollPosition, new Rect(0, 0, 150, 52 * cshipInv.GetCount()));
                for (int i = 0; i < cshipInv.GetCount(); i++)
                {
                    GUI.Label(new Rect(0, 5 + (i * 50), 50, 50), cshipInv[i].GetIcon());
                    Rect lastR = new Rect(60, 10 + (i * 50), 114, 40);
                    GUI.Label(lastR, cshipInv[i].GetItemName(), "Small No Box");
                    Rect modR = new Rect(lastR.x + scrollAreaRect.x, lastR.y + scrollAreaRect.y - m_cShipScrollPosition.y, lastR.width, lastR.height);
                    Rect finalRect = new Rect(modR.x - 50, modR.y, modR.width + 50, modR.height);
                    
                    if (scrollAreaRect.Contains(new Vector2(modR.x, modR.y)) && scrollAreaRect.Contains(new Vector2(modR.x + modR.width, modR.y + modR.height)))
                        m_drawnItems.Add(finalRect, cshipInv[i].GetComponent<ItemScript>());
                    
                    if (shouldRecieveInput && currentEvent.type == EventType.MouseDown)
                    {
                        //bool insideModR = modR.Contains(mousePos);
                        if (finalRect.Contains(mousePos) && !m_isRequestingItem)
                        {
                            //Begin drag & drop
                            m_currentDraggedItem = cshipInv[i];
                            m_currentDraggedItemInventoryId = i;
                            m_currentDraggedItemIsFromPlayerInv = false;
                            cshipInv.RequestServerCancel(m_currentTicket);
                            cshipInv.RequestServerItem(cshipInv[i].m_equipmentID, i);
                            StartCoroutine(AwaitTicketRequestResponse(cshipInv, RequestType.ItemTake, ItemOwner.NetworkInventory));
                        }
                    }
                }
                GUI.EndScrollView();
                
                DrawLeftPanel();
                
                //Handle mouse up if item is selected
                if (shouldRecieveInput && m_currentDraggedItem != null)
                {
                    if (IsMouseUpZero() && !m_isRequestingItem)
                    {
                        Debug.Log("Mouse button released, drop the item");
                        HandleItemDrop(false, mousePos);
                    }
                    
                    //If we still have an item selected by this point, draw it next to the cursor
                    if (m_currentDraggedItem != null)
                    {
                        GUI.Label(new Rect(mousePos.x - 20, mousePos.y - 20, 40, 40), m_currentDraggedItem.GetComponent<ItemScript>().GetIcon());
                    }
                }
                else if(shouldRecieveInput)
                {
                    GameObject[] turrets = m_cshipCache.GetAttachedTurrets();
                    if (m_RightPanelWeapon1Rect.Contains(mousePos))
                    {
                        string text = turrets[0].GetComponent<ItemScript>().GetShopText();
                        DrawHoverText(text, mousePos);
                    }
                    else if (m_RightPanelWeapon2Rect.Contains(mousePos))
                    {
                        string text = turrets[1].GetComponent<ItemScript>().GetShopText();
                        DrawHoverText(text, mousePos);
                    }
                    else if (m_RightPanelWeapon3Rect.Contains(mousePos))
                    {
                        string text = turrets[2].GetComponent<ItemScript>().GetShopText();
                        DrawHoverText(text, mousePos);
                    }
                    else if (m_RightPanelWeapon4Rect.Contains(mousePos))
                    {
                        string text = turrets[3].GetComponent<ItemScript>().GetShopText();
                        DrawHoverText(text, mousePos);
                    }
                }
                
                if (GUI.Button(new Rect(796, 250, 408, 400), "", "label") && shouldRecieveInput)
                {
                    StartCoroutine(AnimatePlayerPanel(408));
                    m_currentCShipPanel = CShipScreen.PanelsAnimating;
                }
                break;
            }
            case CShipScreen.LeftPanelActive:
            {
                DrawLeftPanel();
                
                GUI.Label(new Rect(816, 270, 164, 40), "Player:", "No Box");
                List<GameObject> playerInv = m_playerCache.GetPlayerInventory();
                Rect scrollAreaRectPl = new Rect(816, 330, 180, 320);
                m_playerScrollPosition = GUI.BeginScrollView(new Rect(816, 330, 180, 320), m_playerScrollPosition, new Rect(0, 0, 150, 52 * playerInv.Count));
                for (int i = 0; i < playerInv.Count; i++)
                {
                    GUI.Label(new Rect(0, 5 + (i * 50), 50, 50), playerInv[i].GetComponent<ItemScript>().GetIcon());
                    Rect lastR = new Rect(60, 10 + (i * 50), 114, 40);
                    GUI.Label(lastR, playerInv[i].GetComponent<ItemScript>().GetItemName(), "Small No Box");
                    Rect modR = new Rect(lastR.x + scrollAreaRectPl.x, lastR.y + scrollAreaRectPl.y - m_playerScrollPosition.y, lastR.width, lastR.height);
                    Rect finalRect = new Rect(modR.x - 50, modR.y, modR.width + 50, modR.height);
                    
                    if (scrollAreaRectPl.Contains(new Vector2(modR.x, modR.y)) && scrollAreaRectPl.Contains(new Vector2(modR.x + modR.width, modR.y + modR.height)))
                        m_drawnItems.Add(finalRect, playerInv[i].GetComponent<ItemScript>());
                    
                    if (shouldRecieveInput && currentEvent.type == EventType.MouseDown)
                    {
                        //bool insideModR = modR.Contains(mousePos);
                        if (finalRect.Contains(mousePos) && !m_isRequestingItem)
                        {
                            //Begin drag & drop
                            m_currentDraggedItem = playerInv[i].GetComponent<ItemScript>();
                            m_currentDraggedItemInventoryId = i;
                            m_currentDraggedItemIsFromPlayerInv = true;
                        }
                    }
                }
                GUI.EndScrollView();
                
                GUI.Label(new Rect(1020, 270, 164, 40), "Capital:", "No Box");
                NetworkInventory cshipInv = m_cshipCache.GetComponent<NetworkInventory>();
                Rect scrollAreaRect = new Rect(1020, 330, 180, 320);
                m_cShipScrollPosition = GUI.BeginScrollView(scrollAreaRect, m_cShipScrollPosition, new Rect(0, 0, 150, 52 * cshipInv.GetCount()));
                for (int i = 0; i < cshipInv.GetCount(); i++)
                {
                    GUI.Label(new Rect(0, 5 + (i * 50), 50, 50), cshipInv[i].GetComponent<ItemScript>().GetIcon());
                    Rect lastR = new Rect(60, 10 + (i * 50), 114, 40);
                    GUI.Label(lastR, cshipInv[i].GetComponent<ItemScript>().GetItemName(), "Small No Box");
                    Rect modR = new Rect(lastR.x + scrollAreaRect.x, lastR.y + scrollAreaRect.y - m_cShipScrollPosition.y, lastR.width, lastR.height);
                    Rect finalRect = new Rect(modR.x - 50, modR.y, modR.width + 50, modR.height);
                    
                    if (scrollAreaRect.Contains(new Vector2(modR.x, modR.y)) && scrollAreaRect.Contains(new Vector2(modR.x + modR.width, modR.y + modR.height)))
                        m_drawnItems.Add(finalRect, cshipInv[i].GetComponent<ItemScript>());
                    
                    if (shouldRecieveInput && currentEvent.type == EventType.MouseDown)
                    {
                        //bool insideModR = modR.Contains(mousePos);
                        if (finalRect.Contains(mousePos) && !m_isRequestingItem)
                        {
                            //Begin drag & drop
                            m_currentDraggedItem = cshipInv[i];
                            m_currentDraggedItemInventoryId = i;
                            m_currentDraggedItemIsFromPlayerInv = false;
                            cshipInv.RequestServerCancel(m_currentTicket);
                            cshipInv.RequestServerItem(cshipInv[i].m_equipmentID, i);
                            StartCoroutine(AwaitTicketRequestResponse(cshipInv, RequestType.ItemTake, ItemOwner.NetworkInventory));
                        }
                    }
                }
                GUI.EndScrollView();
                
                DrawRightPanel();
                
                //Handle mouse up if item is selected
            if (shouldRecieveInput && m_currentDraggedItem != null)
                {
                    if (IsMouseUpZero() && !m_isRequestingItem)
                    {
                        Debug.Log("Mouse button released, drop the item");
                        HandleItemDrop(true, mousePos);
                    }
                    
                    //If we still have an item selected by this point, draw it next to the cursor
                    if (m_currentDraggedItem != null)
                    {
                        GUI.Label(new Rect(mousePos.x - 20, mousePos.y - 20, 40, 40), m_currentDraggedItem.GetComponent<ItemScript>().GetIcon());
                    }
                    
                }
                else if(shouldRecieveInput)
                {
                    //Hovers
                    if (m_LeftPanelWeaponRect.Contains(mousePos))
                    {
                        string text = m_playerCache.GetEquipedWeaponItem().GetComponent<ItemScript>().GetShopText();
                        DrawHoverText(text, mousePos);
                    }
                    
                    if (m_LeftPanelShieldRect.Contains(mousePos))
                    {
                        string text = m_playerCache.GetEquipedShieldItem().GetComponent<ItemScript>().GetShopText();
                        DrawHoverText(text, mousePos);
                    }
                    
                    if (m_LeftPanelPlatingRect.Contains(mousePos))
                    {
                        string text = m_playerCache.GetEquipedPlatingItem().GetComponent<ItemScript>().GetShopText();
                        DrawHoverText(text, mousePos);
                    }
                    
                    if (m_LeftPanelEngineRect.Contains(mousePos))
                    {
                        string text = m_playerCache.GetEquipedEngineItem().GetComponent<ItemScript>().GetShopText();
                        DrawHoverText(text, mousePos);
                    }
                }
                
                if (GUI.Button(new Rect(394, 250, 408, 400), "", "label"))
                {
                    //Change back to dual panel
                    m_currentDraggedItem = null;
                    StartCoroutine(AnimateCShipPanel(796));
                    m_currentCShipPanel = CShipScreen.PanelsAnimating;
                }
                break;
            }
            case CShipScreen.PanelsAnimating:
            {
                //Wait for animating to complete;
                DrawLeftPanel();
                DrawRightPanel();
                break;
            }
        }
        
        //Hover text
        if (shouldRecieveInput && m_currentDraggedItem == null)
        {
            foreach (Rect key in m_drawnItems.Keys)
            {
                if (key.Contains(mousePos))
                {
                    string text = m_drawnItems[key].GetShopText();
                    DrawHoverText(text, mousePos);
                }
            }
        }
        
        //Respawn buttons:
        List<DeadPlayer> deadPlayers = m_gscCache.GetDeadPlayers();
        for (int i = 0; i < deadPlayers.Count; i++)
        {
            int fastSpawnCost = 500 + (int)(deadPlayers[i].m_deadTimer * 10);
            float buttonX = 811 + (i * 96);
            GUI.Label(new Rect(buttonX - 20, 690, 124, 33), deadPlayers[i].m_playerObject.m_name, "No Box");
            GUI.Label(new Rect(buttonX - 20, 722, 124, 33), "$" + fastSpawnCost, "No Box");
            
            if (shouldRecieveInput && GUI.Button(new Rect(buttonX, 765, 84, 33), ""))
            {
                //Check if amount is available, then respawn player as usual
                if (m_cshipCache.CShipCanAfford(fastSpawnCost))
                {
                    m_cshipCache.SpendBankedCash(fastSpawnCost);
                    RequestServerRespawnPlayer(deadPlayers[i].m_playerObject.m_netPlayer);
                }
                else
                    StartCoroutine(CountdownTransferFailedPopup(false));
            }
        }
        
        //Repair
        HealthScript m_thisPlayerHP = m_playerCache.GetComponent<HealthScript>();
        float damagePercent = 1.0f - m_thisPlayerHP.GetHPPercentage();
        int damage =m_thisPlayerHP.GetMaxHP() - m_thisPlayerHP.GetCurrHP();
        
        if(damagePercent > 0.0f)
        {
            int cost = Mathf.RoundToInt(damagePercent * 500.0f);
            int cash = m_thisPlayerHP.GetComponent<PlayerControlScript>().GetCash();
            if(m_thisPlayerHP.GetComponent<PlayerControlScript>().CheckCanAffordAmount(cost))
            {
                if(shouldRecieveInput)
                {
                    if(GUI.Button(new Rect(430, 130, 120, 83), "Fully repair ship for $" + cost, "Shared"))
                    {
                        m_thisPlayerHP.GetComponent<PlayerControlScript>().RemoveCash(cost);
                        m_thisPlayerHP.RepairHP(damage);
                    }
                }
                else
                {
                    GUI.Label (new Rect(430, 130, 120, 83), "Fully repair ship for $" + cost, "Shared");
                }
            }
            else if(cash != 0)
            {
                //Work out how much the player can afford
                float percentageAfford = cash / 500.0f;
                int percent = Mathf.RoundToInt(percentageAfford * 100.0f);
                float hpPerPercent = m_thisPlayerHP.GetMaxHP() / 100.0f;
                int cashToSpend = percent * 5;
                
                if(shouldRecieveInput)
                {
                    if(GUI.Button(new Rect(430, 130, 120, 83), "Repair " + percent + "% for $" + cashToSpend, "Shared"))
                    {
                        int damageHealed = (int)(percent * hpPerPercent);
                        m_thisPlayerHP.RepairHP(damageHealed);
                        m_thisPlayerHP.GetComponent<PlayerControlScript>().RemoveCash(cashToSpend);
                    }
                }
                else
                {
                    GUI.Label(new Rect(430, 130, 120, 83), "Repair " + percent + "% for $" + cashToSpend, "Shared");
                }
            }
            else
            {
                GUI.Label (new Rect(430, 130, 120, 83), "Repair -- No cash!", "Shared");
            }
        }
        else
        {
            GUI.Label (new Rect(430, 130, 120, 83), "Repair -- No damage!", "Shared");
        }
        
        //Leave button
        if (shouldRecieveInput && GUI.Button(new Rect(512, 687, 176, 110), "", "label"))
        {
            //This shouldn't be used anymore, instead GSC should be told when the player is docked or not docked, and that info passed back to the GUI
            //m_PlayerHasDockedAtCapital = false;
            Screen.showCursor = false;
            m_thisPlayerHP.gameObject.GetComponent<PlayerControlScript>().TellPlayerStopDocking();
            m_currentCShipPanel = CShipScreen.DualPanel;
            StartCoroutine(AnimateCShipPanel(796));
            StartCoroutine(AnimatePlayerPanel(408));
            
            //Clear dragged item
            m_currentDraggedItem = null;
            m_currentDraggedItemInventoryId = -1;
            m_currentDraggedItemIsFromPlayerInv = false;
            m_cshipCache.GetComponent<NetworkInventory>().RequestServerCancel(m_currentTicket);
        }
    }
    
    void DrawLeftPanel()
    {
        float playerSourceWidth = m_playerPanelXWidth / 408.0f;
        GUI.DrawTexture(new Rect(394, 250, 408, 400), m_dockInventoryBorder);
        GUI.DrawTextureWithTexCoords(new Rect(394, 250, m_playerPanelXWidth, 400), m_dockPlayerImage, new Rect(1 - playerSourceWidth, 1, playerSourceWidth, 1));
        
        //Equipped icons:
        float iconLeftX = 602.0f - (408.0f - m_playerPanelXWidth);
        if (iconLeftX >= 324.0f)
        {
            Rect weaponSource = new Rect(0, 0, 1, 1);
            Rect weaponTemp = new Rect(iconLeftX, m_LeftPanelWeaponRect.y, m_LeftPanelWeaponRect.width, m_LeftPanelWeaponRect.height);
            Rect shieldSource = new Rect(0, 0, 1, 1);
            Rect shieldTemp = new Rect(iconLeftX, m_LeftPanelShieldRect.y, m_LeftPanelShieldRect.width, m_LeftPanelShieldRect.height);
            Rect platingSource = new Rect(0, 0, 1, 1);
            Rect platingTemp = new Rect(iconLeftX, m_LeftPanelPlatingRect.y, m_LeftPanelPlatingRect.width, m_LeftPanelPlatingRect.height);
            Rect engineSource = new Rect(0, 0, 1, 1);
            Rect engineTemp = new Rect(iconLeftX, m_LeftPanelEngineRect.y, m_LeftPanelEngineRect.width, m_LeftPanelEngineRect.height);
            
            if (iconLeftX <= 394.0f)
            {
                //Gives how far over the border we are
                float xDiff = 394.0f - iconLeftX;
                float xPercentageOverlap = xDiff / 70.0f;
                
                //Change the sourceRect for the icons
                weaponSource = new Rect(xPercentageOverlap, 0, 1 - xPercentageOverlap, 1);
                shieldSource = new Rect(xPercentageOverlap, 0, 1 - xPercentageOverlap, 1);
                platingSource = new Rect(xPercentageOverlap, 0, 1 - xPercentageOverlap, 1);
                engineSource = new Rect(xPercentageOverlap, 0, 1 - xPercentageOverlap, 1);
                
                //Now change the coords to reflect the edge
                weaponTemp = new Rect(weaponTemp.x + xDiff, weaponTemp.y, weaponTemp.width - xDiff, weaponTemp.height);
                shieldTemp = new Rect(shieldTemp.x + xDiff, shieldTemp.y, shieldTemp.width - xDiff, shieldTemp.height);
                platingTemp = new Rect(platingTemp.x + xDiff, platingTemp.y, platingTemp.width - xDiff, platingTemp.height);
                engineTemp = new Rect(engineTemp.x + xDiff, engineTemp.y, engineTemp.width - xDiff, engineTemp.height);
            }
            
            GUI.DrawTextureWithTexCoords(weaponTemp, m_playerCache.GetEquipedWeaponItem().GetComponent<ItemScript>().GetIcon(), weaponSource);
            GUI.DrawTextureWithTexCoords(shieldTemp, m_playerCache.GetEquipedShieldItem().GetComponent<ItemScript>().GetIcon(), shieldSource);
            GUI.DrawTextureWithTexCoords(platingTemp, m_playerCache.GetEquipedPlatingItem().GetComponent<ItemScript>().GetIcon(), platingSource);
            GUI.DrawTextureWithTexCoords(engineTemp, m_playerCache.GetComponent<ItemScript>().GetIcon(), engineSource);
        }
    }
    void DrawRightPanel()
    {
        float cshipSourcewidth = (1204.0f - (float)m_cShipPanelXPos) / 408.0f;
        GUI.DrawTexture(new Rect(796, 250, 408, 400), m_dockInventoryBorder);
        GUI.DrawTextureWithTexCoords(new Rect(m_cShipPanelXPos, 250, (1204 - m_cShipPanelXPos), 400), m_dockCShipImage, new Rect(0, 0, cshipSourcewidth, 1));
        
        //Draw Icons
        float iconLeftX = 915.0f + (m_cShipPanelXPos - 796.0f);
        if (iconLeftX <= 1204.0f)
        {
            Rect weapon1Source = new Rect(0, 0, 1, 1);
            Rect weapon1Temp = new Rect(iconLeftX, m_RightPanelWeapon1Rect.y, m_RightPanelWeapon1Rect.width, m_RightPanelWeapon1Rect.height);
            Rect weapon2Source = new Rect(0, 0, 1, 1);
            Rect weapon2Temp = new Rect(iconLeftX, m_RightPanelWeapon2Rect.y, m_RightPanelWeapon2Rect.width, m_RightPanelWeapon2Rect.height);
            Rect weapon3Source = new Rect(0, 0, 1, 1);
            Rect weapon3Temp = new Rect(iconLeftX, m_RightPanelWeapon3Rect.y, m_RightPanelWeapon3Rect.width, m_RightPanelWeapon3Rect.height);
            Rect weapon4Source = new Rect(0, 0, 1, 1);
            Rect weapon4Temp = new Rect(iconLeftX, m_RightPanelWeapon4Rect.y, m_RightPanelWeapon4Rect.width, m_RightPanelWeapon4Rect.height);
            
            if (iconLeftX >= 1126.0f)
            {
                //Get how far over the border our edge is
                float widthOverlap = iconLeftX - 1126.0f;
                float percentageOverlap = widthOverlap / 78.0f;
                
                //Change the source Rect
                weapon1Source = new Rect(0, 0, 1 - percentageOverlap, 1);
                weapon2Source = new Rect(0, 0, 1 - percentageOverlap, 1);
                weapon3Source = new Rect(0, 0, 1 - percentageOverlap, 1);
                weapon4Source = new Rect(0, 0, 1 - percentageOverlap, 1);
                
                //Change the dest rect
                weapon1Temp = new Rect(weapon1Temp.x, weapon1Temp.y, weapon1Temp.width - widthOverlap, weapon1Temp.height);
                weapon2Temp = new Rect(weapon2Temp.x, weapon2Temp.y, weapon2Temp.width - widthOverlap, weapon2Temp.height);
                weapon3Temp = new Rect(weapon3Temp.x, weapon3Temp.y, weapon3Temp.width - widthOverlap, weapon3Temp.height);
                weapon4Temp = new Rect(weapon4Temp.x, weapon4Temp.y, weapon4Temp.width - widthOverlap, weapon4Temp.height);
            }
            
            GameObject[] turrets = m_cshipCache.GetAttachedTurrets();
            GUI.DrawTextureWithTexCoords(weapon1Temp, turrets[0].GetComponent<ItemScript>().GetIcon(), weapon1Source);
            GUI.DrawTextureWithTexCoords(weapon2Temp, turrets[1].GetComponent<ItemScript>().GetIcon(), weapon2Source);
            GUI.DrawTextureWithTexCoords(weapon3Temp, turrets[2].GetComponent<ItemScript>().GetIcon(), weapon3Source);
            GUI.DrawTextureWithTexCoords(weapon4Temp, turrets[3].GetComponent<ItemScript>().GetIcon(), weapon4Source);
        }
    }
    
    void DrawHoverText(string text, Vector2 mousePos)
    {
        float width = 200;
        float height = GUI.skin.GetStyle("Hover").CalcHeight(new GUIContent(text), 200);
        GUI.Label(new Rect(mousePos.x + 10, mousePos.y - 5, width, height), text, GUI.skin.GetStyle("Hover"));
    }
    #endregion
    
    #region AnimationFunctions
    IEnumerator AnimatePlayerPanel(int targetXWidth)
    {
        float t = 0;
        int oldWidth = m_playerPanelXWidth;
        
        while (t < 1)
        {
            t += Time.deltaTime;
            
            m_playerPanelXWidth = (int)Mathf.Lerp((float)oldWidth, (float)targetXWidth, t);
            
            yield return 0;
        }
        
        if (targetXWidth < 300)
            m_currentCShipPanel = CShipScreen.RightPanelActive;
        else
            m_currentCShipPanel = CShipScreen.DualPanel;
    }
    IEnumerator AnimateCShipPanel(int targetXPos)
    {
        float t = 0;
        int oldWidth = m_cShipPanelXPos;
        
        while (t < 1)
        {
            t += Time.deltaTime;
            
            m_cShipPanelXPos = (int)Mathf.Lerp((float)oldWidth, (float)targetXPos, t);
            
            yield return 0;
        }
        
        if (targetXPos > 1000)
            m_currentCShipPanel = CShipScreen.LeftPanelActive;
        else
            m_currentCShipPanel = CShipScreen.DualPanel;
    }
    #endregion
    
    bool IsMouseDownZero()
    {
        return !m_previousMouseZero && m_mouseZero;
    }
    
    bool IsMouseUpZero()
    {
        return m_previousMouseZero && !m_mouseZero;
    }
    
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
    
    void RequestServerRespawnPlayer(NetworkPlayer player)
    {
        if (Network.isClient)
            networkView.RPC("PropagateRespawnRequest", RPCMode.Server, player);
        else
            m_gscCache.RequestFastSpawnOfPlayer(player);
    }
    
    [RPC] void PropagateRespawnRequest(NetworkPlayer player)
    {
        m_gscCache.RequestFastSpawnOfPlayer(player);
    }
    #endregion
    
    #region Drag&Drop Handling
    void HandleItemDrop(bool isLeftPanel, Vector2 mousePos)
    {
        NetworkInventory inventory = m_cshipCache.GetComponent<NetworkInventory>();
        
        //Depending on where the cursor is when the mouse is released, decide what happens to the item
        if (isLeftPanel)
        {
            //If over player inventory, try to store there.
            if (m_LeftPanelPlayerRect.Contains(mousePos))
            {
                //If the item was originally from here, we don't need to do anything
                if (!m_currentDraggedItemIsFromPlayerInv)
                {
                    if (!m_playerCache.InventoryIsFull())
                    {
                        //Debug.Log ("<color=blue>Beginning item transfer sequence</color>");
                        inventory.RequestTicketValidityCheck(m_currentTicket);
                        StartCoroutine(AwaitTicketRequestResponse(inventory, RequestType.TicketValidity, ItemOwner.NetworkInventory, ItemOwner.PlayerInventory));
                        return;
                    }
                    else
                    {
                        inventory.RequestServerCancel(m_currentTicket);
                    }
                }
                m_currentDraggedItem = null;
                m_currentDraggedItemIsFromPlayerInv = false;
                m_currentDraggedItemInventoryId = -1;
            }
            //If over CShip inventory, try and drop there
            else if (m_LeftPanelCShipRect.Contains(mousePos))
            {
                if (m_currentDraggedItemIsFromPlayerInv)
                {
                    inventory.RequestServerAdd(m_currentDraggedItem);
                    StartCoroutine(AwaitTicketRequestResponse(inventory, RequestType.ItemAdd, ItemOwner.PlayerInventory, ItemOwner.NetworkInventory));
                }
                
                else
                {
                    inventory.RequestServerCancel(m_currentTicket);
                }
                
                m_currentDraggedItem = null;
                m_currentDraggedItemIsFromPlayerInv = false;
                m_currentDraggedItemInventoryId = -1;
            }
            //If over any equipment slot points try and equip it
            else if (m_LeftPanelWeaponRect.Contains(mousePos))
            {
                HandlePlayerEquipmentDrop (inventory, 1, ItemType.Weapon);
            }
            else if (m_LeftPanelShieldRect.Contains(mousePos))
            {
                HandlePlayerEquipmentDrop (inventory, 2, ItemType.Shield);
            }
            else if (m_LeftPanelPlatingRect.Contains(mousePos))
            {
                HandlePlayerEquipmentDrop (inventory, 3, ItemType.Plating);
            }
            else if (m_LeftPanelEngineRect.Contains(mousePos))
            {
                HandlePlayerEquipmentDrop (inventory, 4, ItemType.Engine);
            }
        }
        else
        {
            //If over player inventory, try to store there.
            if (m_RightPanelPlayerRect.Contains(mousePos))
            {
                //If the item was originally from here, we don't need to do anything
                if (!m_currentDraggedItemIsFromPlayerInv)
                {
                    if (!m_playerCache.InventoryIsFull())
                    {
                        inventory.RequestTicketValidityCheck(m_currentTicket);
                        StartCoroutine(AwaitTicketRequestResponse(inventory, RequestType.TicketValidity, ItemOwner.NetworkInventory, ItemOwner.PlayerInventory));
                        return;
                    }
                    else
                    {
                        inventory.RequestServerCancel(m_currentTicket);
                    }
                }
                m_currentDraggedItem = null;
                m_currentDraggedItemIsFromPlayerInv = false;
                m_currentDraggedItemInventoryId = -1;
            }
            //If over CShip inventory, try and drop there
            else if (m_RightPanelCShipRect.Contains(mousePos))
            {
                if (m_currentDraggedItemIsFromPlayerInv)
                {
                    //CShip.GetComponent<CapitalShipScript>().AddItemToInventory(thisPlayerHP.GetComponent<PlayerControlScript>().GetItemInSlot(m_currentDraggedItemInventoryId));
                    inventory.RequestServerAdd(m_currentDraggedItem);
                    StartCoroutine(AwaitTicketRequestResponse(inventory, RequestType.ItemAdd, ItemOwner.PlayerInventory, ItemOwner.NetworkInventory));
                }
                
                else
                {
                    // Ensure the ticket is cancelled since the item must have been requested at this point
                    inventory.RequestServerCancel(m_currentTicket);
                }
                
                m_currentDraggedItem = null;
                m_currentDraggedItemIsFromPlayerInv = false;
                m_currentDraggedItemInventoryId = -1;
            }
            //If over any equipment slot points try and equip it
            else if (m_RightPanelWeapon1Rect.Contains(mousePos))
            {
                HandleCShipEquipmentDrop (inventory, 1);
            }
            else if (m_RightPanelWeapon2Rect.Contains(mousePos))
            {
                HandleCShipEquipmentDrop (inventory, 2);
            }
            else if (m_RightPanelWeapon3Rect.Contains(mousePos))
            {               
                HandleCShipEquipmentDrop (inventory, 3);
            }
            else if (m_RightPanelWeapon4Rect.Contains(mousePos))
            {
                HandleCShipEquipmentDrop (inventory, 4);
            }
        }
    }
    
    void HandlePlayerEquipmentDrop (NetworkInventory inventory, int equipmentSlot, ItemType checkIs)
    {
        if (m_currentDraggedItem.GetComponent<ItemScript>().GetTypeOfItem() == checkIs)
        {
            //if the item is a weapon, equip plz!
            if (m_currentDraggedItemIsFromPlayerInv)
            {
                m_playerCache.EquipItemInSlot (m_currentDraggedItemInventoryId);
            }
            
            else
            {
                inventory.RequestTicketValidityCheck (m_currentTicket);
                StartCoroutine (AwaitTicketRequestResponse (inventory, RequestType.TicketValidity, ItemOwner.NetworkInventory, ItemOwner.PlayerEquipment, false, equipmentSlot));
            }
        }
        
        else if (!m_currentDraggedItemIsFromPlayerInv)
        {
            inventory.RequestServerCancel (m_currentTicket);
        }
        
        m_currentDraggedItem = null;
        m_currentDraggedItemIsFromPlayerInv = false;
        m_currentDraggedItemInventoryId = -1;
    }
    
    void HandleCShipEquipmentDrop (NetworkInventory inventory, int turretID)
    {
        if (m_currentDraggedItem.GetComponent<ItemScript>().GetTypeOfItem() == ItemType.CapitalWeapon)
        {
            if (m_currentDraggedItemIsFromPlayerInv)
            {
                GameObject oldTurret = m_cshipCache.GetAttachedTurrets()[turretID - 1];                     
                
                m_cshipCache.TellServerEquipTurret (turretID, m_currentDraggedItem.gameObject);
                m_playerCache.RemoveItemFromInventory (m_currentDraggedItem.gameObject);                   
                
                if (m_playerCache.InventoryIsFull())
                {
                    // Move the item to the CShip inventory since the players inventory is full
                    inventory.RequestServerAdd (oldTurret.GetComponent<ItemScript>());
                    StartCoroutine (AwaitTicketRequestResponse (inventory, RequestType.ItemAdd, ItemOwner.CShipEquipment, ItemOwner.NetworkInventory));
                }
                
                else
                {
                    m_playerCache.AddItemToInventory (oldTurret);
                }
                
            }
            
            else
            {
                inventory.RequestTicketValidityCheck (m_currentTicket);
                StartCoroutine (AwaitTicketRequestResponse (inventory, RequestType.TicketValidity, ItemOwner.NetworkInventory, ItemOwner.CShipEquipment, false, turretID));
            }
        }
        
        else
        {
            inventory.RequestServerCancel (m_currentTicket);
        }
        
        
        m_currentDraggedItem = null;
        m_currentDraggedItemIsFromPlayerInv = false;
        m_currentDraggedItemInventoryId = -1;
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
                        m_playerCache.RemoveItemFromInventory(GameObject.FindGameObjectWithTag("ItemManager").GetComponent<ItemIDHolder>().GetItemWithID(itemID));
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
                        ItemScript item = GameObject.FindGameObjectWithTag ("ItemManager").GetComponent<ItemIDHolder>().GetItemWithID (m_currentTicket.itemID).GetComponent<ItemScript>();
                        switch (to)
                        {
                        case ItemOwner.PlayerInventory:
                        {
                            int costIfShop = fromShop ? inventory.GetComponent<ShopScript>().GetItemCost(m_currentTicket.itemIndex) : -1;
                            
                            if (inventory.RemoveItemFromServer (m_currentTicket))
                            {
                                m_playerCache.AddItemToInventory (item.gameObject);
                                
                                if(fromShop)
                                {
                                    m_shopConfirmBuy = false;
                                    m_confirmBuyItem = null;
                                    m_playerCache.RemoveCash(costIfShop);
                                }
                            }
                            
                            else
                            {
                                Debug.LogError("<color=blue>Ticket mismatch!</color>");
                            }
                            
                            break;
                        }
                        case ItemOwner.CShipEquipment:
                        {
                            if (item.GetTypeOfItem() == ItemType.CapitalWeapon && inventory.RemoveItemFromServer (m_currentTicket))
                            {
                                GameObject oldTurret = m_cshipCache.GetAttachedTurrets()[equipmentSlot - 1];
                                
                                m_cshipCache.TellServerEquipTurret (equipmentSlot, item.gameObject);
                                
                                if (m_playerCache.InventoryIsFull())
                                {
                                    // Move the item to the CShip inventory since the players inventory is full
                                    inventory.RequestServerAdd (oldTurret.GetComponent<ItemScript>());
                                    StartCoroutine (AwaitTicketRequestResponse (inventory, RequestType.ItemAdd, to, from));
                                }
                                
                                else
                                {
                                    m_playerCache.AddItemToInventory (oldTurret);
                                }
                            }
                            
                            break;
                        }
                        case ItemOwner.PlayerEquipment:
                        {   
                            if (item.GetTypeOfItem() != ItemType.CapitalWeapon && inventory.RemoveItemFromServer (m_currentTicket))
                            {
                                GameObject oldEquipment = m_playerCache.GetEquipmentFromSlot (equipmentSlot);
                                
                                if (m_playerCache.InventoryIsFull())
                                {
                                    GameObject lastItem = m_playerCache.GetPlayerInventory()[m_playerCache.GetPlayerInventory().Count - 1];
                                    
                                    // This little wonder makes me want to vomit and should only be used until the demo night
                                    // Remove the last item from the players inventory then equip the new item from that
                                    m_playerCache.RemoveItemFromInventory (lastItem);
                                    m_playerCache.AddItemToInventory (item.gameObject);
                                    m_playerCache.EquipItemInSlot (m_playerCache.GetPlayerInventory().Count - 1);
                                    m_playerCache.RemoveItemFromInventory (oldEquipment);
                                    m_playerCache.AddItemToInventory (lastItem);
                                    
                                    // Move the item to the CShip inventory since the players inventory is full
                                    inventory.RequestServerAdd (oldEquipment.GetComponent<ItemScript>());
                                    StartCoroutine (AwaitTicketRequestResponse (inventory, RequestType.ItemAdd, to, from));
                                }
                                else
                                {
                                    m_playerCache.AddItemToInventory (item.gameObject);
                                    m_playerCache.EquipItemInSlot (m_playerCache.GetPlayerInventory().Count - 1);
                                }
                            }
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
}

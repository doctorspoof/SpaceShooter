using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GUIInGameMaster : GUIBaseMaster 
{
    // Serializable Members
    [SerializeField]        GUISkin m_mainMenuSkin;

    // Cached Members
    PlayerControlScript     m_pcsCache;

	/* Unity Functions */
	void Start () 
    {
        m_listOfScreensToDraw = new List<BaseGUIScreen>();
        List<BaseGUIScreen> temp = new List<BaseGUIScreen>();
        temp.Add(GetComponent<GUIInGameHUDScreen>());
        UpdateScreensToDraw(temp);
	}
    
    void OnGUI ()
    {
        float rx = Screen.width / m_nativeWidth;
        float ry = Screen.height / m_nativeHeight;
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(rx, ry, 1));
        
        if(GUI.skin != m_mainMenuSkin)
            GUI.skin = m_mainMenuSkin;
        
        //Draw all the screens currently in the list
        for(int i = 0; i < m_listOfScreensToDraw.Count; i++)
        {
            bool isHighest = (m_listOfScreensToDraw[i].m_priorityValue == m_highestPriority);
            m_listOfScreensToDraw[i].ManualGUICall(isHighest);
        }
    }
    
    // Custom Functions
    public override void ChangeGameState (GameState newState)
    {
        switch (newState)
        {
            case GameState.InGame:
            {
                List<BaseGUIScreen> temp = new List<BaseGUIScreen>();
                temp.Add(GetComponent<GUIInGameHUDScreen>());
                temp.Add(GetComponent<GUIMapOverlayScreen>());
                UpdateScreensToDraw(temp);
                AlertPlayerHomingReady(true);
                break;
            }
            case GameState.InGameConnectionLost:
            {
                List<BaseGUIScreen> temp = new List<BaseGUIScreen>(m_listOfScreensToDraw);
                temp.Add(GetComponent<GUIDisconnectedScreen>());
                UpdateScreensToDraw(temp);
                AlertPlayerHomingReady(false);
                break;
            }
            case GameState.InGameCShipDock:
            {
                List<BaseGUIScreen> temp = new List<BaseGUIScreen>();
                temp.Add(GetComponent<GUIInGameHUDScreen>());
                temp.Add(GetComponent<GUICShipDockScreen>());
                UpdateScreensToDraw(temp);
                AlertPlayerHomingReady(false);
                break;
            }
            case GameState.InGameShopDock:
            {
                List<BaseGUIScreen> temp = new List<BaseGUIScreen>();
                temp.Add(GetComponent<GUIInGameHUDScreen>());
                temp.Add(GetComponent<GUIShopDockScreen>());
                UpdateScreensToDraw(temp);
                AlertPlayerHomingReady(false);
                break;
            }
            case GameState.InGameGameOver:
            {
                List<BaseGUIScreen> temp = new List<BaseGUIScreen>(m_listOfScreensToDraw);
                temp.Add(GetComponent<GUILossSplashScreen>());
                UpdateScreensToDraw(temp);
                AlertPlayerHomingReady(false);
                break;
            }
            case GameState.InGameMenu:
            {
                List<BaseGUIScreen> temp = new List<BaseGUIScreen>(m_listOfScreensToDraw);
                temp.Add(GetComponent<GUIEscapeMenuScreen>());
                UpdateScreensToDraw(temp);
                AlertPlayerHomingReady(false);
                break;
            }
            case GameState.LoadingScreen:
            {
                m_listOfScreensToDraw.Clear();
                m_listOfScreensToDraw.Add (GetComponent<GUILoadingScreen>());
                m_highestPriority = 3;
                break;
            }
            default:
            {
                Debug.Log ("Passed game state is not handled by this GUIMaster!");
                break;
            }
        }
    }
    
    #region Generic Sets/Calls
    public void PassThroughCShipReference(GameObject cShip)
    {
        GetComponent<GUIMapOverlayScreen>().SetCShipReference(cShip);
        GetComponent<GUIInGameHUDScreen>().SetCShipReference(cShip);
        GetComponent<GUICShipDockScreen>().SetCShipReference(cShip);
    }
    public void PassThroughPlayerReference(GameObject player)
    {
        GetComponent<GUIMapOverlayScreen>().SetPlayerReference(player);
        GetComponent<GUIInGameHUDScreen>().SetPlayerReference(player);
        GetComponent<GUICShipDockScreen>().SetPlayerReference(player);
        GetComponent<GUIShopDockScreen>().SetPlayerReference(player);
        m_pcsCache = player.GetComponent<PlayerControlScript>();
    }
    #endregion
    
    #region Specific Toggles/Calls
    public void ClearAlerts()
    {
        GetComponent<GUIInGameHUDScreen>().ClearGUIAlerts();
    }
    public void AlertTransitionNeedsInput(bool state)
    {
        networkView.RPC ("PassThroughTransitionInputState", RPCMode.All, state);
    }
    [RPC] void PassThroughTransitionInputState(bool state)
    {
        GetComponent<GUICShipDockScreen>().SetTransitionConfirm(state);
    }
    public void AlertCShipDockJumpDone()
    {
        GetComponent<GUICShipDockScreen>().SetCShipJumpIsDone();
    }
    public void ToggleShowSectorJump(bool show)
    {
        GetComponent<GUICShipDockScreen>().SetCShipCanSectorJump(show);
    }
    public void PassThroughNewGUIAlert(string message, float displayTime)
    {
        GetComponent<GUIInGameHUDScreen>().AddNewGUIAlert(message, displayTime);
    }
    public void PassThroughUpdateShopReferences()
    {
        GetComponent<GUIMapOverlayScreen>().UpdateShopList();
    }
    public void PassThroughCShipDockableState(bool dockable)
    {
        GetComponent<GUIInGameHUDScreen>().SetCShipDockable(dockable);
    }
    public void PassThroughShopDockableState(bool dockable)
    {
        GetComponent<GUIInGameHUDScreen>().SetShopDockable(dockable);
    }
    public void PassThroughPlayerWeaponReference(GameObject weapon)
    {
        GetComponent<GUIInGameHUDScreen>().SetWeaponReference(weapon.GetComponent<EquipmentWeapon>());
    }
    public void ToggleBigMapState()
    {
        GetComponent<GUIMapOverlayScreen>().ToggleBigMap();
    }
    
    public void ToggleSmallMapState()
    {
        GetComponent<GUIMapOverlayScreen>().ToggleSmallMap();
    }
    
    public void ToggleMapsTogether()
    {
        GetComponent<GUIMapOverlayScreen>().ToggleMapsTogether();
    }
    
    public void StartPopupCShipTakenDamage()
    {
        GetComponent<GUIInGameHUDScreen>().SetCShipUnderFire();
    }
    
    public void SetInsufficientRespawnCash(bool state)
    {
        GetComponent<GUIInGameHUDScreen>().SetNoRespawnCash(state);
    }
    
    public void SetDockedShop(GameObject shop)
    {
        GetComponent<GUIShopDockScreen>().SetShopCache(shop);
    }
    
    public void SetPlayerDead(bool state)
    {
        GetComponent<GUIInGameHUDScreen>().SetPlayerDead(state);
    }
    
    public void SetOutOfBoundsWarning(bool state)
    {
        GetComponent<GUIInGameHUDScreen>().SetIsOutOfBounds(state);
    }
    
    public void ResetPlayerList()
    {
        GetComponent<GUIMapOverlayScreen>().ResetPlayerList();
    }
    
    public void PassThroughHomingState(bool locked, bool locking)
    {
        GetComponent<GUIInGameHUDScreen>().SetLockingState(locked, locking);
    }
    #endregion
    
    #region External Script Access
    void AlertPlayerHomingReady(bool ready)
    {
        if(m_pcsCache != null)
            m_pcsCache.SetCorrectHomingScreen(ready);
    }
    #endregion
}

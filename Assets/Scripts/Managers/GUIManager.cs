using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.IO;

public enum CShipScreen
{
    PlayerPanel = 1,
    StatusPanel = 2,
    ObjectivePanel = 3,
    DualPanel = 5,
    LeftPanelActive = 6,
    RightPanelActive = 7,
    PanelsAnimating = 8
}

public class GUIManager : MonoBehaviour
{
#if UNITY_STANDALONE
	[System.Runtime.InteropServices.DllImport("user32.dll")]
	static extern bool ClipCursor(ref RECT lpRect);
	[System.Runtime.InteropServices.DllImport("user32.dll")]
	static extern void GetClipCursor(ref RECT lpRect);
	[System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "SetWindowPos")]
	private static extern bool SetWindowPos(System.IntPtr hwnd, int hWndInsertAfter, int x, int Y, int cx, int cy, int wFlags);
	[System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "FindWindow")]
	public static extern System.IntPtr FindWindow(System.String className, System.String windowName);
	
	public struct RECT
	{
		public int Left;
		public int Top;
		public int Right;
		public int Bottom;
	}
    
    RECT previousCursorClip;
#endif

    // Assignables
    [SerializeField]        float m_shopRestockTime = 120.0f;       //How long in seconds it takes for the stores to restock
    
    // Textures
    [SerializeField]        Texture m_menuBackground;
    [SerializeField]        Texture m_menuButtonHighlight;
    
    // Docking Assets
    [SerializeField]        Texture m_DockBackground;
    [SerializeField]        Texture m_DockCShipImage;
    [SerializeField]        Texture m_DockPlayerImage;
    [SerializeField]        Texture m_DockInventoryBorder;
    [SerializeField]        Texture m_shopBaseTexture;
    [SerializeField]        Texture m_smallShopTexture;
    
    // Spectator Assets
    [SerializeField]        Texture m_SpecCShipImage;
    [SerializeField]        Texture m_SpecBottomPanel;
    
    // Map Assets
    [SerializeField]        Texture m_mapOverlay;
    [SerializeField]        Texture m_selfPBlob;
    [SerializeField]        Texture m_otherPBlob;
    [SerializeField]        Texture m_cShipBlob;
    [SerializeField]        Texture m_enemyBlob;
    [SerializeField]        Texture m_specEnemyBlob;
    
    // InGame Assets
    [SerializeField]        Texture m_playerBarBorder;
    [SerializeField]        Texture m_playerBarHealth;
    [SerializeField]        Texture m_playerBarShield;
    [SerializeField]        Texture m_cShipBarBorder;
    [SerializeField]        Texture m_cShipBarHealth;
    [SerializeField]        Texture m_cShipBarShield;
    [SerializeField]        Texture m_healthBar;
    [SerializeField]        Texture m_healthBackground;
    [SerializeField]        Texture m_shieldBar;
    [SerializeField]        Texture m_iconBorder;
    [SerializeField]        Texture m_playerIcon;
    [SerializeField]        Texture m_CShipIcon;
    [SerializeField]        Texture m_cursor;
    [SerializeField]        Texture m_cursorLocking;
    [SerializeField]        Texture m_cursorLocked;
    [SerializeField]        Texture m_enemyLocked;
    [SerializeField]        Texture m_reloadBackground;
    [SerializeField]        Texture m_reloadBar;
    [SerializeField]        Texture m_barEnd;
    [SerializeField]        Texture m_barMid;
    
    // GUIStyles
    [SerializeField]        GUIStyle m_sharedGUIStyle;
    [SerializeField]        GUIStyle m_sharedHighlightedGUIStyle;
    [SerializeField]        GUIStyle m_hoverBoxTextStyle;
    [SerializeField]        GUIStyle m_nonBoxStyle;
    [SerializeField]        GUIStyle m_nonBoxSmallStyle;
    [SerializeField]        GUIStyle m_nonBoxBigStyle;
    [SerializeField]        GUIStyle m_invisibleStyle;

    /* Internal members */
    GameObject[] m_playerShips;
    GameObject[] m_shops;
    GameObject m_gameStateController;
    bool m_noRespawnCash = false;
    //bool m_PlayerRequestsRound = false;
    //bool m_ArenaClearOfEnemies = true;
    bool m_PlayerHasDied = false;
    bool m_CShipHasDied = false;
    bool m_PlayerHasDockedAtCapital = false;
    //bool m_playerIsSelectingCShipTurret = false;
    bool m_PlayerHasDockedAtShop = false;
    bool m_shouldShowVictorySplash = false;
    bool m_shouldShowLossSplash = false;
    Resolution m_newResolution;
    //bool m_playerHasAlreadyLeft = false;
    bool m_currentWeaponNeedsLockon = false;
    GameObject m_lastLockonTarget = null;
    bool m_isOnFollowMap = true;
    //bool m_shipyardScreen = true;
    bool m_isOnMap = false;
    float m_blobSize;
    bool m_shouldShowWarningAttack = false;
    //bool m_shouldShowDisconnectedSplash = false;
    bool isOoBCountingDown = false;
    bool m_cshipDying = false;

    #region getset

    public bool GetCurrentWeaponNeedsLockon()
    {
        return m_currentWeaponNeedsLockon;
    }

    public void SetCurrentWeaponNeedsLockon(bool flag_)
    {
        m_currentWeaponNeedsLockon = flag_;
    }

    public bool GetIsOnFollowMap()
    {
        return m_isOnFollowMap;
    }

    public void SetIsOnFollowMap(bool flag_)
    {
        m_isOnFollowMap = flag_;
    }

    public void FlipIsOnFollowMap()
    {
        m_isOnFollowMap = !m_isOnFollowMap;
    }

    public void SetPlayerHasDockedAtCapital(bool flag_)
    {
        m_PlayerHasDockedAtCapital = flag_;
    }

    public void SetPlayerHasDockedAtShop(bool flag_)
    {
        m_PlayerHasDockedAtShop = flag_;
    }

    #endregion getset

    // Events stuff
    string eventText = "You shouldn't see this";
    string eventTriggerer = "NameHere";
    EventScript currEventSc;
    bool m_eventIsActive = false;
    bool m_eventIsOnOutcome = false;
    bool m_hostShouldSelectTiebreaker = false;
    bool m_hostIsTieBreaking = false;
    bool m_hasContinued = false;
    int continueVotes = 0;
    
    // Player Select vars
    string m_lastVote = "";
    int[] m_playerVotes;
    bool m_eventIsOnPlayerSelect = false;
    float m_playerSelectTimer = 20.0f;
    string m_selectedPlayerName = "";
    
    // State cache
    bool m_inGameMenuIsOpen = false;
    GameState m_currentGameState = GameState.MainMenu;
    string m_IPField;
    string m_username = "Name";
    CShipScreen m_currentCShipPanel = CShipScreen.DualPanel;
    GameObject m_shopDockedAt = null;
    HealthScript m_thisPlayerHP;
    bool m_previousMouseZero = false;
    bool m_mouseZero = false;
    ItemTicket m_currentTicket;

    #region getset

    public void SetShopDockedAt(GameObject shop_)
    {
        m_shopDockedAt = shop_;
    }

    public HealthScript GetThisPlayerHP()
    {
        return m_thisPlayerHP;
    }

    public void SetThisPlayerHP(HealthScript healthScript_)
    {
        m_thisPlayerHP = healthScript_;
    }

    #endregion getset

    // Spec mode versions
    bool m_hostShouldStartSpec = false;
    bool m_isSpecMode = false;
    int m_trackedPlayerID = -1;
    public GameObject[] m_players;
    bool m_beginLockBreak = false;
    bool m_isLockingOn = false;

    // Enemy radar vars
    float m_nativeWidth = 1600;
    float m_nativeHeight = 900;
    float m_currentPingTime = 0.0f;
    float m_reqPingTime = 10.0f;
    GameObject[] m_pingedEnemies;
    GameObject[] m_pingedMissiles;

    #region getset

    public bool GetUseController()
    {
        int control = PlayerPrefs.GetInt("UseControl");
        if (control == 1)
            return true;
        else
        return false;
    }

    #endregion getset

    // Button remembrance
    int m_selectedButton = 0;
    int m_selectedSubButton = 0;
    int m_maxButton = 4;
    int m_maxSubButton = 1;
    
    // CShip cache
    GameObject m_cShipGameObject;
    HealthScript m_cShipHealth;
    
    // Timers
    bool m_shouldResetShopsNow = false;
    float m_shopTimer = 0.0f;
    float m_shopResetDisplayTimer = 200.0f;
    //float m_lockOffTime = 0.0f;
    float m_lockOnTime = 0.0f;
    float m_deathTimer = 45.0f;
    float m_continueTimer = 10.0f;
    float m_attackWarningTimer = 50.0f;
    float m_outOfBoundsTimer = 10.0f;
    
    // Input/Controls
    float m_dpadScrollPause = 0;
    bool m_hasLockedTarget = false;
    
    // Drag & Drop stuff
    
    ItemScript m_currentDraggedItem = null;
    bool m_currentDraggedItemIsFromPlayerInv = true;
    int m_currentDraggedItemInventoryId = -1;
    Vector2 m_playerScrollPosition = Vector2.zero;
    Vector2 m_cShipScrollPosition = Vector2.zero;
    bool m_transferFailed = false;
    string m_transferFailedMessage = "Transfer failed - Item Requested Elsewhere";

    // Chat Vars
    //List<string> m_chatMessages;


    #region getset

    public void SetCShip(GameObject cship)
    {
        //Debug.Log("Attaching cship to GUI");
        m_cShipGameObject = cship;
        m_cShipHealth = cship.GetComponent<HealthScript>();
    }

    #endregion getset

    /* Unity Functions */
    void Start()
    {
        Time.timeScale = 1.0f;
        
        
#if UNITY_STANDALONE
        GetClipCursor(ref previousCursorClip);

        System.IntPtr window = FindWindow(null, "Galaxodus");
        Rect current = new Rect(0, 0, Screen.width, Screen.height);
        SetWindowPos(window, 0, 0, 0, (int)current.width, (int)current.height, 0);

        RECT cursorLimits;
        cursorLimits.Left = 0;
        cursorLimits.Top = 0;
        cursorLimits.Right = Screen.width - 1;
        cursorLimits.Bottom = Screen.height - 1;
        ClipCursor(ref cursorLimits);
#endif

        m_newResolution = Screen.currentResolution;

        m_blobSize = Screen.height * 0.015f;
        
        if(m_gameStateController == null)
        {
            m_gameStateController = GameObject.FindGameObjectWithTag("GameController");
        }
        
        if(m_shops == null || m_shops.Length == 0)
        {
            m_shops = GameObject.FindGameObjectsWithTag("Shop");
        }
    }

    void Update()
    {
        
        if (m_shopResetDisplayTimer < 7.5f)
            m_shopResetDisplayTimer += Time.deltaTime;
        
        if (Network.isServer)
        {
            m_shopTimer += Time.deltaTime;
            
            if (m_shopTimer >= m_shopRestockTime || m_shouldResetShopsNow)
            {
                m_shouldResetShopsNow = false;
                m_shopTimer = 0.0f;
                networkView.RPC ("PropagateRestockShops", RPCMode.All);
                
                
                
                //Tell shops to reset their inventories
                GameObject[] shops = GameObject.FindGameObjectsWithTag("Shop");
                
                foreach (GameObject shop in shops)
                {
                    //TODO: UNCOMMENT AND FIX THIS
                    //shop.GetComponent<ShopScript>().RequestNewInventory(m_gameTimer);
                }
            }
        }
        
        if (m_currentGameState == GameState.InGame)
        {
            GameStateController controller = m_gameStateController.GetComponent<GameStateController>();
            if (m_playerShips == null || m_playerShips.Length == 0 || m_playerShips.Length < controller.GetConnectedPlayers().Count - controller.GetDeadPlayers().Count)
            {
                Debug.Log("Resetting player array. Length vs Count == " + m_playerShips.Length + " vs " + (controller.GetConnectedPlayers().Count - controller.GetDeadPlayers().Count));
                m_playerShips = GameObject.FindGameObjectsWithTag("Player");
            }
            
            if (!m_CShipHasDied && !m_cshipDying && (m_cShipGameObject == null || m_cShipGameObject.GetComponent<HealthScript>() == null))
            {
                Debug.Log("No capital ship attached! Finding in game cship...");
                GameObject cship = GameObject.FindGameObjectWithTag("Capital");
                SetCShip(cship);
            }
            
            m_currentPingTime += Time.deltaTime;
            if (m_currentPingTime >= m_reqPingTime)
            {
                PingForEnemies();
            }
        }
        else
        {
            if (Input.GetButtonDown("X360A"))
            {
                Debug.Log("Activating selected button.");
                ActivateMenuControllerPress();
            }
            
            if (Input.GetButtonDown("X360B"))
            {
                AttemptGoBackControllerPress();
            }
            
            if (Input.GetAxis("X360DPADVertical") > 0 || Input.GetAxis("LeftStickVertical") > 0)
            {
                if (m_dpadScrollPause <= 0.0f)
                {
                    ScrollUpMenu();
                    m_dpadScrollPause = 0.5f;
                }
            }
            else if (Input.GetAxis("X360DPADVertical") < 0 || Input.GetAxis("LeftStickVertical") < 0)
            {
                if (m_dpadScrollPause <= 0.0f)
                {
                    ScrollDownMenu();
                    m_dpadScrollPause = 0.5f;
                }
            }
            else if (Input.GetAxis("X360DPADHorizontal") > 0 || Input.GetAxis("LeftStickHorizontal") > 0)
            {
                if (m_dpadScrollPause <= 0.0f)
                {
                    ScrollRightMenu();
                    m_dpadScrollPause = 0.5f;
                }
            }
            else if (Input.GetAxis("X360DPADHorizontal") < 0 || Input.GetAxis("LeftStickHorizontal") < 0)
            {
                if (m_dpadScrollPause <= 0.0f)
                {
                    ScrollLeftMenu();
                    m_dpadScrollPause = 0.5f;
                }
            }
            else
            {
                m_dpadScrollPause = 0.0f;
            }
        }
        
        if (m_dpadScrollPause > 0)
            m_dpadScrollPause -= Time.deltaTime;
        
        if (m_shouldShowWarningAttack)
        {
            m_attackWarningTimer += Time.deltaTime;
            if (m_attackWarningTimer >= 1.5f)
                m_shouldShowWarningAttack = false;
        }
        
        if (m_eventIsActive)
        {
            if (m_eventIsOnOutcome)
            {
                m_continueTimer -= Time.deltaTime;
                if (m_continueTimer <= 0)
                {
                    OnEventComplete();
                }
            }
            else if (m_eventIsOnPlayerSelect)
            {
                m_playerSelectTimer -= Time.deltaTime;
                if (m_playerSelectTimer <= 0)
                {
                    CheckMostPlayerVotes();
                }
            }
        }
        
        if (isOoBCountingDown)
        {
            m_outOfBoundsTimer -= Time.deltaTime;
            // Check if the player has died
            if (!m_thisPlayerHP)
            {
                StopOutOfBoundsWarning();
            }
            
            if (m_outOfBoundsTimer <= 0)
            {
                Debug.Log("[GUI]: Telling player to die from OoB");
                //thisPlayerHP.DamageMobHullDirectly(1000);
                m_thisPlayerHP.RemotePlayerRequestsDirectDamage(10000);
                m_outOfBoundsTimer = 45.0f;
                isOoBCountingDown = false;
            }
        }
        
        if (m_PlayerHasDied)
        {
            if (m_deathTimer > 0)
                m_deathTimer -= Time.deltaTime;
        }
        
        if (m_lastLockonTarget == null)
        {
            m_hasLockedTarget = false;
        }
        
        if (m_isLockingOn && !m_hasLockedTarget)
        {
            m_lockOnTime += Time.deltaTime;
            //TODO: Parametise this later
            if (m_lockOnTime > 0.7f)
            {
                if (m_thisPlayerHP)
                {
                    m_thisPlayerHP.GetComponent<PlayerControlScript>().SetNewTargetLock(m_lastLockonTarget);
                    m_hasLockedTarget = true;
                }
                
                else
                {
                    m_lockOnTime = 0f;
                }
                
            }
        }
        
        if (m_beginLockBreak && m_lastLockonTarget != null)
        {
            //Distance-based lock break
            if (CheckPlayerTargetDistanceOver())
            {
                m_hasLockedTarget = false;
                m_lastLockonTarget = null;
                m_thisPlayerHP.GetComponent<PlayerControlScript>().UnsetTargetLock();
                m_lockOnTime = 0.0f;
                m_beginLockBreak = false;
            }
        }
    }

    void OnGUI()
    {
        // Update our fake input tracking
        m_previousMouseZero = m_mouseZero;
        m_mouseZero = Input.GetMouseButton(0);
        
        float rx = Screen.width / m_nativeWidth;
        float ry = Screen.height / m_nativeHeight;
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(rx, ry, 1));
        
        switch (m_currentGameState)
        {
            case GameState.MainMenu:
            {
                DrawMainMenu();
                break;
            }
            case GameState.HostMenu:
            {
                DrawHostMenu();
                break;
            }
            case GameState.ClientInputIP:
            {
                DrawClientConnectMenu();
                break;
            }
            case GameState.ClientMenu:
            {
                DrawClientMenu();
                break;
            }
            case GameState.MapMenu:
            {
                break;
            }
            case GameState.InGame:
            {
                DrawInGame();
                break;
            }
            case GameState.OptionMenu:
            {
                DrawOptionMenu();
                break;
            }
            case GameState.AttemptingConnect:
            {
                DrawConnectingScreen();
                break;
            }
            case GameState.FailedConnectName:
            {
                DrawFailedConnectByName();
                break;
            }
            case GameState.InGameConnectionLost:
            {
                DrawLostConnection();
                break;
            }
        }
    }

    void OnApplicationQuit()
    {
#if UNITY_STANDALONE
        ClipCursor(ref previousCursorClip);
#endif
    }

    /* Custom Functions */
    
    // GUI Functions
    #region DrawFunctions
    void DrawLostConnection()
    {
        
    }
    
    void DrawFailedConnectByName()
    {
        
    }
    
    void DrawConnectingScreen()
    {
        
    }
    
    void DrawOptionMenu()
    {
        
        
        /* Control option */
        
        //Redraw main menu, but any click on it should close option
        GUI.DrawTexture(new Rect(222, 130, 290, 620), m_menuBackground);
        if (GUI.Button(new Rect(222, 130, 290, 620), "", m_invisibleStyle))
        {
            m_gameStateController.GetComponent<GameStateController>().CloseOptionMenu();
        }
        
        GUI.Label(new Rect(225, 131, 285, 100), "Please enter a name and select an option:", m_sharedGUIStyle);
        GUI.Label(new Rect(225, 228, 285, 50), m_username, m_sharedGUIStyle);
        GUI.Label(new Rect(225, 400, 285, 50), "HOST", m_sharedGUIStyle);
        GUI.Button(new Rect(225, 450, 285, 50), "JOIN", m_sharedGUIStyle);
        GUI.Button(new Rect(225, 500, 285, 50), "OPTIONS", m_sharedGUIStyle);
        GUI.Button(new Rect(225, 698, 285, 50), "QUIT", m_sharedGUIStyle);
    }
    
    void DrawMainMenu()
    {
        
    }
    
    void DrawHostMenu()
    {
        GUI.DrawTexture(new Rect(222, 130, 290, 620), m_menuBackground);
        
        GUI.Label(new Rect(225, 131, 285, 50), "Connected Players:", m_nonBoxBigStyle);
        GameStateController gsc = m_gameStateController.GetComponent<GameStateController>();
        for (int i = 0; i < gsc.GetConnectedPlayers().Count; i++)
        {
            GUI.Label(new Rect(225, 188 + (i * 40), 285, 40), gsc.GetConnectedPlayers()[i].m_name, m_nonBoxStyle);
        }
        
        if (GUI.Button(new Rect(225, 600, 285, 100), "START", m_sharedGUIStyle))
        {
            HostMenuStartButtonActivate();
        }
        if (m_selectedButton == 1 && m_selectedSubButton == 0)
        {
            GUI.DrawTexture(new Rect(225, 600, 285, 100), m_menuButtonHighlight);
        }
        
        if (!m_hostShouldStartSpec)
        {
            if (GUI.Button(new Rect(510, 600, 140, 100), "Spectator mode", m_sharedGUIStyle))
            {
                m_hostShouldStartSpec = true;
            }
        }
        else
        {
            if (GUI.Button(new Rect(510, 600, 140, 100), "Spectator mode", m_sharedHighlightedGUIStyle))
            {
                m_hostShouldStartSpec = false;
            }
        }
        if (m_selectedButton == 1 && m_selectedSubButton == 1)
        {
            GUI.DrawTexture(new Rect(510, 600, 140, 100), m_menuButtonHighlight);
        }
        //m_hostShouldStartSpec = GUI.Toggle(new Rect(600, 850, 75, 75), m_hostShouldStartSpec, "");
        
        if (GUI.Button(new Rect(225, 698, 285, 50), "BACK", m_sharedGUIStyle))
        {
            HostMenuBackActivate();
        }
        if (m_selectedButton == 2)
        {
            GUI.DrawTexture(new Rect(225, 698, 285, 50), m_menuButtonHighlight);
        }
    }
    
    void DrawClientConnectMenu()
    {
        
    }
    
    void DrawClientMenu()
    {
        
    }
    
    void DrawInGameSpec()
    {
        //Timer: show timer in a nice box :)
        GUI.DrawTexture(new Rect(740, 5, 10, 50), m_barEnd);
        GUI.DrawTexture(new Rect(750, 5, 100, 50), m_barMid);
        GUI.DrawTexture(new Rect(860, 5, -10, 50), m_barEnd);
        //TODO: Uncomment this when split
        //int seconds2 = (int)m_gameTimer;
        //string displayedTime = string.Format("{0:00}:{1:00}", (seconds2 / 60) % 60, seconds2 % 60);
        //GUI.Label(new Rect(760, 10, 80, 40), displayedTime, m_nonBoxStyle);
        
        //Border
        GUI.DrawTexture(new Rect(0, 650, 1600, 250), m_SpecBottomPanel);
        
        //CShip: show a cship picture, and it's HP/Shield. Bottom-middle
        if (m_cShipGameObject)
        {
            GUI.DrawTexture(new Rect(640, 770, 340, 100), m_SpecCShipImage);
            float hpPercent = m_cShipHealth.GetHPPercentage();
            hpPercent = Mathf.Max(0, hpPercent);
            float shieldPercent = m_cShipHealth.GetShieldPercentage();
            shieldPercent = Mathf.Max(0, shieldPercent);
            
            GUI.DrawTexture(new Rect(570, 680, 460, 60), m_healthBackground);
            GUI.DrawTextureWithTexCoords(new Rect(570, 680, 460 * hpPercent, 60), m_healthBar, new Rect(0, 0, hpPercent, 1));
            GUI.DrawTextureWithTexCoords(new Rect(570, 680, 460 * shieldPercent, 60), m_shieldBar, new Rect(0, 0, shieldPercent, 1));
        }
        
        //Players: show the player's name, hp/shield and equipment icons. Bottom line, left, left mid, right mid & right
        //List<DeadPlayer> deadPlayers = m_gameStateController.GetComponent<GameStateController>().GetDeadPlayers();
        for (int i = 0; i < m_players.Length; i++)
        {
            if (m_players[i] != null)
            {
                if (i < 2)
                {
                    //Left side
                    //Name
                    string name = m_gameStateController.GetComponent<GameStateController>().GetNameFromID(i);
                    GUI.Label(new Rect(20 + (i * 270), 700, 240, 40), name, m_nonBoxStyle);
                    
                    if (i == m_trackedPlayerID)
                    {
                        GUI.DrawTexture(new Rect(20 + (i * 270), 695, 20, 60), m_barEnd);
                        GUI.DrawTexture(new Rect(40 + (i * 270), 695, 200, 60), m_barMid);
                        GUI.DrawTexture(new Rect(260 + (i * 270), 695, -20, 60), m_barEnd);
                    }
                    
                    //Health
                    float hpPercent = m_players[i].GetComponent<HealthScript>().GetHPPercentage();
                    hpPercent = Mathf.Max(0, hpPercent);
                    float shieldPercent = m_players[i].GetComponent<HealthScript>().GetShieldPercentage();
                    shieldPercent = Mathf.Max(0, shieldPercent);
                    GUI.DrawTexture(new Rect(20 + (i * 270), 760, 240, 45), m_healthBackground);
                    GUI.DrawTextureWithTexCoords(new Rect(20 + (i * 270), 760, 240 * hpPercent, 45), m_healthBar, new Rect(0, 0, hpPercent, 1));
                    GUI.DrawTextureWithTexCoords(new Rect(20 + (i * 270), 760, 240 * shieldPercent, 45), m_shieldBar, new Rect(0, 0, shieldPercent, 1));
                    
                    //Equipment
                    Texture weapTex = m_players[i].GetComponent<PlayerControlScript>().GetEquipedWeaponItem().GetComponent<ItemScript>().GetIcon();
                    Texture shieldTex = m_players[i].GetComponent<PlayerControlScript>().GetEquipedShieldItem().GetComponent<ItemScript>().GetIcon();
                    Texture armourTex = m_players[i].GetComponent<PlayerControlScript>().GetEquipedPlatingItem().GetComponent<ItemScript>().GetIcon();
                    Texture engineTex = m_players[i].GetComponent<PlayerControlScript>().GetEquipedEngineItem().GetComponent<ItemScript>().GetIcon();
                    
                    GUI.DrawTexture(new Rect(20 + (i * 270), 820, 49, 55), weapTex);
                    GUI.DrawTexture(new Rect(84 + (i * 270), 820, 49, 55), shieldTex);
                    GUI.DrawTexture(new Rect(148 + (i * 270), 820, 49, 55), armourTex);
                    GUI.DrawTexture(new Rect(212 + (i * 270), 820, 49, 55), engineTex);
                }
                else
                {
                    //Right side
                    //Name
                    string name = m_gameStateController.GetComponent<GameStateController>().GetNameFromID(i);
                    GUI.Label(new Rect(1070 + ((i - 2) * 270), 700, 240, 40), name, m_nonBoxStyle);
                    
                    if (i == m_trackedPlayerID)
                    {
                        GUI.DrawTexture(new Rect(1070 + ((i - 2) * 270), 695, 20, 60), m_barEnd);
                        GUI.DrawTexture(new Rect(1090 + ((i - 2) * 270), 695, 200, 60), m_barMid);
                        GUI.DrawTexture(new Rect(1310 + ((i - 2) * 270), 695, -20, 60), m_barEnd);
                    }
                    
                    //Health
                    float hpPercent = m_players[i].GetComponent<HealthScript>().GetHPPercentage();
                    float shieldPercent = m_players[i].GetComponent<HealthScript>().GetShieldPercentage();
                    GUI.DrawTexture(new Rect(1070 + ((i - 2) * 270), 760, 240, 45), m_healthBackground);
                    GUI.DrawTextureWithTexCoords(new Rect(1070 + ((i - 2) * 270), 760, 240 * hpPercent, 45), m_healthBar, new Rect(0, 0, hpPercent, 1));
                    GUI.DrawTextureWithTexCoords(new Rect(1070 + ((i - 2) * 270), 760, 240 * shieldPercent, 45), m_shieldBar, new Rect(0, 0, shieldPercent, 1));
                    
                    //Equipment
                    Texture weapTex = m_players[i].GetComponent<PlayerControlScript>().GetEquipedWeaponItem().GetComponent<ItemScript>().GetIcon();
                    Texture shieldTex = m_players[i].GetComponent<PlayerControlScript>().GetEquipedShieldItem().GetComponent<ItemScript>().GetIcon();
                    Texture armourTex = m_players[i].GetComponent<PlayerControlScript>().GetEquipedPlatingItem().GetComponent<ItemScript>().GetIcon();
                    Texture engineTex = m_players[i].GetComponent<PlayerControlScript>().GetEquipedEngineItem().GetComponent<ItemScript>().GetIcon();
                    
                    GUI.DrawTexture(new Rect(1070 + ((i - 2) * 270), 820, 49, 55), weapTex);
                    GUI.DrawTexture(new Rect(1134 + ((i - 2) * 270), 820, 49, 55), shieldTex);
                    GUI.DrawTexture(new Rect(1198 + ((i - 2) * 270), 820, 49, 55), armourTex);
                    GUI.DrawTexture(new Rect(1262 + ((i - 2) * 270), 820, 49, 55), engineTex);
                }
            }
            else
            {
                //Catch if they've respawned:
                m_players[i] = m_gameStateController.GetComponent<GameStateController>().GetPlayerFromNetworkPlayer(m_gameStateController.GetComponent<GameStateController>().GetNetworkPlayerFromID(i));
                
                if (m_players[i] == null)
                {
                    //If they're still dead, show death screen
                    if (i < 2)
                    {
                        //Name
                        string name = m_gameStateController.GetComponent<GameStateController>().GetNameFromID(i);
                        GUI.Label(new Rect(20 + (i * 270), 700, 240, 40), name, m_nonBoxStyle);
                        
                        //Dead
                        GUI.Label(new Rect(20 + (i * 270), 760, 240, 45), "DESTROYED", m_nonBoxStyle);
                        
                        //Respawntime
                        GameStateController gsc = m_gameStateController.GetComponent<GameStateController>();
                        float timer = gsc.GetDeathTimerFromNetworkPlayer(gsc.GetNetworkPlayerFromID(i));
                        GUI.Label(new Rect(20 + (i * 270), 820, 240, 55), "Respawn in: " + System.Math.Round(timer, System.MidpointRounding.AwayFromZero), m_nonBoxStyle);
                    }
                    else
                    {
                        //Name
                        string name = m_gameStateController.GetComponent<GameStateController>().GetNameFromID(i);
                        GUI.Label(new Rect(1070 + ((i - 2) * 270), 700, 240, 40), name, m_nonBoxStyle);
                        
                        //Dead
                        GUI.Label(new Rect(1070 + ((i - 2) * 270), 760, 240, 45), "DESTROYED", m_nonBoxStyle);
                        
                        //Respawntime
                        GameStateController gsc = m_gameStateController.GetComponent<GameStateController>();
                        float timer = gsc.GetDeathTimerFromNetworkPlayer(gsc.GetNetworkPlayerFromID(i));
                        GUI.Label(new Rect(1070 + ((i - 2) * 270), 820, 240, 55), "Respawn in: " + System.Math.Round(timer, System.MidpointRounding.AwayFromZero), m_nonBoxStyle);
                    }
                }
            }
        }
        
        //if (m_isOnMap)
            //DrawMap();
    }
    
    void DrawInGame()
    {
        if (m_isSpecMode)
        {
            DrawInGameSpec();
            return;
        }
        
        if (!m_PlayerHasDied && m_thisPlayerHP != null && m_thisPlayerHP.gameObject != null)
        {
            if (m_PlayerHasDockedAtCapital)
            {
                DrawCShipDockOverlay();
            }
            else
            {
                if (m_cShipGameObject != null)
                {
                    //If we're near the capital ship, display 'press x to dock'
                    //Instead of being 'near' the CShip, be near an area to the right of the CShhip (IE, the dock)
                    
                    Vector3 distance = m_thisPlayerHP.gameObject.transform.position - (m_cShipGameObject.transform.position + (m_cShipGameObject.transform.right * 10.0f));
                    if (distance.magnitude <= 7.5f)
                    {
                        GUI.Label(new Rect(700, 750, 200, 100), "Press X to dock");
                        m_thisPlayerHP.gameObject.GetComponent<PlayerControlScript>().SetIsInRangeOfCapitalDock(true);
                    }
                    else
                    {
                        m_thisPlayerHP.gameObject.GetComponent<PlayerControlScript>().SetIsInRangeOfCapitalDock(false);
                    }
                }
            }
            
            if (m_PlayerHasDockedAtShop && !m_PlayerHasDied)
            {
                
            }
            else
            {
                GameObject shop = GetClosestShop();
                if (shop != null)
                {
                    Vector3 shopDockPoint = shop.GetComponent<ShopScript>().GetDockPoint();
                    float distance = Vector3.Distance(shopDockPoint, m_thisPlayerHP.transform.position);
                    if (distance < 1.5f)
                    {
                        GUI.Label(new Rect(700, 750, 200, 100), "Press X to dock at trading station");
                        m_thisPlayerHP.gameObject.GetComponent<PlayerControlScript>().SetIsInRangeOfTradingDock(true);
                        m_thisPlayerHP.gameObject.GetComponent<PlayerControlScript>().SetNearbyShop(shop);
                    }
                    else
                    {
                        m_thisPlayerHP.gameObject.GetComponent<PlayerControlScript>().SetIsInRangeOfTradingDock(false);
                    }
                }
            }
        }
        
        //Warning
        if (m_shouldShowWarningAttack)
        {
            GUI.Label (new Rect(1205, 130, 220, 44), "Capital ship under attack!", m_sharedGUIStyle);
        }
        
        if (m_shopResetDisplayTimer < 7.5f)
        {
            GUI.Label (new Rect(175, 130, 220, 44), "Shops have restocked!", m_sharedGUIStyle);
        }

        if (m_PlayerHasDied && !m_cshipDying)
        {
            GUI.DrawTexture(new Rect(650, 100, 300, 250), m_menuBackground);
            GUI.Label(new Rect(700, 130, 200, 80), "You have been destroyed", m_nonBoxBigStyle);
            
            if (m_noRespawnCash)
            {
                GUI.Label(new Rect(675, 200, 250, 80), "Not enough banked cash to respawn! You need $500.", m_nonBoxStyle);
            }
            else
            {
                GUI.Label(new Rect(700, 200, 200, 80), "Respawn in: " + System.Math.Round(m_deathTimer, System.MidpointRounding.AwayFromZero), m_nonBoxStyle);
            }
        }
        
        if (m_inGameMenuIsOpen)
        {
            DrawInGameMenu();
        }
        else
        {
            //Splash screens
            if (m_shouldShowVictorySplash)
            {
                GUI.Box(new Rect(400, 100, 800, 700), "");
                GUI.Label(new Rect(700, 130, 200, 80), "Victory!");
                GUI.Label(new Rect(700, 200, 200, 80), "The capital ship survives another sector");
            }
            else if (m_shouldShowLossSplash)
            {
                //GUI.Box(new Rect(400, 100, 800, 700), "");
                GUI.DrawTexture(new Rect(600, 100, 400, 400), m_menuBackground);
                GUI.Label(new Rect(700, 130, 200, 80), "Defeat!", m_nonBoxBigStyle);
                GUI.Label(new Rect(700, 200, 200, 80), "The capital ship was destroyed", m_nonBoxStyle);
                
                Time.timeScale = 0.0f;
                //TODO: Uncomment this when split
                //int seconds = (int)m_gameTimer;
                //string displayedTime2 = string.Format("{0:00}:{1:00}", (seconds / 60) % 60, seconds % 60);
                //GUI.Label(new Rect(700, 300, 200, 80), "Final time: " + displayedTime2, m_nonBoxStyle);
                
                if (GUI.Button(new Rect(750, 400, 100, 80), "Restart", m_sharedGUIStyle))
                {
                    Time.timeScale = 1.0f;
                    Network.Disconnect();
                    Application.LoadLevel(0);
                }
            }
            
            //Map screen
            /*if (m_isOnMap)
                DrawMap();
            else
            {
                if (m_isOnFollowMap)
                    DrawSmallFollowMap();
                else
                {
                    DrawSmallMap();
                }
            }*/
            
            //Event
            if (m_eventIsActive)
            {
                DrawEventScreen();
            }
            
            //Overlay if OoB
            if (isOoBCountingDown)
            {
                GUI.DrawTexture(new Rect(600, 350, 400, 200), m_menuBackground);
                GUI.Label(new Rect(650, 400, 300, 50), "You are leaving the sector, turn back!", m_nonBoxStyle);
                GUI.Label(new Rect(750, 500, 100, 50), System.Math.Round(m_outOfBoundsTimer, System.MidpointRounding.AwayFromZero).ToString(), m_nonBoxStyle);
            }
            
            if (!Screen.showCursor)
                DrawCursor();
            
            //After everythings else, check to see if we can do lockon stuff
            if (m_currentWeaponNeedsLockon)
            {
                if (m_lastLockonTarget == null && !m_isLockingOn)
                {
                    //Raycast from cursor pos, if we find a target begin lockon phase
                    RaycastHit info;
                    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                    int mask = (1 << 11 | 1 << 24);
                    if (Physics.Raycast(ray, out info, 200, mask))
                    {
                        m_lastLockonTarget = info.collider.attachedRigidbody.gameObject;
                        m_isLockingOn = true;
                        m_lockOnTime = 0.0f;
                    }
                }
                else if (m_isLockingOn && !m_hasLockedTarget)
                {
                    RaycastHit info;
                    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                    int mask = (1 << 11 | 1 << 24);
                    if (Physics.Raycast(ray, out info, 200, mask))
                    {
                        if (info.collider.attachedRigidbody.gameObject != m_lastLockonTarget)
                        {
                            m_lastLockonTarget = info.collider.attachedRigidbody.gameObject;
                            m_isLockingOn = true;
                            m_lockOnTime = 0.0f;
                            //m_lockOffTime = 0.0f;
                        }
                    }
                    else
                    {
                        m_isLockingOn = false;
                        m_lastLockonTarget = null;
                        m_lockOnTime = 0.0f;
                        //m_lockOffTime = 0.0f;
                    }
                }
                
                if (m_hasLockedTarget)
                {
                    if (m_beginLockBreak)
                    {
                        //See if the player maintains the lock
                        RaycastHit info;
                        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                        int mask = (1 << 11 | 1 << 24);
                        if (Physics.Raycast(ray, out info, 200, mask))
                        {
                            if (info.collider.attachedRigidbody.gameObject == m_lastLockonTarget)
                            {
                                m_beginLockBreak = false;
                                //m_lockOffTime = 0.0f;
                            }
                        }
                    }
                    else
                    {
                        //See if we should begin lock break
                        RaycastHit info;
                        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                        int mask = (1 << 11 | 1 << 24);
                        if (Physics.Raycast(ray, out info, 200, mask))
                        {
                            if (info.collider.attachedRigidbody.gameObject != m_lastLockonTarget)
                            {
                                m_beginLockBreak = true;
                            }
                        }
                        else
                        {
                            m_beginLockBreak = true;
                        }
                    }
                }
            }
        }
        
        
    }
    
    void DrawCShipDockOverlay()
    {
        
    }
    
    void DrawInGameMenu()
    {
        
    }
    
    void DrawCursor()
    {
        if (m_thisPlayerHP != null)
        {
            //Cursor
            Matrix4x4 oldMat = GUI.matrix;
            GUI.matrix = Matrix4x4.identity;
            Vector3 mousePos = Input.mousePosition;
            if (m_hasLockedTarget)
            {
                GUI.DrawTexture(new Rect(mousePos.x - 20, (Screen.height - mousePos.y) - 20, 40, 40), m_cursorLocked);
                
                //Draw locked target over the enemy
                if (m_lastLockonTarget != null)
                {
                    Vector2 pos = Camera.main.WorldToScreenPoint(m_lastLockonTarget.transform.position);
                    pos.y = Screen.height - pos.y;
                    GUI.DrawTexture(new Rect((pos.x - 15), (pos.y - 15), 30, 30), m_enemyLocked);
                }
                else
                    m_hasLockedTarget = false;
            }
            else if (m_isLockingOn)
                GUI.DrawTexture(new Rect(mousePos.x - 20, (Screen.height - mousePos.y) - 20, 40, 40), m_cursorLocking);
            else
                GUI.DrawTexture(new Rect(mousePos.x - 20, (Screen.height - mousePos.y) - 20, 40, 40), m_cursor);
            
            //Reload on cursor
            Rect reloadBoxPos = new Rect(mousePos.x + 12, (Screen.height - mousePos.y), 11, 18);
            GUI.DrawTexture(reloadBoxPos, m_reloadBackground);
            
            //Draw reload percentage
            float reloadPercent = m_thisPlayerHP.GetComponent<PlayerControlScript>().GetReloadPercentage();
            reloadBoxPos.width *= reloadPercent;
            GUI.DrawTextureWithTexCoords(reloadBoxPos, m_reloadBar, new Rect(0, 0, reloadPercent, 1.0f));
            
            GUI.matrix = oldMat;
        }
    }
    
    
    
    
    
    void DrawEventScreen()
    {
        //Draw an outline box
        GUI.Box(new Rect(400, 100, 800, 700), "EVENT - Triggered by " + eventTriggerer);
        
        if (m_eventIsOnOutcome)
        {
            GUI.Label(new Rect(550, 200, 500, 300), eventText);
            GUI.Label(new Rect(550, 150, 500, 100), "Time remaining: " + System.Math.Round(m_continueTimer, 0));
            
            if (GUI.Button(new Rect(450, 400, 700, 80), "Continue"))
            {
                if (!m_hasContinued)
                {
                    if (Network.isServer)
                    {
                        m_hasContinued = true;
                        VoteForContinue();
                    }
                    else
                    {
                        m_hasContinued = true;
                        networkView.RPC("VoteForContinue", RPCMode.Server);
                    }
                }
            }
        }
        else if (m_eventIsOnPlayerSelect)
        {
            GUI.Label(new Rect(550, 200, 500, 300), eventText);
            GUI.Label(new Rect(550, 150, 500, 100), "Time remaining: " + System.Math.Round(m_playerSelectTimer, 0));
            
            //Draw a button for each player here, then pass votes to host
            if (m_hostIsTieBreaking)
            {
                GUI.Label(new Rect(450, 400, 700, 80), "Host is tiebreaking!");
            }
            else
            {
                List<Player> players = m_gameStateController.GetComponent<GameStateController>().GetConnectedPlayers();
                for (int i = 0; i < players.Count; i++)
                {
                    if (GUI.Button(new Rect(450, 400 + (i * 100), 700, 80), players[i].m_name + " #" + m_playerVotes[i]))
                    {
                        if (m_hostShouldSelectTiebreaker)
                        {
                            m_selectedPlayerName = m_gameStateController.GetComponent<GameStateController>().GetNameFromID(i);
                            GameObject player = m_gameStateController.GetComponent<GameStateController>().GetPlayerFromNetworkPlayer(players[i].m_netPlayer);
                            OnPlayerSelected(player);
                        }
                        else
                        {
                            if (Network.isServer)
                            {
                                VoteForPlayer(players[i].m_name, m_lastVote);
                                m_lastVote = players[i].m_name;
                            }
                            else
                            {
                                networkView.RPC("VoteForPlayer", RPCMode.Server, players[i].m_name, m_lastVote);
                                m_lastVote = players[i].m_name;
                            }
                        }
                    }
                }
            }
        }
        else
        {
            //Display event text
            GUI.Label(new Rect(550, 200, 500, 300), eventText);
            GUI.Label(new Rect(550, 150, 500, 100), "Time remaining: " + System.Math.Round(currEventSc.GetTimer(), 0));
            
            //Draw each of the buttons
            for (int i = 0; i < currEventSc.GetPossibleOptions().Length; i++)
            {
                if (m_hostIsTieBreaking)
                {
                    GUI.Label(new Rect(450, 400, 700, 80), "Host is tiebreaking!");
                }
                else
                {
                    //If a button is clicked, activate the relevent option (or vote for it)
                    if (m_hostShouldSelectTiebreaker)
                    {
                        if (GUI.Button(new Rect(450, 400 + (i * 100), 700, 80), currEventSc.GetPossibleOptions()[i].optionText + ": #" + currEventSc.GetOptionVotes()[i]))
                        {
                            eventText = currEventSc.ActivateOption(i);
                            m_eventIsOnOutcome = true;
                            m_hostShouldSelectTiebreaker = false;
                        }
                    }
                    else
                    {
                        if (GUI.Button(new Rect(450, 400 + (i * 100), 700, 80), currEventSc.GetPossibleOptions()[i].optionText + ": #" + currEventSc.GetOptionVotes()[i]))
                        {
                            currEventSc.VoteForOption(i);
                        }
                    }
                }
            }
        }
    }
    #endregion
    
    // Non-GUI funcs
    void PingForEnemies()
    {
        m_pingedEnemies = GameObject.FindGameObjectsWithTag("Enemy");
		m_pingedMissiles = GetAllDestructibleMissiles();
        m_currentPingTime = 0;
    }

	GameObject[] GetAllDestructibleMissiles()
	{
		GameObject[] temp = GameObject.FindGameObjectsWithTag("Bullet");
		List<GameObject> output = new List<GameObject>();

		for(int i = 0; i < temp.Length; i++)
		{
			if(temp[i].layer == Layers.enemyDestructibleBullet)
			{
				output.Add(temp[i]);
			}
		}

		return output.ToArray();
	}

    [RPC] void PropagateRestockShops()
    {
        m_shopResetDisplayTimer = 0.0f;
    }

    bool CheckPlayerTargetDistanceOver()
    {
        if (m_thisPlayerHP)
        {
            return ((m_thisPlayerHP.transform.position - m_lastLockonTarget.transform.position).sqrMagnitude >
                    (m_thisPlayerHP.GetComponent<PlayerWeaponScript>().FindAttachedWeapon().GetComponent<EquipmentWeapon>().GetBulletMaxDistance() * 0.5f).Squared());
        }

        return false;
    }

    void AttemptGoBackControllerPress()
    {
        switch (m_currentGameState)
        {
            case GameState.MainMenu:
                {
                    m_selectedButton = 4;
                    break;
                }
            case GameState.HostMenu:
                {
                    HostMenuBackActivate();
                    break;
                }
            case GameState.AttemptingConnect:
                {
                    ClientConnectingBackActivate();
                    break;
                }
            case GameState.ClientInputIP:
                {
                    ClientConnectBackActivate();
                    break;
                }
            case GameState.OptionMenu:
                {
                    m_gameStateController.GetComponent<GameStateController>().CloseOptionMenu();
                    break;
                }
        }
    }
    
    /* TEMP: THESE WILL BE REMOVED */
    void ClientConnectBackActivate()
    {
    
    }
    void HostButtonActivate()
    {
    
    }
    void JoinButtonActivate()
    {
    
    }
    bool m_useController = false;
    void ClientConnectJoinActivate()
    {
    
    }
    
    void ActivateMenuControllerPress()
    {
        switch (m_currentGameState)
        {
            case GameState.MainMenu:
                {
                    switch (m_selectedButton)
                    {
                        case 1:
                            {
                                HostButtonActivate();
                                break;
                            }
                        case 2:
                            {
                                JoinButtonActivate();
                                break;
                            }
                        case 3:
                            {
                                m_gameStateController.GetComponent<GameStateController>().OpenOptionMenu();
                                break;
                            }
                        case 4:
                            {
                                Application.Quit();
                                break;
                            }
                    }
                    break;
                }
            case GameState.HostMenu:
                {
                    switch (m_selectedButton)
                    {
                        case 1:
                            {
                                if (m_selectedSubButton == 0)
                                    HostMenuStartButtonActivate();
                                else
                                    m_hostShouldStartSpec = !m_hostShouldStartSpec;
                                break;
                            }
                        case 2:
                            {
                                HostMenuBackActivate();
                                break;
                            }
                    }
                    break;
                }
            case GameState.OptionMenu:
                {
                    switch (m_selectedButton)
                    {
                        case 1:
                            {
                                //Resolution, ignore for now
                                break;
                            }
                        case 2:
                            {
                                //Fullscreen, ignore for now
                                break;
                            }
                        case 3:
                            {
                                //Apply, ignore for now
                                break;
                            }
                        case 4:
                            {
                                m_useController = !m_useController;
                                break;
                            }
                        case 5:
                            {
                                //Music, do nothing
                                break;
                            }
                        case 6:
                            {
                                //Sound, do nothing
                                break;
                            }
                    }
                    break;
                }
            case GameState.ClientInputIP:
                {
                    switch (m_selectedButton)
                    {
                        case 1:
                            {
                                //Input keyboard field here
                                break;
                            }
                        case 2:
                            {
                                ClientConnectJoinActivate();
                                break;
                            }
                        case 3:
                            {
                                ClientConnectBackActivate();
                                break;
                            }
                    }
                    break;
                }
            case GameState.ClientMenu:
                {
                    m_selectedButton = 0;
                    m_maxButton = 1;
                    break;
                }
            case GameState.AttemptingConnect:
                {
                    switch (m_selectedButton)
                    {
                        case 1:
                            {
                                ClientConnectingBackActivate();
                                break;
                            }
                    }
                    break;
                }
            case GameState.FailedConnectName:
                {
                    m_selectedButton = 0;
                    m_maxButton = 1;
                    break;
                }
        }
    }
    void ScrollUpMenu()
    {
        m_selectedButton--;
        if (m_selectedButton <= 0)
            m_selectedButton = m_maxButton;
    }
    void ScrollDownMenu()
    {
        m_selectedButton++;
        if (m_selectedButton > m_maxButton)
            m_selectedButton = 1;
    }
    void ScrollLeftMenu()
    {
        if (m_currentGameState == GameState.OptionMenu)
        {
            if (m_selectedButton == 5)
            {
                float music = PlayerPrefs.GetFloat("MusicVolume") - 0.05f;
                if (music < 0.0f)
                    music = 0.0f;
                PlayerPrefs.SetFloat("MusicVolume", music);
            }
            else if (m_selectedButton == 6)
            {
                float effect = PlayerPrefs.GetFloat("EffectVolume") - 0.05f;
                if (effect < 0.0f)
                    effect = 0.0f;
                PlayerPrefs.SetFloat("EffectVolume", effect);
            }
        }
        else
        {
            m_selectedSubButton--;
            if (m_selectedSubButton < 0)
                m_selectedSubButton = m_maxSubButton;
        }
        
    }
    void ScrollRightMenu()
    {
        if (m_currentGameState == GameState.OptionMenu)
        {
            if (m_selectedButton == 5)
            {
                float music = PlayerPrefs.GetFloat("MusicVolume") + 0.05f;
                if (music > 1.0f)
                    music = 1.0f;
                PlayerPrefs.SetFloat("MusicVolume", music);
            }
            else if (m_selectedButton == 6)
            {
                float effect = PlayerPrefs.GetFloat("EffectVolume") + 0.05f;
                if (effect > 1.0f)
                    effect = 1.0f;
                PlayerPrefs.SetFloat("EffectVolume", effect);
            }
        }
        else
        {
            m_selectedSubButton++;
            if (m_selectedSubButton > m_maxSubButton)
                m_selectedSubButton = 0;
        }
    }

    public void RequestBreakLock()
    {
        m_hasLockedTarget = false;
        m_lastLockonTarget = null;
        m_lockOnTime = 0.0f;
        m_isLockingOn = false;
        m_thisPlayerHP.GetComponent<PlayerControlScript>().UnsetTargetLock();
    }
    
    public void AlertGUIPlayerHasRespawned()
    {
        m_PlayerHasDied = false;
        networkView.RPC("PropagateObtainPlayerShips", RPCMode.All);
    }
    
    [RPC] void PropagateObtainPlayerShips()
    {
        m_playerShips = GameObject.FindGameObjectsWithTag("Player");
    }
    
    public void AlertGUINoMoneyToRespawn(NetworkPlayer player)
    {
        Debug.Log("Alerting player '" + m_gameStateController.GetComponent<GameStateController>().GetNameFromNetworkPlayer(player) + "' that no money is available to respawn now.");
        if (player == Network.player)
        {
            PropagateMoneyToRespawn (true);
        }
        
        else
        {
            networkView.RPC("PropagateMoneyToRespawn", player, true);
        }
    }
    [RPC] void PropagateMoneyToRespawn(bool value)
    {
        m_noRespawnCash = value;
    }
    public void AlertGUIMoneyToRespawn(NetworkPlayer player)
    {
        Debug.Log("Alerting player '" + m_gameStateController.GetComponent<GameStateController>().GetNameFromNetworkPlayer(player) + "' that money is available to respawn now.");
        
        if (player == Network.player)
        {
            PropagateMoneyToRespawn (false);
        }
        
        else
        {
            networkView.RPC("PropagateMoneyToRespawn", player, false);
        }
    }
    
    public bool GetNoRespawnCash()
    {
        return m_noRespawnCash;
    }
    
    

    void HostMenuStartButtonActivate()
    {
        m_gameStateController.GetComponent<GameStateController>().StartGameFromMenu(m_hostShouldStartSpec);
        AskServerToBeginSpawns();
    }
    void HostMenuBackActivate()
    {
        m_gameStateController.GetComponent<GameStateController>().BackToMenu();
        m_gameStateController.GetComponent<GameStateController>().WipeConnectionInfo();
        Network.Disconnect();
        Debug.Log("Closed Server.");
    }

    //Spec mode
    public void BeginSpecModeGameTimer()
    {
        m_isSpecMode = true;
        //TODO: Uncomment this when split
        //m_gameTimer = 0;
        GameStateController gsc = m_gameStateController.GetComponent<GameStateController>();
        List<Player> playersL = gsc.GetConnectedPlayers();
        m_players = new GameObject[playersL.Count];
        for (int i = 0; i < playersL.Count; i++)
        {
            m_players[i] = gsc.GetPlayerFromNetworkPlayer(gsc.GetNetworkPlayerFromID(i));
        }
    }
    public void RecieveActivePlayerSpec(int player)
    {
        m_trackedPlayerID = player;
    }
    
    
   
    void ClientConnectingBackActivate()
    {
        m_gameStateController.GetComponent<GameStateController>().BackToMenu();
        m_gameStateController.GetComponent<GameStateController>().WipeConnectionInfo();
        Network.Disconnect();
    }

    public void AlertGUIPlayerHasDied()
    {
        m_PlayerHasDied = true;
        m_PlayerHasDockedAtShop = false;

        //Also begin respawn countdown locally for GUI-ness
        m_deathTimer = 45.0f;
    }

    [RPC] void AskServerToBeginSpawns()
    {
        if (Network.isServer)
            m_gameStateController.GetComponent<GameStateController>().AlertGameControllerBeginSpawning();
    }

    public void ToggleMenuState()
    {
        m_inGameMenuIsOpen = !m_inGameMenuIsOpen;

        if (m_inGameMenuIsOpen)
        {
            Screen.showCursor = true;
            if (m_thisPlayerHP != null)
                m_thisPlayerHP.GetComponent<PlayerControlScript>().TellShipStopRecievingInput();
        }
        else
        {
            if (!m_PlayerHasDockedAtCapital && !m_PlayerHasDockedAtShop)
            {
                Screen.showCursor = false;
                if (m_thisPlayerHP != null)
                    m_thisPlayerHP.GetComponent<PlayerControlScript>().TellShipStartRecievingInput();
            }

        }
    }

    public int GetMapStatus()
    {
        if (m_isOnMap)
            return 2;
        else if (!m_isOnFollowMap)
            return 1;
        else
            return 0;
    }
    public void ToggleMap()
    {
        m_isOnMap = !m_isOnMap;
    }
    public void CloseMap()
    {
        m_isOnMap = false;
    }
   
    
    
    

	bool IsMouseDownZero()
	{
		return !m_previousMouseZero && m_mouseZero;
	}

	bool IsMouseUpZero()
	{
		return m_previousMouseZero && !m_mouseZero;
	}

    

	

    


	// WARNING! EXTREMELY HAZARDOUS CODE LIES BEYOND THIS POINT! DO NOT LOOK AT THIS FUNCTION OR YOU MAY TURN BLIND!
    

    

    public void SetActiveEvent(GameObject currEvent, NetworkPlayer causer)
    {
        //Set up event vars
        m_eventIsActive = true;
        m_eventIsOnOutcome = false;
        currEventSc = currEvent.GetComponent<EventScript>();
        eventText = currEventSc.GetEventText();
        eventTriggerer = m_gameStateController.GetComponent<GameStateController>().GetNameFromNetworkPlayer(causer);

        //Freeze all the baddies
        FreezeAllEnemies();

        //Freeze player control
        m_thisPlayerHP.gameObject.GetComponent<PlayerControlScript>().TellShipStopRecievingInput();
        m_thisPlayerHP.gameObject.rigidbody.isKinematic = true;

        //Stop CShip from moving
        m_cShipGameObject.GetComponent<CapitalShipScript>().SetShouldStart(false);
        m_cShipGameObject.rigidbody.constraints = RigidbodyConstraints.FreezePosition | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY;

        //Stop spawners from spawning
        m_gameStateController.GetComponent<GameStateController>().RequestSpawnerPause();

        //Init #votes required
        m_playerVotes = new int[m_gameStateController.GetComponent<GameStateController>().GetConnectedPlayers().Count];
    }
    
    void OnEventComplete()
    {
        //Restore player control
        m_thisPlayerHP.gameObject.GetComponent<PlayerControlScript>().TellShipStartRecievingInput();
        m_thisPlayerHP.gameObject.rigidbody.isKinematic = false;

        //Unfreeze baddies
        UnfreezeAllEnemies();

        //Unfreeze CShip
        m_cShipGameObject.rigidbody.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY;
        m_cShipGameObject.GetComponent<CapitalShipScript>().SetShouldStart(true);

        //Purge event vars
        m_eventIsActive = false;
        m_eventIsOnOutcome = false;
        m_eventIsOnPlayerSelect = false;
        m_hostShouldSelectTiebreaker = false;
        m_continueTimer = 10.0f;
        continueVotes = 0;
        m_playerSelectTimer = 10.0f;
        m_hasContinued = false;

        //Tell spawners to continue spawning
        m_gameStateController.GetComponent<GameStateController>().RequestSpawnerUnPause();
    }

    [RPC] void PropagatePlayerVotes(string player, int votes)
    {
        int id = m_gameStateController.GetComponent<GameStateController>().GetIDFromName(player);
        m_playerVotes[id] = votes;
    }
    [RPC] void VoteForPlayer(string player, string previous, NetworkMessageInfo info)
    {
        string sender = m_gameStateController.GetComponent<GameStateController>().GetNameFromNetworkPlayer(info.sender);
        
        if (previous != "")
        {
            int prevID = m_gameStateController.GetComponent<GameStateController>().GetIDFromName(previous);
            m_playerVotes[prevID]--;
        }

        Debug.Log("Player " + sender + " voted for player " + player + ".");
        int id = m_gameStateController.GetComponent<GameStateController>().GetIDFromName(player);
        m_playerVotes[id]++;

        for (int i = 0; i < m_playerVotes.Length; i++)
        {
            networkView.RPC("PropagatePlayerVotes", RPCMode.Others, m_gameStateController.GetComponent<GameStateController>().GetNameFromID(i), m_playerVotes[i]);
        }

        CheckIfPlayerVotesAreOverHalf();
    }
    void VoteForPlayer(string player, string previous)
    {
        Debug.Log("Host voted for player " + player + ".");

        if (previous != null && previous != "")
        {
            int prevID = m_gameStateController.GetComponent<GameStateController>().GetIDFromName(previous);
            m_playerVotes[prevID]--;
        }

        int id = m_gameStateController.GetComponent<GameStateController>().GetIDFromName(player);
        m_playerVotes[id]++;

        for (int i = 0; i < m_playerVotes.Length; i++)
        {
            networkView.RPC("PropagatePlayerVotes", RPCMode.Others, m_gameStateController.GetComponent<GameStateController>().GetNameFromID(i), m_playerVotes[i]);
        }

        CheckIfPlayerVotesAreOverHalf();
    }

    void CheckIfPlayerVotesAreOverHalf()
    {
        int numPlayers = m_gameStateController.GetComponent<GameStateController>().GetConnectedPlayers().Count;
        float rawHalf = (float)numPlayers / 2.0f;

        for (int i = 0; i < m_playerVotes.Length; i++)
        {
            if (m_playerVotes[i] > rawHalf)
            {
                m_selectedPlayerName = m_gameStateController.GetComponent<GameStateController>().GetNameFromID(i);
                GameObject player = m_gameStateController.GetComponent<GameStateController>().GetPlayerFromNetworkPlayer(m_gameStateController.GetComponent<GameStateController>().GetNetworkPlayerFromID(i));
                OnPlayerSelected(player);
            }
        }
    }

    void CheckMostPlayerVotes()
    {
        int highest = -1;
        bool isTie = false;
        for (int i = 0; i < m_playerVotes.Length; i++)
        {
            if (highest == -1 || m_playerVotes[i] > m_playerVotes[highest])
            {
                highest = i;
            }
            else if (m_playerVotes[i] == m_playerVotes[highest])
            {
                //Tie!
                HostShouldTieBreak();
                isTie = true;
            }
        }

        if (!isTie)
        {
            m_selectedPlayerName = m_gameStateController.GetComponent<GameStateController>().GetNameFromID(highest);
            GameObject player = m_gameStateController.GetComponent<GameStateController>().GetPlayerFromNetworkPlayer(m_gameStateController.GetComponent<GameStateController>().GetNetworkPlayerFromID(highest));
            OnPlayerSelected(player);
        }
    }

    void OnPlayerSelected(GameObject player)
    {
        currEventSc.SetSelectedPlayer(player);
    }

    [RPC] void VoteForContinue(NetworkMessageInfo info)
    {
        continueVotes++;
        Debug.Log("Recieved vote from player: " + info.sender + ", bringing total to: " + continueVotes);
        if (continueVotes >= m_gameStateController.GetComponent<GameStateController>().GetConnectedPlayers().Count)
            networkView.RPC("PropagateContinueComplete", RPCMode.All);
    }
    void VoteForContinue()
    {
        continueVotes++;
        Debug.Log("Recieved vote from host, bringing total to: " + continueVotes);
        if (continueVotes >= m_gameStateController.GetComponent<GameStateController>().GetConnectedPlayers().Count)
            networkView.RPC("PropagateContinueComplete", RPCMode.All);

    }
    [RPC] void PropagateContinueComplete()
    {
        Debug.Log("Continue!");
        OnEventComplete();
    }

    public void HostShouldTieBreak()
    {
        m_hostShouldSelectTiebreaker = true;
        m_hostIsTieBreaking = false;
        networkView.RPC("PropagateHostIsTieBreaking", RPCMode.Others);
    }
    [RPC] void PropagateHostIsTieBreaking()
    {
        m_hostIsTieBreaking = true;
    }
    public void RecievePlayerRequiresSelectingForEvent(string text)
    {
        m_playerVotes = new int[m_gameStateController.GetComponent<GameStateController>().GetConnectedPlayers().Count];
        eventText = text;
        m_playerSelectTimer = 20.0f;
        m_eventIsOnPlayerSelect = true;
        m_lastVote = "";
        networkView.RPC("PropagateEventPlayerSelectionText", RPCMode.Others, text);
    }
    [RPC] void PropagateEventPlayerSelectionText(string text)
    {
        m_playerVotes = new int[m_gameStateController.GetComponent<GameStateController>().GetConnectedPlayers().Count];
        eventText = text;
        m_playerSelectTimer = 20.0f;
        m_eventIsOnPlayerSelect = true;
    }
    public void RecieveEventTextFromEventCompletion(string text)
    {
        if (m_eventIsOnPlayerSelect)
        {
            eventText = m_selectedPlayerName + text;
            m_eventIsOnOutcome = true;
            networkView.RPC("PropagateEventCompletionText", RPCMode.Others, eventText);
        }
        else
        {
            eventText = text;
            m_eventIsOnOutcome = true;
            networkView.RPC("PropagateEventCompletionText", RPCMode.Others, text);
        }
    }
    [RPC] void PropagateEventCompletionText(string text)
    {
        eventText = text;
        m_continueTimer = 10.0f;
        continueVotes = 0;
        m_hasContinued = false;
        m_eventIsOnOutcome = true;
    }

    public void UpdateCurrentState(GameState newState)
    {
        m_currentGameState = newState;

        switch (newState)
        {
            case GameState.InGame:
                {
                    //TODO: Move this out to split stuff
                    //m_gameTimer = 0.0f;
                    m_shops = GameObject.FindGameObjectsWithTag("Shop");
                    m_selectedButton = 0;
                    m_selectedSubButton = 0;
                    m_shouldResetShopsNow = true;
                    break;
                }
            case GameState.MainMenu:
                {
                    m_selectedButton = 0;
                    m_selectedSubButton = 0;
                    m_maxButton = 4;
                    break;
                }
            case GameState.HostMenu:
                {
                    m_selectedButton = 0;
                    m_selectedSubButton = 0;
                    m_maxButton = 3;
                    break;
                }
            case GameState.OptionMenu:
                {
                    m_selectedButton = 0;
                    m_selectedSubButton = 0;
                    m_maxButton = 6;
                    break;
                }
            case GameState.ClientInputIP:
                {
                    m_selectedButton = 0;
                    m_selectedSubButton = 0;
                    m_maxButton = 3;
                    break;
                }
            case GameState.AttemptingConnect:
                {
                    m_selectedButton = 0;
                    m_selectedSubButton = 0;
                    m_maxButton = 1;
                    break;
                }
            case GameState.ClientMenu:
                {
                    m_selectedButton = 0;
                    m_selectedSubButton = 0;
                    m_maxButton = 1;
                    break;
                }
            case GameState.FailedConnectName:
                {
                    m_selectedButton = 0;
                    m_selectedSubButton = 0;
                    m_maxButton = 1;
                    break;
                }
                case GameState.InGameConnectionLost:
                {
                    Screen.showCursor = true;
                    break;
                }
        }
    }

    public void AlertCapitalUnderAttack()
    {
        m_shouldShowWarningAttack = true;
        m_attackWarningTimer = 0;
    }

    public void ShowDisconnectedSplash()
    {
        //m_shouldShowDisconnectedSplash = true;
    }

    public void ShowVictorySplash()
    {
        m_shouldShowVictorySplash = true;
    }

    public void ShowLossSplash()
    {
        //Unset any overlays that would overwrite loss screen
        m_shouldShowVictorySplash = false;
        m_shouldShowWarningAttack = false;
        m_isOnMap = false;
        m_PlayerHasDockedAtCapital = false;

        Screen.showCursor = true;

        m_CShipHasDied = true;
        m_shouldShowLossSplash = true;
        m_isSpecMode = false;
    }

    public void AlertGUIRemotePlayerHasRespawned()
    {
        m_playerShips = GameObject.FindGameObjectsWithTag("Player");
    }

    bool isFirstRound = true;
    public void StartRound()
    {
        if (isFirstRound)
        {
            m_playerShips = GameObject.FindGameObjectsWithTag("Player");
            m_shops = GameObject.FindGameObjectsWithTag("Shop");
            isFirstRound = false;
        }
        //m_ArenaClearOfEnemies = false;
    }

    GameObject GetClosestShop()
    {
        if (m_shops != null)
        {
            float shortestDistance = 999;
            GameObject shortestShop = null;
            foreach (GameObject shop in m_shops)
            {
                float distance = Vector3.Distance(shop.transform.position, m_thisPlayerHP.transform.position);
                if (shortestShop == null || distance < shortestDistance)
                {
                    shortestDistance = distance;
                    shortestShop = shop;
                }
            }
            return shortestShop;
        }
        else
        {
            return null;
        }
    }

    void FreezeAllEnemies()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in enemies)
            enemy.GetComponent<EnemyScript>().TellEnemyToFreeze();
    }
    void UnfreezeAllEnemies()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in enemies)
            enemy.GetComponent<EnemyScript>().AlertEnemyUnFreeze();
    }

    public void BeginOutOfBoundsWarning()
    {
        m_outOfBoundsTimer = 10.0f;
        isOoBCountingDown = true;
    }
    public void StopOutOfBoundsWarning()
    {
        m_outOfBoundsTimer = 10.0f;
        isOoBCountingDown = false;
    }

    public void AlertGUIDeathSequenceBegins()
    {
        m_cshipDying = true;
        Screen.showCursor = false;

        m_PlayerHasDied = false;

        //Make sure we unset all popup bools
        m_isOnMap = false;
        m_PlayerHasDockedAtShop = false;
        m_PlayerHasDockedAtCapital = false;

        m_inGameMenuIsOpen = false;
        m_shouldShowWarningAttack = false;

        //Stop all enemies
        FreezeAllEnemies();

        //Tell the player to stop inputting
		if(m_thisPlayerHP != null)
        	m_thisPlayerHP.GetComponent<PlayerControlScript>().TellShipStopRecievingInput();

        //Now tell the camera to start lerping
        Camera.main.gameObject.GetComponent<CameraScript>().TellCameraBeginDeathSequence();
    }
}
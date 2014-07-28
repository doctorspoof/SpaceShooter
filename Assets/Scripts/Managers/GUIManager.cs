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
    bool m_PlayerRequestsRound = false;
    bool m_ArenaClearOfEnemies = true;
    bool m_PlayerHasDied = false;
    bool m_CShipHasDied = false;
    bool m_PlayerHasDockedAtCapital = false;
    bool m_playerIsSelectingCShipTurret = false;
    bool m_PlayerHasDockedAtShop = false;
    bool m_shouldShowVictorySplash = false;
    bool m_shouldShowLossSplash = false;
    bool m_playerHasAlreadyLeft = false;
    bool m_currentWeaponNeedsLockon = false;
    GameObject m_lastLockonTarget = null;
    bool m_isOnFollowMap = true;
    bool m_shipyardScreen = true;
    bool m_shopConfirmBuy = false;
    ItemScript m_confirmBuyItem = null;
    bool m_isOnMap = false;
    float m_blobSize;
    bool m_shouldShowWarningAttack = false;
    bool m_shouldShowDisconnectedSplash = false;
    bool isOoBCountingDown = false;
    bool m_cshipDying = false;
    
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
    
    // Spec mode versions
    bool m_hostShouldStartSpec = false;
    bool m_isSpecMode = false;
    float m_gameTimer = 0;
    int m_trackedPlayerID = -1;
    public GameObject[] m_players;
    bool m_beginLockBreak = false;
    bool m_isLockingOn = false;
    
    // Enemy radar vars
    float m_nativeWidth = 1600;
    float m_nativeHeight = 900;
    float m_currentPingTime = 0.0f;
    float m_reqPingTime = 10.0f;
    bool m_resoDropdown = false;
    bool m_shouldFullscreen = false;
    Resolution m_newResolution;
    bool m_useController = false;
    GameObject[] m_pingedEnemies;
    GameObject[] m_pingedMissiles;
    
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
    float m_lockOffTime = 0.0f;
    float m_lockOnTime = 0.0f;
    float m_deathTimer = 45.0f;
    float m_continueTimer = 10.0f;
    float m_attackWarningTimer = 50.0f;
    float m_outOfBoundsTimer = 10.0f;
    
    // Input/Controls
    float m_dpadScrollPause = 0;
    bool m_hasLockedTarget = false;
    
    // Drag & Drop stuff
    bool m_isRequestingItem = false;
    ItemScript m_currentDraggedItem = null;
    bool m_currentDraggedItemIsFromPlayerInv = true;
    int m_currentDraggedItemInventoryId = -1;
    Vector2 m_playerScrollPosition = Vector2.zero;
    Vector2 m_cShipScrollPosition = Vector2.zero;
    bool m_transferFailed = false;
    string m_transferFailedMessage = "Transfer failed - Item Requested Elsewhere";
    
    // Animatables
    int m_playerPanelXWidth = 408;
    int m_cShipPanelXPos = 796;
    
    // Rects
    Dictionary<Rect, ItemScript> m_drawnItems = new Dictionary<Rect, ItemScript>();
    Dictionary<Rect, ItemScript> m_drawnItemsSecondary = new Dictionary<Rect, ItemScript>();
    
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

    // Chat Vars
    List<string> m_chatMessages;

    /* Getters/Setters */
    public void SetCShip(GameObject cship)
    {
        Debug.Log("Attaching cship to GUI");
        m_cShipGameObject = cship;
        m_cShipHealth = cship.GetComponent<HealthScript>();
    }

    /* Unity Functions */
    void Start()
    {
        Time.timeScale = 1.0f;
        int control = PlayerPrefs.GetInt("UseControl");
        if (control == 1)
            m_useController = true;

        m_chatMessages = new List<string>();
        
#if UNITY_STANDALONE
        GetClipCursor(ref previousCursorClip);

        System.IntPtr window = FindWindow(null, "Galaxodus");
        Rect current = new Rect(0, 0, Screen.width, Screen.height);
        SetWindowPos(window, 0, 0, 0, current.width, current.height, 0);

        RECT cursorLimits;
        cursorLimits.Left = 0;
        cursorLimits.Top = 0;
        cursorLimits.Right = Screen.width - 1;
        cursorLimits.Bottom = Screen.height - 1;
        ClipCursor(ref cursorLimits);
#endif

        m_newResolution = Screen.currentResolution;

        m_blobSize = Screen.height * 0.015f;

        m_IPField = PlayerPrefs.GetString("LastIP");
        m_username = PlayerPrefs.GetString("LastUsername", "Name");
        
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
        m_gameTimer += Time.deltaTime;
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
                    shop.GetComponent<ShopScript>().RequestNewInventory(m_gameTimer);
                }
            }
        }
        
        if (m_currentGameState == GameState.InGame)
        {
            GameStateController controller = m_gameStateController.GetComponent<GameStateController>();
            if (m_playerShips == null || m_playerShips.Length == 0 || m_playerShips.Length < controller.m_connectedPlayers.Count - controller.m_deadPlayers.Count)
            {
                Debug.Log("Resetting player array. Length vs Count == " + m_playerShips.Length + " vs " + (controller.m_connectedPlayers.Count - controller.m_deadPlayers.Count));
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
            m_lockonTime += Time.deltaTime;
            //TODO: Parametise this later
            if (m_lockonTime > 0.7f)
            {
                if (m_thisPlayerHP)
                {
                    m_thisPlayerHP.GetComponent<PlayerControlScript>().SetNewTargetLock(m_lastLockonTarget);
                    m_hasLockedTarget = true;
                }
                
                else
                {
                    m_lockonTime = 0f;
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
                m_lockonTime = 0.0f;
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
            case GameState.ClientConnectingMenu:
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
                DrawMapMenu();
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
    void DrawLostConnection()
    {
        GUI.DrawTexture(new Rect(600, 350, 400, 200), m_menuBackground);
        GUI.Label(new Rect(650, 400, 300, 50), "The host has disconnected.", m_nonBoxStyle);
        if (GUI.Button(new Rect(750, 500, 100, 50), "Return to menu", m_sharedGUIStyle))
        {
            Application.LoadLevel(0);
        }
    }
    
    void DrawFailedConnectByName()
    {
        GUI.DrawTexture(new Rect(222, 130, 290, 620), m_menuBackground);
        
        GUI.Label(new Rect(222, 331, 290, 50), "Nickname mismatch!", m_nonBoxBigStyle);
        
        if (GUI.Button(new Rect(225, 698, 285, 50), "BACK", m_sharedGUIStyle))
        {
            m_gameStateController.GetComponent<GameStateController>().BackToMenu();
        }
    }
    
    void DrawConnectingScreen()
    {
        GUI.DrawTexture(new Rect(222, 130, 290, 620), m_menuBackground);
        
        GUI.Label(new Rect(222, 331, 290, 50), "Connecting...", m_nonBoxBigStyle);
        
        if (GUI.Button(new Rect(225, 698, 285, 50), "CANCEL", m_sharedGUIStyle))
        {
            m_gameStateController.GetComponent<GameStateController>().PlayerCancelsConnect();
        }
        if (m_selectedButton == 1)
        {
            GUI.DrawTexture(new Rect(225, 698, 285, 50), m_menuButtonHighlight);
        }
    }
    
    void DrawOptionMenu()
    {
        GUI.DrawTexture(new Rect(512, 130, 290, 620), m_menuBackground);
        
        GUI.Label(new Rect(515, 131, 288, 50), "OPTIONS", m_nonBoxBigStyle);
        /* Graphics options */
        
        //Reso + FS options
        GUI.Label(new Rect(515, 220, 288, 50), "RESOLUTION", m_nonBoxBigStyle);
        if (GUI.Button(new Rect(513, 270, 288, 50), m_newResolution.width + "x" + m_newResolution.height, m_sharedGUIStyle))
        {
            m_resoDropdown = !m_resoDropdown;
        }
        if (m_selectedButton == 1)
        {
            GUI.DrawTexture(new Rect(515, 220, 288, 50), m_menuButtonHighlight);
        }
        
        if (m_resoDropdown)
        {
            Resolution[] possibleResos = Screen.resolutions;
            //GUI.Box(new Rect(300, 100, 200, possibleResos.Length * 50), "");
            
            //Draw each reso as a button, on click, set it and unset resoDropdown
            for (int i = 0; i < possibleResos.Length; i++)
            {
                if (GUI.Button(new Rect(815, 270 + (i * 50), 288, 50), possibleResos[i].width + "x" + possibleResos[i].height, m_sharedGUIStyle))
                {
                    m_newResolution = possibleResos[i];
                    m_resoDropdown = false;
                }
            }
        }
        
        if (m_shouldFullscreen)
        {
            if (GUI.Button(new Rect(540, 330, 238, 50), "Fullscreen On", m_sharedHighlightedGUIStyle))
            {
                m_shouldFullscreen = !m_shouldFullscreen;
            }
        }
        else
        {
            if (GUI.Button(new Rect(540, 330, 238, 50), "Fullscreen Off", m_sharedGUIStyle))
            {
                m_shouldFullscreen = !m_shouldFullscreen;
            }
        }
        if (m_selectedButton == 2)
        {
            GUI.DrawTexture(new Rect(540, 330, 238, 50), m_menuButtonHighlight);
        }
        
        if (GUI.Button(new Rect(540, 390, 238, 50), "Apply Resolution Changes", m_sharedGUIStyle))
        {
            Screen.SetResolution(m_newResolution.width, m_newResolution.height, m_shouldFullscreen);
        }
        if (m_selectedButton == 3)
        {
            GUI.DrawTexture(new Rect(540, 390, 238, 50), m_menuButtonHighlight);
        }
        
        if (m_useController)
        {
            if (GUI.Button(new Rect(540, 450, 238, 50), "Use Controller", m_sharedHighlightedGUIStyle))
            {
                m_useController = false;
                PlayerPrefs.SetInt("UseControl", 0);
            }
        }
        else
        {
            if (GUI.Button(new Rect(540, 450, 238, 50), "Use Controller", m_sharedGUIStyle))
            {
                if (Input.GetJoystickNames().Length > 0)
                {
                    m_useController = true;
                    PlayerPrefs.SetInt("UseControl", 1);
                }
            }
        }
        if (m_selectedButton == 4)
        {
            GUI.DrawTexture(new Rect(540, 450, 238, 50), m_menuButtonHighlight);
        }
        
        //Quality settings
        
        /* Audio options */
        GUI.Label(new Rect(515, 540, 288, 50), "SOUND", m_nonBoxBigStyle);
        
        GUI.Label(new Rect(515, 590, 288, 50), "Music", m_nonBoxStyle);
        PlayerPrefs.SetFloat("MusicVolume", GUI.HorizontalSlider(new Rect(520, 640, 278, 20), PlayerPrefs.GetFloat("MusicVolume"), 0.0f, 1.0f));
        if (m_selectedButton == 5)
        {
            GUI.DrawTexture(new Rect(515, 590, 288, 50), m_menuButtonHighlight);
        }
        
        GUI.Label(new Rect(515, 660, 288, 50), "Effects", m_nonBoxStyle);
        PlayerPrefs.SetFloat("EffectVolume", GUI.HorizontalSlider(new Rect(520, 710, 278, 20), PlayerPrefs.GetFloat("EffectVolume"), 0.0f, 1.0f));
        if (m_selectedButton == 6)
        {
            GUI.DrawTexture(new Rect(515, 660, 288, 50), m_menuButtonHighlight);
        }
        
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
        GUI.DrawTexture(new Rect(222, 130, 290, 620), m_menuBackground);
        GUI.Label(new Rect(225, 131, 285, 100), "Please enter a name and select an option:", m_sharedGUIStyle);
        
        m_username = GUI.TextField(new Rect(225, 228, 285, 50), m_username, 19, m_sharedGUIStyle);
        m_username = Regex.Replace(m_username, @"[^a-zA-Z0-9 ]", "");
        
        
        
        if (GUI.Button(new Rect(225, 400, 285, 50), "HOST", m_sharedGUIStyle))
        {
            HostButtonActivate();
        }
        if (m_selectedButton == 1)
        {
            GUI.DrawTexture(new Rect(225, 400, 285, 50), m_menuButtonHighlight);
        }
        
        if (GUI.Button(new Rect(225, 450, 285, 50), "JOIN", m_sharedGUIStyle))
        {
            JoinButtonActivate();
        }
        if (m_selectedButton == 2)
        {
            GUI.DrawTexture(new Rect(225, 450, 285, 50), m_menuButtonHighlight);
        }
        
        if (GUI.Button(new Rect(225, 500, 285, 50), "OPTIONS", m_sharedGUIStyle))
        {
            m_gameStateController.GetComponent<GameStateController>().OpenOptionMenu();
        }
        if (m_selectedButton == 3)
        {
            GUI.DrawTexture(new Rect(225, 500, 285, 50), m_menuButtonHighlight);
        }
        
        if (GUI.Button(new Rect(225, 698, 285, 50), "QUIT", m_sharedGUIStyle))
        {
            Application.Quit();
        }
        if (m_selectedButton == 4)
        {
            GUI.DrawTexture(new Rect(225, 698, 285, 50), m_menuButtonHighlight);
        }
    }
    
    void DrawHostMenu()
    {
        GUI.DrawTexture(new Rect(222, 130, 290, 620), m_menuBackground);
        
        GUI.Label(new Rect(225, 131, 285, 50), "Connected Players:", m_nonBoxBigStyle);
        GameStateController gsc = m_gameStateController.GetComponent<GameStateController>();
        for (int i = 0; i < gsc.m_connectedPlayers.Count; i++)
        {
            GUI.Label(new Rect(225, 188 + (i * 40), 285, 40), gsc.m_connectedPlayers[i].m_name, m_nonBoxStyle);
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
        GUI.DrawTexture(new Rect(222, 130, 290, 620), m_menuBackground);
        //GUI.Label (new Rect(200, 200, 300, 100), "Your name: ");
        //username = GUI.TextField(new Rect(200, 275, 300, 100), username);
        
        GUI.Label(new Rect(222, 331, 290, 50), "IP Address:", m_nonBoxBigStyle);
        m_IPField = GUI.TextField(new Rect(222, 380, 290, 50), m_IPField, m_sharedGUIStyle);
        if (m_selectedButton == 1)
        {
            GUI.DrawTexture(new Rect(222, 380, 290, 50), m_menuButtonHighlight);
        }
        
        //GUI.Label (new Rect(200, 400, 300, 50), "I.P. Address:");
        //IPField = GUI.TextField(new Rect(200, 475, 300, 50), IPField);
        
        if (GUI.Button(new Rect(222, 600, 290, 100), "CONNECT", m_sharedGUIStyle) || Event.current.keyCode == KeyCode.KeypadEnter || Event.current.keyCode == KeyCode.Return)
        {
            //if(username != "Name")
            ClientConnectJoinActivate();
        }
        if (m_selectedButton == 2)
        {
            GUI.DrawTexture(new Rect(222, 600, 290, 100), m_menuButtonHighlight);
        }
        
        if (GUI.Button(new Rect(222, 698, 290, 50), "BACK", m_sharedGUIStyle))
        {
            ClientConnectBackActivate();
        }
        if (m_selectedButton == 3)
        {
            GUI.DrawTexture(new Rect(222, 698, 290, 50), m_menuButtonHighlight);
        }
    }
    
    void DrawClientMenu()
    {
        GUI.DrawTexture(new Rect(222, 130, 290, 620), m_menuBackground);
        
        GUI.Label(new Rect(225, 131, 285, 50), "Connected Players:", m_nonBoxBigStyle);
        GameStateController gsc = m_gameStateController.GetComponent<GameStateController>();
        for (int i = 0; i < gsc.m_connectedPlayers.Count; i++)
        {
            GUI.Label(new Rect(225, 188 + (i * 40), 285, 40), gsc.m_connectedPlayers[i].m_name, m_nonBoxStyle);
        }
        
        if (GUI.Button(new Rect(225, 698, 285, 50), "BACK", m_sharedGUIStyle))
        {
            ClientConnectingBackActivate();
        }
    }
    
    void DrawInGameSpec()
    {
        //Timer: show timer in a nice box :)
        GUI.DrawTexture(new Rect(740, 5, 10, 50), m_barEnd);
        GUI.DrawTexture(new Rect(750, 5, 100, 50), m_barMid);
        GUI.DrawTexture(new Rect(860, 5, -10, 50), m_barEnd);
        int seconds2 = (int)m_gameTimer;
        string displayedTime = string.Format("{0:00}:{1:00}", (seconds2 / 60) % 60, seconds2 % 60);
        GUI.Label(new Rect(760, 10, 80, 40), displayedTime, m_nonBoxStyle);
        
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
        List<DeadPlayer> deadPlayers = m_gameStateController.GetComponent<GameStateController>().m_deadPlayers;
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
                    Texture weapTex = m_players[i].GetComponent<PlayerControlScript>().m_equippedWeaponItem.GetComponent<ItemScript>().GetIcon();
                    Texture shieldTex = m_players[i].GetComponent<PlayerControlScript>().m_equippedShieldItem.GetComponent<ItemScript>().GetIcon();
                    Texture armourTex = m_players[i].GetComponent<PlayerControlScript>().m_equippedPlatingItem.GetComponent<ItemScript>().GetIcon();
                    Texture engineTex = m_players[i].GetComponent<PlayerControlScript>().m_equippedEngineItem.GetComponent<ItemScript>().GetIcon();
                    
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
                    Texture weapTex = m_players[i].GetComponent<PlayerControlScript>().m_equippedWeaponItem.GetComponent<ItemScript>().GetIcon();
                    Texture shieldTex = m_players[i].GetComponent<PlayerControlScript>().m_equippedShieldItem.GetComponent<ItemScript>().GetIcon();
                    Texture armourTex = m_players[i].GetComponent<PlayerControlScript>().m_equippedPlatingItem.GetComponent<ItemScript>().GetIcon();
                    Texture engineTex = m_players[i].GetComponent<PlayerControlScript>().m_equippedEngineItem.GetComponent<ItemScript>().GetIcon();
                    
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
        
        if (m_isOnMap)
            DrawMap();
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
                        m_thisPlayerHP.gameObject.GetComponent<PlayerControlScript>().m_isInRangeOfCapitalDock = true;
                    }
                    else
                    {
                        m_thisPlayerHP.gameObject.GetComponent<PlayerControlScript>().m_isInRangeOfCapitalDock = false;
                    }
                }
            }
            
            if (m_PlayerHasDockedAtShop && !m_PlayerHasDied)
            {
                if (m_shopDockedAt != null)
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
                        GUI.Label(new Rect(600, 180, 400, 50), m_transferFailedMessage, m_nonBoxStyle);
                    }

                    //Player - left
                    GUI.Label(new Rect(816, 270, 164, 40), "Player:", m_nonBoxStyle);
                    List<GameObject> playerInv = m_thisPlayerHP.GetComponent<PlayerControlScript>().m_playerInventory;
                    Rect scrollAreaRectPl = new Rect(816, 330, 180, 320);
                    m_playerScrollPosition = GUI.BeginScrollView(new Rect(816, 330, 180, 320), m_playerScrollPosition, new Rect(0, 0, 150, 52 * playerInv.Count));
                    for (int i = 0; i < playerInv.Count; i++)
                    {
                        GUI.Label(new Rect(0, 5 + (i * 50), 50, 50), playerInv[i].GetComponent<ItemScript>().GetIcon());
                        Rect lastR = new Rect(60, 10 + (i * 50), 114, 40);
                        GUI.Label(lastR, playerInv[i].GetComponent<ItemScript>().GetItemName(), m_nonBoxSmallStyle);
                        Rect modR = new Rect(lastR.x + scrollAreaRectPl.x, lastR.y + scrollAreaRectPl.y - m_playerScrollPosition.y, lastR.width, lastR.height);
                        Rect finalRect = new Rect(modR.x - 50, modR.y, modR.width + 50, modR.height);
                        
                        if (scrollAreaRectPl.Contains(new Vector2(modR.x, modR.y)) && scrollAreaRectPl.Contains(new Vector2(modR.x + modR.width, modR.y + modR.height)))
                            m_drawnItemsSecondary.Add(finalRect, playerInv[i].GetComponent<ItemScript>());
                        
                        if (!m_inGameMenuIsOpen && currentEvent.type == EventType.MouseDown && m_shopDockedAt.GetComponent<ShopScript>().GetShopType() == ShopScript.ShopType.Shipyard)
                        {
                            bool insideFinalRect = finalRect.Contains(mousePos);
                            if (!m_shopConfirmBuy && insideFinalRect && !m_isRequestingItem)
                            {
                                //Begin drag & drop
                                m_currentDraggedItem = playerInv[i].GetComponent<ItemScript>();
                                m_currentDraggedItemInventoryId = i;
                                m_currentDraggedItemIsFromPlayerInv = true;
                            }
                        }
                    }
                    GUI.EndScrollView();
                    
                    //Shop - right
                    GUI.Label(new Rect(1020, 270, 164, 40), "Shop:", m_nonBoxStyle);
                    NetworkInventory shopInv = m_shopDockedAt.GetComponent<NetworkInventory>();
                    Rect scrollAreaRect = new Rect(1020, 330, 180, 320);
                    m_cShipScrollPosition = GUI.BeginScrollView(scrollAreaRect, m_cShipScrollPosition, new Rect(0, 0, 150, 52 * shopInv.GetCount()));
                    for (int i = 0; i < shopInv.GetCount(); i++)
                    {
                        if(shopInv[i] != null)
                        {
                            GUI.Label(new Rect(0, 5 + (i * 50), 50, 50), shopInv[i].GetIcon());
                            Rect lastR = new Rect(60, 10 + (i * 50), 114, 40);
                            GUI.Label(lastR, shopInv[i].GetComponent<ItemScript>().GetItemName(), m_nonBoxSmallStyle);
                            Rect modR = new Rect(lastR.x + scrollAreaRect.x, lastR.y + scrollAreaRect.y - m_cShipScrollPosition.y, lastR.width, lastR.height);
                            Rect finalRect = new Rect(modR.x - 50, modR.y, modR.width + 50, modR.height);
                            
                            if (scrollAreaRect.Contains(new Vector2(modR.x, modR.y)) && scrollAreaRect.Contains(new Vector2(modR.x + modR.width, modR.y + modR.height)))
                                m_drawnItems.Add(finalRect, shopInv[i]);
                            
                            if (!m_inGameMenuIsOpen && currentEvent.type == EventType.MouseDown)
                            {
                                bool insideFinalRect = finalRect.Contains(mousePos);
                                if (!m_shopConfirmBuy && insideFinalRect && !m_isRequestingItem)
                                {
                                    if(m_thisPlayerHP.GetComponent<PlayerControlScript>().CheckCanAffordAmount(m_shopDockedAt.GetComponent<ShopScript>().GetItemCost(i)))
                                    {
                                        //Since we're a shop, on mouseDown, open the item confirmation box
                                        shopInv.RequestServerCancel(m_currentTicket);
                                        shopInv.RequestServerItem(shopInv[i].m_equipmentID, i);
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
                    if(m_shopDockedAt.GetComponent<ShopScript>().GetShopType() == ShopScript.ShopType.Basic)
                    {
                        GUI.DrawTexture(new Rect(396, 221, 403, 460), m_smallShopTexture);
                        int hpPercent = (int)(m_thisPlayerHP.GetHPPercentage() * 100.0f);
                        GUI.Label (new Rect(695, 440, 90, 40), hpPercent.ToString() + "%", m_nonBoxStyle);
                    }
                    else
                    {
                        float playerSourceWidth = m_playerPanelXWidth / 408.0f;
                        GUI.DrawTexture(new Rect(396, 221, 403, 460), m_DockInventoryBorder);
                        GUI.DrawTexture(new Rect(396, 221, 403, 460), m_DockPlayerImage);
                        
                        //Equipped icons:
                        float iconLeftX = 602.0f - (408.0f - m_playerPanelXWidth);
                        if (iconLeftX >= 324.0f)
                        {
                            
                            m_drawnItemsSecondary.Add(weaponTemp, m_thisPlayerHP.GetComponent<PlayerControlScript>().m_equippedWeaponItem.GetComponent<ItemScript>());
                            m_drawnItemsSecondary.Add(shieldTemp, m_thisPlayerHP.GetComponent<PlayerControlScript>().m_equippedShieldItem.GetComponent<ItemScript>());
                            m_drawnItemsSecondary.Add(platingTemp, m_thisPlayerHP.GetComponent<PlayerControlScript>().m_equippedPlatingItem.GetComponent<ItemScript>());
                            m_drawnItemsSecondary.Add(engineTemp, m_thisPlayerHP.GetComponent<PlayerControlScript>().m_equippedEngineItem.GetComponent<ItemScript>());
                            
                            GUI.DrawTexture(weaponTemp, m_thisPlayerHP.GetComponent<PlayerControlScript>().m_equippedWeaponItem.GetComponent<ItemScript>().GetIcon());
                            GUI.DrawTexture(shieldTemp, m_thisPlayerHP.GetComponent<PlayerControlScript>().m_equippedShieldItem.GetComponent<ItemScript>().GetIcon());
                            GUI.DrawTexture(platingTemp, m_thisPlayerHP.GetComponent<PlayerControlScript>().m_equippedPlatingItem.GetComponent<ItemScript>().GetIcon());
                            GUI.DrawTexture(engineTemp, m_thisPlayerHP.GetComponent<PlayerControlScript>().m_equippedEngineItem.GetComponent<ItemScript>().GetIcon());
                        }
                    }
                    
                    //Hover text
                    if (!m_inGameMenuIsOpen && m_currentDraggedItem == null)
                    {
                        foreach (Rect key in m_drawnItems.Keys)
                        {
                            if (key.Contains(mousePos))
                            {
                                int id = m_shopDockedAt.GetComponent<ShopScript>().GetIDIfItemPresent(m_drawnItems[key]);
                                string text = m_drawnItems[key].GetShopText(m_shopDockedAt.GetComponent<ShopScript>().GetItemCost(id));
                                DrawHoverText(text, mousePos);
                            }
                        }
                        
                        foreach (Rect key in m_drawnItemsSecondary.Keys)
                        {
                            if (key.Contains(mousePos))
                            {
                                string text = m_drawnItemsSecondary[key].GetShopText();
                                DrawHoverText(text, mousePos);  
                            }
                        }
                    }
                    else if(!m_inGameMenuIsOpen)
                    {
                        GUI.Label(new Rect(mousePos.x - 20, mousePos.y - 20, 40, 40), m_currentDraggedItem.GetComponent<ItemScript>().GetIcon());
                        
                        if(!Input.GetMouseButton(0))
                        {
                            //Drop item whereever you are
                            if(weaponTemp.Contains(mousePos))
                            {
                                if(m_currentDraggedItem.m_typeOfItem == ItemType.Weapon)
                                {
                                    m_thisPlayerHP.GetComponent<PlayerControlScript>().EquipItemInSlot(m_currentDraggedItemInventoryId);
                                    m_currentDraggedItem = null;
                                    m_currentDraggedItemInventoryId = -1;
                                    m_currentDraggedItemIsFromPlayerInv = false;
                                }
                                else
                                {
                                    m_currentDraggedItem = null;
                                    m_currentDraggedItemInventoryId = -1;
                                    m_currentDraggedItemIsFromPlayerInv = false;  
                                }
                            }
                            else if(shieldTemp.Contains(mousePos))
                            {
                                if(m_currentDraggedItem.m_typeOfItem == ItemType.Shield)
                                {
                                    m_thisPlayerHP.GetComponent<PlayerControlScript>().EquipItemInSlot(m_currentDraggedItemInventoryId);
                                    m_currentDraggedItem = null;
                                    m_currentDraggedItemInventoryId = -1;
                                    m_currentDraggedItemIsFromPlayerInv = false;
                                }
                                else
                                {
                                    m_currentDraggedItem = null;
                                    m_currentDraggedItemInventoryId = -1;
                                    m_currentDraggedItemIsFromPlayerInv = false;  
                                }
                            }
                            else if(platingTemp.Contains(mousePos))
                            {
                                if(m_currentDraggedItem.m_typeOfItem == ItemType.Plating)
                                {
                                    m_thisPlayerHP.GetComponent<PlayerControlScript>().EquipItemInSlot(m_currentDraggedItemInventoryId);
                                    m_currentDraggedItem = null;
                                    m_currentDraggedItemInventoryId = -1;
                                    m_currentDraggedItemIsFromPlayerInv = false;
                                }
                                else
                                {
                                    m_currentDraggedItem = null;
                                    m_currentDraggedItemInventoryId = -1;
                                    m_currentDraggedItemIsFromPlayerInv = false;  
                                }
                            }
                            else if(engineTemp.Contains(mousePos))
                            {
                                if(m_currentDraggedItem.m_typeOfItem == ItemType.Engine)
                                {
                                    m_thisPlayerHP.GetComponent<PlayerControlScript>().EquipItemInSlot(m_currentDraggedItemInventoryId);
                                    m_currentDraggedItem = null;
                                    m_currentDraggedItemInventoryId = -1;
                                    m_currentDraggedItemIsFromPlayerInv = false;
                                }
                                else
                                {
                                    m_currentDraggedItem = null;
                                    m_currentDraggedItemInventoryId = -1;
                                    m_currentDraggedItemIsFromPlayerInv = false;  
                                }
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
                        GUI.Label(new Rect(662, 350, 277, 50), "Buy '" + m_confirmBuyItem.GetItemName() + "' for $" + m_confirmBuyItem.m_cost + "?", m_nonBoxStyle);
                        
                        if(GUI.Button (new Rect(700, 440, 70, 40), "Confirm"))
                        {
                            //int cost = m_shopDockedAt.GetComponent<ShopScript>().GetItemCost(m_currentTicket.itemIndex);
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
                    if (!m_inGameMenuIsOpen && !m_shopConfirmBuy && GUI.Button(new Rect(512, 687, 176, 110), "", "label"))
                    {
                        m_PlayerHasDockedAtShop = false;
                        m_shopDockedAt = null;
                        m_shipyardScreen = true;
                        m_thisPlayerHP.gameObject.GetComponent<PlayerControlScript>().nearbyShop = null;
                        m_thisPlayerHP.transform.parent = null;
                        m_thisPlayerHP.gameObject.GetComponent<PlayerControlScript>().TellShipStartRecievingInput();
                        m_thisPlayerHP.rigidbody.isKinematic = false;
                        Screen.showCursor = false;
                    }
                }
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
                        m_thisPlayerHP.gameObject.GetComponent<PlayerControlScript>().m_isInRangeOfTradingDock = true;
                        m_thisPlayerHP.gameObject.GetComponent<PlayerControlScript>().nearbyShop = shop;
                    }
                    else
                    {
                        m_thisPlayerHP.gameObject.GetComponent<PlayerControlScript>().m_isInRangeOfTradingDock = false;
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
                int seconds = (int)m_gameTimer;
                string displayedTime2 = string.Format("{0:00}:{1:00}", (seconds / 60) % 60, seconds % 60);
                GUI.Label(new Rect(700, 300, 200, 80), "Final time: " + displayedTime2, m_nonBoxStyle);
                
                if (GUI.Button(new Rect(750, 400, 100, 80), "Restart", m_sharedGUIStyle))
                {
                    Time.timeScale = 1.0f;
                    Network.Disconnect();
                    Application.LoadLevel(0);
                }
            }
            
            //Map screen
            if (m_isOnMap)
                DrawMap();
            else
            {
                if (m_isOnFollowMap)
                    DrawSmallFollowMap();
                else
                {
                    DrawSmallMap();
                }
            }
            
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
                        m_lockonTime = 0.0f;
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
                            m_lockonTime = 0.0f;
                            m_lockOffTime = 0.0f;
                        }
                    }
                    else
                    {
                        m_isLockingOn = false;
                        m_lastLockonTarget = null;
                        m_lockonTime = 0.0f;
                        m_lockOffTime = 0.0f;
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
                                m_lockOffTime = 0.0f;
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
        
        //Show gametime
        GUI.DrawTexture(new Rect(690, 5, 10, 50), m_barEnd);
        GUI.DrawTexture(new Rect(700, 5, 200, 50), m_barMid);
        GUI.DrawTexture(new Rect(910, 5, -10, 50), m_barEnd);
        int seconds2 = (int)m_gameTimer;
        string displayedTime = string.Format("{0:00}:{1:00}", (seconds2 / 60) % 60, seconds2 % 60);
        GUI.Label(new Rect(710, 10, 180, 40), displayedTime, m_nonBoxStyle);
        
        //Now, finally, draw the hp/shield areas
        if (!m_PlayerHasDied && m_thisPlayerHP != null)
        {
            //New New HP Bar
            float healthPercent = m_thisPlayerHP.GetHPPercentage();
            healthPercent = Mathf.Max(0, healthPercent);
            float shieldPercent = m_thisPlayerHP.GetShieldPercentage();
            shieldPercent = Mathf.Max(0, shieldPercent);
            
            GUI.DrawTexture(new Rect(0, 0, 150, 150), m_iconBorder);
            GUI.DrawTexture(new Rect(0, 0, 150, 150), m_playerIcon);
            
            GUI.DrawTexture(new Rect(150, 0, 350, 50), m_healthBackground);
            GUI.DrawTextureWithTexCoords(new Rect(150, 0, 350 * healthPercent, 50), m_healthBar, new Rect(0, 0, healthPercent, 1));
            GUI.DrawTextureWithTexCoords(new Rect(150, 0, 350 * shieldPercent, 50), m_shieldBar, new Rect(0, 0, shieldPercent, 1));
            
            //Show spacebux
            GUI.DrawTexture(new Rect(175, 80, 10, 50), m_barEnd);
            GUI.DrawTexture(new Rect(185, 80, 200, 50), m_barMid);
            GUI.DrawTexture(new Rect(395, 80, -10, 50), m_barEnd);
            GUI.Label(new Rect(195, 85, 180, 44), "$ " + m_thisPlayerHP.gameObject.GetComponent<PlayerControlScript>().GetCash(), m_nonBoxStyle);
        }
        else
        {
            GUI.DrawTexture(new Rect(0, 0, 150, 150), m_iconBorder);
            GUI.DrawTexture(new Rect(0, 0, 150, 150), m_playerIcon);
            
            GUI.DrawTexture(new Rect(150, 0, 350, 50), m_healthBackground);
            
            GUI.DrawTexture(new Rect(175, 80, 10, 50), m_barEnd);
            GUI.DrawTexture(new Rect(185, 80, 200, 50), m_barMid);
            GUI.DrawTexture(new Rect(395, 80, -10, 50), m_barEnd);
            GUI.Label(new Rect(195, 85, 180, 44), "--", m_nonBoxStyle);
        }
        
        //Now show CShip HP
        if (m_cShipGameObject == null)
        {
            GUI.DrawTexture(new Rect(1450, 0, 150, 150), m_iconBorder);
            GUI.DrawTexture(new Rect(1450, 0, 150, 150), m_CShipIcon);
            
            GUI.DrawTexture(new Rect(1100, 0, 350, 50), m_healthBackground);
            GUI.DrawTexture(new Rect(1205, 80, 10, 50), m_barEnd);
            GUI.DrawTexture(new Rect(1215, 80, 200, 50), m_barMid);
            GUI.DrawTexture(new Rect(1425, 80, -10, 50), m_barEnd);
        }
        else
        {
            //New New CShip Health
            float healthPercent = m_cShipHealth.GetHPPercentage();
            healthPercent = Mathf.Max(0, healthPercent);
            float shieldPercent = m_cShipHealth.GetShieldPercentage();
            shieldPercent = Mathf.Max(0, shieldPercent);
            
            GUI.DrawTexture(new Rect(1450, 0, 150, 150), m_iconBorder);
            GUI.DrawTexture(new Rect(1450, 0, 150, 150), m_CShipIcon);
            
            GUI.DrawTexture(new Rect(1100, 0, 350, 50), m_healthBackground);
            GUI.DrawTextureWithTexCoords(new Rect(1450, 0, -350 * healthPercent, 50), m_healthBar, new Rect(0, 0, healthPercent, 1));
            GUI.DrawTextureWithTexCoords(new Rect(1450, 0, -350 * shieldPercent, 50), m_shieldBar, new Rect(0, 0, shieldPercent, 1));
            
            //Show CShip moolah
            GUI.DrawTexture(new Rect(1205, 80, 10, 50), m_barEnd);
            GUI.DrawTexture(new Rect(1215, 80, 200, 50), m_barMid);
            GUI.DrawTexture(new Rect(1425, 80, -10, 50), m_barEnd);
            GUI.Label(new Rect(1225, 85, 180, 44), "$ " + m_cShipGameObject.GetComponent<CapitalShipScript>().GetBankedCash(), m_nonBoxStyle);
        }
    }
    
    void DrawCShipDockOverlay()
    {
        Event currentEvent = Event.current;
        Vector3 mousePos = currentEvent.mousePosition;
        
        GUI.DrawTexture(new Rect(396, 86, 807, 727), m_DockBackground);
        
        if(m_transferFailed)
        {
            GUI.Label(new Rect(600, 180, 400, 50), m_transferFailedMessage, m_nonBoxStyle);
        }
        
        //Show bank status
        GUI.Label(new Rect(1012, 140, 134, 40), "$" + m_cShipGameObject.GetComponent<CapitalShipScript>().GetBankedCash(), m_nonBoxStyle);
        
        //Desposit moneys
        if (GUI.Button(new Rect(1038, 180, 84, 33), "", "label"))
        {
            PlayerControlScript pCSc = m_thisPlayerHP.gameObject.GetComponent<PlayerControlScript>();
            int cashAmount = pCSc.GetSpaceBucks();
            pCSc.RemoveSpaceBucks(cashAmount);
            m_cShipGameObject.GetComponent<CapitalShipScript>().DepositCashToCShip(cashAmount);
        }
        
        m_drawnItems.Clear();
        //Do screen specific stuff here:
        switch (m_currentCShipPanel)
        {
            case CShipScreen.DualPanel:
            {
                if (!m_inGameMenuIsOpen && GUI.Button(new Rect(394, 250, m_playerPanelXWidth, 400), ""))
                {
                    //If player is selected, CShip should animate away
                    StartCoroutine(AnimateCShipPanel(1204));
                    m_currentCShipPanel = CShipScreen.PanelsAnimating;
                }
                
                if (!m_inGameMenuIsOpen && GUI.Button(new Rect(m_cShipPanelXPos, 250, (1204 - m_cShipPanelXPos), 400), ""))
                {
                    //If CShip is selected, player should animate away
                    StartCoroutine(AnimatePlayerPanel(0));
                    m_currentCShipPanel = CShipScreen.PanelsAnimating;
                }
                
                DrawLeftPanel();
                
                DrawRightPanel();
                
                break;
            }
            case CShipScreen.RightPanelActive:
            {
                DrawRightPanel();
                
                GUI.Label(new Rect(408, 270, 164, 40), "Player:", m_nonBoxStyle);
                List<GameObject> playerInv = m_thisPlayerHP.GetComponent<PlayerControlScript>().m_playerInventory;
                Rect scrollAreaRectPl = new Rect(408, 330, 180, 320);
                m_playerScrollPosition = GUI.BeginScrollView(scrollAreaRectPl, m_playerScrollPosition, new Rect(0, 0, 150, 52 * playerInv.Count));
                for (int i = 0; i < playerInv.Count; i++)
                {
                    GUI.Label(new Rect(0, 5 + (i * 50), 50, 50), playerInv[i].GetComponent<ItemScript>().GetIcon());
                    Rect lastR = new Rect(60, 10 + (i * 50), 114, 40);
                    GUI.Label(lastR, playerInv[i].GetComponent<ItemScript>().GetItemName(), m_nonBoxSmallStyle);
                    Rect modR = new Rect(lastR.x + scrollAreaRectPl.x, lastR.y + scrollAreaRectPl.y - m_playerScrollPosition.y, lastR.width, lastR.height);
                    Rect finalRect = new Rect(modR.x - 50, modR.y, modR.width + 50, modR.height);
                    
                    if (scrollAreaRectPl.Contains(new Vector2(modR.x, modR.y)) && scrollAreaRectPl.Contains(new Vector2(modR.x + modR.width, modR.y + modR.height)))
                        m_drawnItems.Add(finalRect, playerInv[i].GetComponent<ItemScript>());
                    
                    if (!m_inGameMenuIsOpen && currentEvent.type == EventType.MouseDown)
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
                
                GUI.Label(new Rect(612, 270, 164, 40), "Capital:", m_nonBoxStyle);
                NetworkInventory cshipInv = m_cShipGameObject.GetComponent<NetworkInventory>();
                Rect scrollAreaRect = new Rect(612, 330, 180, 320);
                m_cShipScrollPosition = GUI.BeginScrollView(scrollAreaRect, m_cShipScrollPosition, new Rect(0, 0, 150, 52 * cshipInv.GetCount()));
                for (int i = 0; i < cshipInv.GetCount(); i++)
                {
                    GUI.Label(new Rect(0, 5 + (i * 50), 50, 50), cshipInv[i].GetIcon());
                    Rect lastR = new Rect(60, 10 + (i * 50), 114, 40);
                    GUI.Label(lastR, cshipInv[i].GetItemName(), m_nonBoxSmallStyle);
                    Rect modR = new Rect(lastR.x + scrollAreaRect.x, lastR.y + scrollAreaRect.y - m_cShipScrollPosition.y, lastR.width, lastR.height);
                    Rect finalRect = new Rect(modR.x - 50, modR.y, modR.width + 50, modR.height);
                    
                    if (scrollAreaRect.Contains(new Vector2(modR.x, modR.y)) && scrollAreaRect.Contains(new Vector2(modR.x + modR.width, modR.y + modR.height)))
                        m_drawnItems.Add(finalRect, cshipInv[i].GetComponent<ItemScript>());
                    
                    if (!m_inGameMenuIsOpen && currentEvent.type == EventType.MouseDown)
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
                if (!m_inGameMenuIsOpen && m_currentDraggedItem != null)
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
                else if(!m_inGameMenuIsOpen)
                {
                    GameObject[] turrets = m_cShipGameObject.GetComponent<CapitalShipScript>().GetAttachedTurrets();
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
                
                if (GUI.Button(new Rect(796, 250, 408, 400), "", "label"))
                {
                    StartCoroutine(AnimatePlayerPanel(408));
                    m_currentCShipPanel = CShipScreen.PanelsAnimating;
                }
                break;
            }
            case CShipScreen.LeftPanelActive:
            {
                DrawLeftPanel();
                
                GUI.Label(new Rect(816, 270, 164, 40), "Player:", m_nonBoxStyle);
                List<GameObject> playerInv = m_thisPlayerHP.GetComponent<PlayerControlScript>().m_playerInventory;
                Rect scrollAreaRectPl = new Rect(816, 330, 180, 320);
                m_playerScrollPosition = GUI.BeginScrollView(new Rect(816, 330, 180, 320), m_playerScrollPosition, new Rect(0, 0, 150, 52 * playerInv.Count));
                for (int i = 0; i < playerInv.Count; i++)
                {
                    GUI.Label(new Rect(0, 5 + (i * 50), 50, 50), playerInv[i].GetComponent<ItemScript>().GetIcon());
                    Rect lastR = new Rect(60, 10 + (i * 50), 114, 40);
                    GUI.Label(lastR, playerInv[i].GetComponent<ItemScript>().GetItemName(), m_nonBoxSmallStyle);
                    Rect modR = new Rect(lastR.x + scrollAreaRectPl.x, lastR.y + scrollAreaRectPl.y - m_playerScrollPosition.y, lastR.width, lastR.height);
                    Rect finalRect = new Rect(modR.x - 50, modR.y, modR.width + 50, modR.height);
                    
                    if (scrollAreaRectPl.Contains(new Vector2(modR.x, modR.y)) && scrollAreaRectPl.Contains(new Vector2(modR.x + modR.width, modR.y + modR.height)))
                        m_drawnItems.Add(finalRect, playerInv[i].GetComponent<ItemScript>());
                    
                    if (!m_inGameMenuIsOpen && currentEvent.type == EventType.MouseDown)
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
                
                GUI.Label(new Rect(1020, 270, 164, 40), "Capital:", m_nonBoxStyle);
                NetworkInventory cshipInv = m_cShipGameObject.GetComponent<NetworkInventory>();
                Rect scrollAreaRect = new Rect(1020, 330, 180, 320);
                m_cShipScrollPosition = GUI.BeginScrollView(scrollAreaRect, m_cShipScrollPosition, new Rect(0, 0, 150, 52 * cshipInv.GetCount()));
                for (int i = 0; i < cshipInv.GetCount(); i++)
                {
                    GUI.Label(new Rect(0, 5 + (i * 50), 50, 50), cshipInv[i].GetComponent<ItemScript>().GetIcon());
                    Rect lastR = new Rect(60, 10 + (i * 50), 114, 40);
                    GUI.Label(lastR, cshipInv[i].GetComponent<ItemScript>().GetItemName(), m_nonBoxSmallStyle);
                    Rect modR = new Rect(lastR.x + scrollAreaRect.x, lastR.y + scrollAreaRect.y - m_cShipScrollPosition.y, lastR.width, lastR.height);
                    Rect finalRect = new Rect(modR.x - 50, modR.y, modR.width + 50, modR.height);
                    
                    if (scrollAreaRect.Contains(new Vector2(modR.x, modR.y)) && scrollAreaRect.Contains(new Vector2(modR.x + modR.width, modR.y + modR.height)))
                        m_drawnItems.Add(finalRect, cshipInv[i].GetComponent<ItemScript>());
                    
                    if (!m_inGameMenuIsOpen && currentEvent.type == EventType.MouseDown)
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
                if (!m_inGameMenuIsOpen && m_currentDraggedItem != null)
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
                else if(!m_inGameMenuIsOpen)
                {
                    //Hovers
                    if (m_LeftPanelWeaponRect.Contains(mousePos))
                    {
                        string text = m_thisPlayerHP.GetComponent<PlayerControlScript>().m_equippedWeaponItem.GetComponent<ItemScript>().GetShopText();
                        DrawHoverText(text, mousePos);
                    }
                    
                    if (m_LeftPanelShieldRect.Contains(mousePos))
                    {
                        string text = m_thisPlayerHP.GetComponent<PlayerControlScript>().m_equippedShieldItem.GetComponent<ItemScript>().GetShopText();
                        DrawHoverText(text, mousePos);
                    }
                    
                    if (m_LeftPanelPlatingRect.Contains(mousePos))
                    {
                        string text = m_thisPlayerHP.GetComponent<PlayerControlScript>().m_equippedPlatingItem.GetComponent<ItemScript>().GetShopText();
                        DrawHoverText(text, mousePos);
                    }
                    
                    if (m_LeftPanelEngineRect.Contains(mousePos))
                    {
                        string text = m_thisPlayerHP.GetComponent<PlayerControlScript>().m_equippedEngineItem.GetComponent<ItemScript>().GetShopText();
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
        if (!m_inGameMenuIsOpen && m_currentDraggedItem == null)
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
        List<DeadPlayer> deadPlayers = m_gameStateController.GetComponent<GameStateController>().m_deadPlayers;
        for (int i = 0; i < deadPlayers.Count; i++)
        {
            int fastSpawnCost = 500 + (int)(deadPlayers[i].m_deadTimer * 10);
            float buttonX = 811 + (i * 96);
            GUI.Label(new Rect(buttonX - 20, 690, 124, 33), deadPlayers[i].m_playerObject.m_name, m_nonBoxStyle);
            GUI.Label(new Rect(buttonX - 20, 722, 124, 33), "$" + fastSpawnCost, m_nonBoxStyle);
            
            if (GUI.Button(new Rect(buttonX, 765, 84, 33), ""))
            {
                //Check if amount is available, then respawn player as usual
                if (m_cShipGameObject.GetComponent<CapitalShipScript>().CShipCanAfford(fastSpawnCost))
                {
                    m_cShipGameObject.GetComponent<CapitalShipScript>().SpendBankedCash(fastSpawnCost);
                    RequestServerRespawnPlayer(deadPlayers[i].m_playerObject.m_netPlayer);
                }
                else
                    StartCoroutine(CountdownTransferFailedPopup(false));
            }
        }
        
        //Repair
        float damagePercent = 1.0f - m_thisPlayerHP.GetHPPercentage();
        int damage = m_thisPlayerHP.GetMaxHP() - m_thisPlayerHP.GetCurrHP();
        
        if(damagePercent > 0.0f)
        {
            int cost = Mathf.RoundToInt(damagePercent * 500.0f);
            int cash = m_thisPlayerHP.GetComponent<PlayerControlScript>().GetSpaceBucks();
            if(m_thisPlayerHP.GetComponent<PlayerControlScript>().CheckCanAffordAmount(cost))
            {
                if(GUI.Button(new Rect(430, 130, 120, 83), "Fully repair ship for $" + cost, m_sharedGUIStyle))
                {
                    m_thisPlayerHP.GetComponent<PlayerControlScript>().RemoveSpaceBucks(cost);
                    m_thisPlayerHP.RepairHP(damage);
                }
            }
            else if(cash != 0)
            {
                //Work out how much the player can afford
                float percentageAfford = cash / 500.0f;
                int percent = Mathf.RoundToInt(percentageAfford * 100.0f);
                float hpPerPercent = m_thisPlayerHP.GetMaxHP() / 100.0f;
                int cashToSpend = percent * 5;
                
                if(GUI.Button(new Rect(430, 130, 120, 83), "Repair " + percent + "% for $" + cashToSpend, m_sharedGUIStyle))
                {
                    int damageHealed = (int)(percent * hpPerPercent);
                    m_thisPlayerHP.RepairHP(damageHealed);
                    m_thisPlayerHP.GetComponent<PlayerControlScript>().RemoveSpaceBucks(cashToSpend);
                }
            }
            else
            {
                GUI.Label (new Rect(430, 130, 120, 83), "Repair -- No cash!", m_sharedGUIStyle);
            }
        }
        else
        {
            GUI.Label (new Rect(430, 130, 120, 83), "Repair -- No damage!", m_sharedGUIStyle);
        }
        
        //Leave button
        if (!m_inGameMenuIsOpen && GUI.Button(new Rect(512, 687, 176, 110), "", "label"))
        {
            m_PlayerHasDockedAtCapital = false;
            Screen.showCursor = false;
            m_thisPlayerHP.gameObject.GetComponent<PlayerControlScript>().TellPlayerStopDocking();
            m_currentCShipPanel = CShipScreen.DualPanel;
            StartCoroutine(AnimateCShipPanel(796));
            StartCoroutine(AnimatePlayerPanel(408));
            
            //Clear dragged item
            m_currentDraggedItem = null;
            m_currentDraggedItemInventoryId = -1;
            m_currentDraggedItemIsFromPlayerInv = false;
            m_cShipGameObject.GetComponent<NetworkInventory>().RequestServerCancel(m_currentTicket);
        }
    }
    
    void DrawInGameMenu()
    {
        GUI.DrawTexture(new Rect(305, 130, 290, 620), m_menuBackground);
        
        if (GUI.Button(new Rect(308, 430, 284, 130), "QUIT TO MENU", m_sharedGUIStyle))
        {
            m_gameStateController.GetComponent<GameStateController>().WipeConnectionInfo();
            Application.LoadLevel(0);
        }
        
        if (GUI.Button(new Rect(308, 560, 284, 130), "QUIT TO DESKTOP", m_sharedGUIStyle))
        {
            Application.Quit();
        }
        
        if (GUI.Button(new Rect(308, 300, 284, 130), "CONTINUE", m_sharedGUIStyle))
        {
            ToggleMenuState();
        }
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
    
    void DrawMap()
    {
        //Store gui matrix, restore to identity to remove scaling
        Matrix4x4 oldGUIMat = GUI.matrix;
        GUI.matrix = Matrix4x4.identity;
        
        //Map should be screen.height * screen.height, center on 1/2 screen.width
        GUI.DrawTexture(new Rect((Screen.width * 0.5f) - Screen.height * 0.5f, 0, Screen.height, Screen.height), m_mapOverlay);
        
        //Now draw shizz
        
        //Player - self
        if (m_thisPlayerHP != null)
        {
            Vector2 playerSpotPos = WorldToMapPos(m_thisPlayerHP.transform.position);
            GUI.DrawTexture(new Rect(playerSpotPos.x - (m_blobSize * 0.5f), playerSpotPos.y - (m_blobSize * 0.5f), m_blobSize, m_blobSize), m_selfPBlob);
        }
        else
        {
            Vector2 playerSpotPos = WorldToMapPos(Camera.main.transform.position);
            GUI.DrawTexture(new Rect(playerSpotPos.x - (m_blobSize * 0.5f), playerSpotPos.y - (m_blobSize * 0.5f), m_blobSize, m_blobSize), m_selfPBlob);
        }
        
        //Player - others
        if (m_playerShips != null)
        {
            foreach (GameObject player in m_playerShips)
            {
                //if(player != null && player != thisPlayerHP.gameObject)
                if (player && (!m_thisPlayerHP || player != m_thisPlayerHP.gameObject))
                {
                    Vector2 playPos = WorldToMapPos(player.transform.position);
                    GUI.DrawTexture(new Rect(playPos.x - (m_blobSize * 0.5f), playPos.y - (m_blobSize * 0.5f), m_blobSize, m_blobSize), m_otherPBlob);
                    GUI.Label(new Rect(playPos.x - (m_blobSize * 1.5f), playPos.y + (m_blobSize * 0.5f), 75, 40),
                              m_gameStateController.GetComponent<GameStateController>().GetNameFromNetworkPlayer(player.GetComponent<PlayerControlScript>().GetOwner()));
                }
            }
        }
        else
        {
            m_playerShips = GameObject.FindGameObjectsWithTag("Player");
        }
        
        //CShip
        if (m_cShipGameObject != null)
        {
            Vector2 cshipPos = WorldToMapPos(m_cShipGameObject.transform.position);
            GUI.DrawTexture(new Rect(cshipPos.x - (m_blobSize * 0.5f), cshipPos.y - (m_blobSize * 0.5f), m_blobSize, m_blobSize), m_cShipBlob);
        }
        else
            m_cShipGameObject = GameObject.FindGameObjectWithTag("Capital");
        
        //Enemies?
        if (m_pingedEnemies != null)
        {
            foreach (GameObject enemy in m_pingedEnemies)
            {
                if (enemy != null)// && )IsEnemyInViewableRange(enemy.transform.position))
                {
                    Vector2 pingPos = WorldToMapPos(enemy.transform.position);
                    if(enemy.GetComponent<EnemyScript>().GetShipSize() == ShipSize.Utility)
                    {
                        GUI.DrawTexture(new Rect(pingPos.x - (m_blobSize * 0.5f), pingPos.y - (m_blobSize * 0.5f), m_blobSize, m_blobSize), m_specEnemyBlob);
                    }
                    else
                    {
                        GUI.DrawTexture(new Rect(pingPos.x - (m_blobSize * 0.5f), pingPos.y - (m_blobSize * 0.5f), m_blobSize, m_blobSize), m_enemyBlob);
                    }
                }
            }
        }
        
        //Missiles
        if(m_pingedMissiles != null && m_pingedMissiles.Length != 0)
        {
            foreach(GameObject bullet in m_pingedMissiles)
            {
                if(bullet != null)
                {
                    Vector2 pingPos = WorldToMapPos(bullet.transform.position);
                    GUI.DrawTexture(new Rect(pingPos.x - (m_blobSize * 0.2f), pingPos.y - (m_blobSize * 0.2f), m_blobSize, m_blobSize), m_specEnemyBlob);
                }
            }
        }
        
        //Shops!
        for (int i = 0; i < m_shops.Length; i++)
        {
            Vector2 shopPos = WorldToMapPos(m_shops[i].transform.position);
            GUI.Label(new Rect(shopPos.x - 10, shopPos.y - 10, 20, 20), "$", "Label");
        }
        
        //When we're done, reset the gui matrix
        GUI.matrix = oldGUIMat;
    }
    
    void DrawSmallFollowMap()
    {
        //Store gui matrix, restore to identity to remove scaling
        Matrix4x4 oldGUIMat = GUI.matrix;
        GUI.matrix = Matrix4x4.identity;
        float pixelGapPercent = (53.0f) / (Screen.height * 0.5f);
        float mapSize = 280.0f;
        
        //If map is bottom left:
        //Map should be screen.height/5 * screen.height/5, centered on (screen.height/5, (screen.height/5)*4)
        //GUI.DrawTexture(new Rect((Screen.width * 0.5f) - Screen.height * 0.5f, 0, Screen.height, Screen.height), m_mapOverlay);
        
        //Step one: Get 'imagepos' from playerPos
        Vector2 imagePos = Vector2.zero;
        if (m_thisPlayerHP != null)
            imagePos = new Vector2(m_thisPlayerHP.transform.position.x / mapSize, m_thisPlayerHP.transform.position.y / mapSize);
        else
            imagePos = new Vector2(Camera.main.transform.position.x / mapSize, Camera.main.transform.position.y / mapSize);
        Vector2 playerPos = imagePos;
        imagePos *= (1.0f - pixelGapPercent);
        imagePos.x /= 2;
        imagePos.x += 0.5f;
        imagePos.y /= 2;
        imagePos.y += 0.5f;
        
        //Step two: draw map around this area
        Rect drawRect = new Rect(0, (Screen.height / 4.0f) * 3.0f, Screen.height / 4.0f, Screen.height / 4.0f);
        float texDrawArea = 0.25f;
        GUI.DrawTextureWithTexCoords(drawRect,
                                     m_mapOverlay,
                                     //new Rect((imagePos.x - 0.125f) * (1.0f + pixelGapPercent), imagePos.y - 0.125f * (1.0f + pixelGapPercent), 0.25f, 0.25f));
                                     /*new Rect( (imagePos.x - (texDrawArea / 2)) * (1.0f - pixelGapPercent), 
                       (imagePos.y - (texDrawArea / 2)) * (1.0f - pixelGapPercent), 
                       texDrawArea, texDrawArea));*/
                                     new Rect((imagePos.x - (texDrawArea / 2)), (imagePos.y - (texDrawArea / 2)), texDrawArea, texDrawArea));
        //GUI.DrawTexture (new Rect(0, (Screen.height / 4.0f) * 3.0f, Screen.height / 4.0f, Screen.height / 4.0f), m_mapOverlay);
        
        GUI.DrawTexture(new Rect((Screen.height * 0.125f) - (m_blobSize * 0.5f), ((Screen.height * 0.125f) * 7.0f) - (m_blobSize * 0.5f), m_blobSize, m_blobSize), m_selfPBlob);
        
        Vector2 playerNormalDrawPos = Vector2.zero;
        if (m_thisPlayerHP)
            playerNormalDrawPos = WorldToSmallMapPos(m_thisPlayerHP.transform.position);
        else
            playerNormalDrawPos = WorldToSmallMapPos(new Vector3(Camera.main.transform.position.x, Camera.main.transform.position.y, 10.0f));
        
        //Step three: draw CShip blob
        if (m_cShipGameObject != null)
        {
            Vector2 cshipRelMapPos = new Vector2(m_cShipGameObject.transform.position.x / mapSize, m_cShipGameObject.transform.position.y / mapSize);
            
            //Debug.Log ("CShipPos: " + cshipPos + System.Environment.NewLine + "PlayerPos: " + playerPos + ".");
            
            //Relativise cship -> player
            cshipRelMapPos.x -= playerPos.x;
            cshipRelMapPos.y -= playerPos.y;
            //cshipPos will be at max ~0.4
            
            
            //Debug.Log ("Gives relative cshipPos of: " + cshipPos + ".");
            
            Vector2 drawPos = Vector2.zero;
            drawPos.x = cshipRelMapPos.x * (Screen.height * (0.5f - (pixelGapPercent * 0.5f)));
            drawPos.y = cshipRelMapPos.y * (Screen.height * (0.5f - (pixelGapPercent * 0.5f)));
            
            Vector2 finalDrawPos = new Vector2((Screen.height * 0.125f) + drawPos.x,
                                               ((Screen.height * 0.125f) * 7.0f) - drawPos.y);
            if (drawRect.Contains(finalDrawPos))
            {
                GUI.DrawTexture(new Rect(finalDrawPos.x - (m_blobSize * 0.5f),
                                         finalDrawPos.y - (m_blobSize * 0.5f),
                                         m_blobSize, m_blobSize), m_cShipBlob);
            }
        }
        
        //Draw self:
        /*if(thisPlayerHP != null)
        {
            Vector2 playerSpotPos = WorldToSmallMapPos(thisPlayerHP.transform.position);
            GUI.DrawTexture(new Rect(playerSpotPos.x - (m_blobSize * 0.25f), playerSpotPos.y - (m_blobSize * 0.25f), m_blobSize * 0.5f, m_blobSize * 0.5f), m_selfPBlob);
            //Debug.Log ("Attempting to draw player blob at " + playerSpotPos.ToString() + ".");
            GUI.DrawTexture (new Rect(33, 800, 5, 5), m_selfPBlob);
        }
        else
        {
            Vector2 playerSpotPos = WorldToSmallMapPos(Camera.main.transform.position);
            GUI.DrawTexture(new Rect(playerSpotPos.x - (m_blobSize * 0.25f), playerSpotPos.y - (m_blobSize * 0.25f), m_blobSize * 0.5f, m_blobSize * 0.5f), m_selfPBlob);
        }*/
        
        //Draw other players:
        if (m_playerShips != null)
        {
            foreach (GameObject player in m_playerShips)
            {
                //if(player != null && player != thisPlayerHP.gameObject)
                if (player && (!m_thisPlayerHP || player != m_thisPlayerHP.gameObject))
                {
                    Vector2 playerMapPos = new Vector2(player.transform.position.x / mapSize, player.transform.position.y / mapSize);
                    
                    playerMapPos.x -= playerPos.x;
                    playerMapPos.y -= playerPos.y;
                    
                    Vector3 playerDrawPos = Vector2.zero;
                    playerDrawPos.x = playerMapPos.x * (Screen.height * (0.5f));
                    playerDrawPos.y = playerMapPos.y * (Screen.height * (0.5f));
                    
                    Vector2 finalDrawPos = new Vector2((Screen.height * 0.125f) + playerDrawPos.x,
                                                       ((Screen.height * 0.125f) * 7.0f) - playerDrawPos.y);
                    
                    if (drawRect.Contains(finalDrawPos))
                    {
                        GUI.DrawTexture(new Rect(finalDrawPos.x - (m_blobSize * 0.5f),
                                                 finalDrawPos.y - (m_blobSize * 0.5f),
                                                 m_blobSize, m_blobSize), m_otherPBlob);
                    }
                }
            }
        }
        else
        {
            m_playerShips = GameObject.FindGameObjectsWithTag("Player");
        }
        
        //Enemies
        if (m_pingedEnemies != null)
        {
            foreach (GameObject enemy in m_pingedEnemies)
            {
                if (enemy != null)
                {
                    Vector2 enemyMapPos = new Vector2(enemy.transform.position.x / mapSize, enemy.transform.position.y / mapSize);
                    enemyMapPos.x -= playerPos.x;
                    enemyMapPos.y -= playerPos.y;
                    
                    Vector3 enemyDrawPos = Vector2.zero;
                    enemyDrawPos.x = enemyMapPos.x * (Screen.height * (0.5f));
                    enemyDrawPos.y = enemyMapPos.y * (Screen.height * (0.5f));
                    
                    Vector2 finalDrawPos = new Vector2((Screen.height * 0.125f) + enemyDrawPos.x,
                                                       ((Screen.height * 0.125f) * 7.0f) - enemyDrawPos.y);
                    
                    if (drawRect.Contains(finalDrawPos))
                    {
                        if(enemy.GetComponent<EnemyScript>().GetShipSize() == ShipSize.Utility)
                        {
                            GUI.DrawTexture(new Rect(finalDrawPos.x - (m_blobSize * 0.5f),
                                                     finalDrawPos.y - (m_blobSize * 0.5f),
                                                     m_blobSize, m_blobSize), m_specEnemyBlob);
                        }
                        else
                        {
                            GUI.DrawTexture(new Rect(finalDrawPos.x - (m_blobSize * 0.5f),
                                                     finalDrawPos.y - (m_blobSize * 0.5f),
                                                     m_blobSize, m_blobSize), m_enemyBlob);
                        }
                    }
                    
                    /*Vector2 pingPos = WorldToSmallMapPos(enemy.transform.position);
                    GUI.DrawTexture(new Rect(pingPos.x - (m_blobSize * 0.25f), pingPos.y - (m_blobSize * 0.25f), m_blobSize * 0.5f, m_blobSize * 0.5f), m_enemyBlob);*/
                }
            }
        }
        
        //Missiles
        if(m_pingedMissiles != null && m_pingedMissiles.Length != 0)
        {
            foreach(GameObject bullet in m_pingedMissiles)
            {
                if(bullet != null)
                {
                    Vector2 enemyMapPos = new Vector2(bullet.transform.position.x / mapSize, bullet.transform.position.y / mapSize);
                    enemyMapPos.x -= playerPos.x;
                    enemyMapPos.y -= playerPos.y;
                    
                    Vector3 enemyDrawPos = Vector2.zero;
                    enemyDrawPos.x = enemyMapPos.x * (Screen.height * (0.5f));
                    enemyDrawPos.y = enemyMapPos.y * (Screen.height * (0.5f));
                    
                    Vector2 finalDrawPos = new Vector2((Screen.height * 0.125f) + enemyDrawPos.x,
                                                       ((Screen.height * 0.125f) * 7.0f) - enemyDrawPos.y);
                    
                    if (drawRect.Contains(finalDrawPos))
                    {
                        GUI.DrawTexture(new Rect(finalDrawPos.x - (m_blobSize * 0.2f), finalDrawPos.y - (m_blobSize * 0.2f), m_blobSize, m_blobSize), m_specEnemyBlob);
                    }
                }
            }
        }
        
        //Always reset the gui!
        GUI.matrix = oldGUIMat;
    }
    
    void DrawSmallMap()
    {
        Matrix4x4 oldGUIMat = GUI.matrix;
        GUI.matrix = Matrix4x4.identity;
        
        GUI.DrawTexture(new Rect(0, (Screen.height * 0.25f) * 3.0f, Screen.height * 0.25f, Screen.height * 0.25f), m_mapOverlay);
        
        //Draw Self
        if (m_thisPlayerHP != null)
        {
            Vector2 playerSpotPos = WorldToSmallMapPos(m_thisPlayerHP.transform.position);
            GUI.DrawTexture(new Rect(playerSpotPos.x - (m_blobSize * 0.25f), playerSpotPos.y - (m_blobSize * 0.25f), m_blobSize * 0.5f, m_blobSize * 0.5f), m_selfPBlob);
        }
        else
        {
            Vector2 playerSpotPos = WorldToSmallMapPos(Camera.main.transform.position);
            GUI.DrawTexture(new Rect(playerSpotPos.x - (m_blobSize * 0.25f), playerSpotPos.y - (m_blobSize * 0.25f), m_blobSize * 0.5f, m_blobSize * 0.5f), m_selfPBlob);
        }
        
        //Draw CShip
        //CShip
        if (m_cShipGameObject != null)
        {
            Vector2 cshipPos = WorldToSmallMapPos(m_cShipGameObject.transform.position);
            GUI.DrawTexture(new Rect(cshipPos.x - (m_blobSize * 0.25f), cshipPos.y - (m_blobSize * 0.25f), m_blobSize * 0.5f, m_blobSize * 0.5f), m_cShipBlob);
        }
        else
            m_cShipGameObject = GameObject.FindGameObjectWithTag("Capital");
        
        //Draw others
        if (m_playerShips != null)
        {
            foreach (GameObject player in m_playerShips)
            {
                //if(player != null && player != thisPlayerHP.gameObject)
                if (player && (!m_thisPlayerHP || player != m_thisPlayerHP.gameObject))
                {
                    Vector2 playPos = WorldToSmallMapPos(player.transform.position);
                    GUI.DrawTexture(new Rect(playPos.x - (m_blobSize * 0.25f), playPos.y - (m_blobSize * 0.25f), m_blobSize * 0.5f, m_blobSize * 0.5f), m_otherPBlob);
                }
            }
        }
        else
        {
            m_playerShips = GameObject.FindGameObjectsWithTag("Player");
        }
        
        //Draw enemies
        if (m_pingedEnemies != null)
        {
            foreach (GameObject enemy in m_pingedEnemies)
            {
                if (enemy != null)
                {
                    //Check if enemy is in viewable range
                    //if(IsEnemyInViewableRange(enemy.transform.position))
                    //{
                    Vector2 pingPos = WorldToSmallMapPos(enemy.transform.position);
                    if(enemy.GetComponent<EnemyScript>().GetShipSize() == ShipSize.Utility)
                    {
                        GUI.DrawTexture(new Rect(pingPos.x - (m_blobSize * 0.25f), pingPos.y - (m_blobSize * 0.25f), m_blobSize * 0.5f, m_blobSize * 0.5f), m_specEnemyBlob);
                    }
                    else
                    {
                        GUI.DrawTexture(new Rect(pingPos.x - (m_blobSize * 0.25f), pingPos.y - (m_blobSize * 0.25f), m_blobSize * 0.5f, m_blobSize * 0.5f), m_enemyBlob);
                    }
                    //}
                }
            }
        }
        
        //Missiles
        if(m_pingedMissiles != null && m_pingedMissiles.Length != 0)
        {
            foreach(GameObject bullet in m_pingedMissiles)
            {
                if(bullet != null)
                {
                    Vector2 pingPos = WorldToSmallMapPos(bullet.transform.position);
                    GUI.DrawTexture(new Rect(pingPos.x - (m_blobSize * 0.2f), pingPos.y - (m_blobSize * 0.2f), m_blobSize, m_blobSize), m_specEnemyBlob);
                }
            }
        }
        
        GUI.matrix = oldGUIMat;
    }
    
    void DrawLeftPanel()
    {
        float playerSourceWidth = m_playerPanelXWidth / 408.0f;
        GUI.DrawTexture(new Rect(394, 250, 408, 400), m_DockInventoryBorder);
        GUI.DrawTextureWithTexCoords(new Rect(394, 250, m_playerPanelXWidth, 400), m_DockPlayerImage, new Rect(1 - playerSourceWidth, 1, playerSourceWidth, 1));
        
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
            
            GUI.DrawTextureWithTexCoords(weaponTemp, m_thisPlayerHP.GetComponent<PlayerControlScript>().m_equippedWeaponItem.GetComponent<ItemScript>().GetIcon(), weaponSource);
            GUI.DrawTextureWithTexCoords(shieldTemp, m_thisPlayerHP.GetComponent<PlayerControlScript>().m_equippedShieldItem.GetComponent<ItemScript>().GetIcon(), shieldSource);
            GUI.DrawTextureWithTexCoords(platingTemp, m_thisPlayerHP.GetComponent<PlayerControlScript>().m_equippedPlatingItem.GetComponent<ItemScript>().GetIcon(), platingSource);
            GUI.DrawTextureWithTexCoords(engineTemp, m_thisPlayerHP.GetComponent<PlayerControlScript>().m_equippedEngineItem.GetComponent<ItemScript>().GetIcon(), engineSource);
        }
    }
    
    void DrawRightPanel()
    {
        float cshipSourcewidth = (1204.0f - (float)m_cShipPanelXPos) / 408.0f;
        GUI.DrawTexture(new Rect(796, 250, 408, 400), m_DockInventoryBorder);
        GUI.DrawTextureWithTexCoords(new Rect(m_cShipPanelXPos, 250, (1204 - m_cShipPanelXPos), 400), m_DockCShipImage, new Rect(0, 0, cshipSourcewidth, 1));
        
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
            
            GameObject[] turrets = m_cShipGameObject.GetComponent<CapitalShipScript>().GetAttachedTurrets();
            GUI.DrawTextureWithTexCoords(weapon1Temp, turrets[0].GetComponent<ItemScript>().GetIcon(), weapon1Source);
            GUI.DrawTextureWithTexCoords(weapon2Temp, turrets[1].GetComponent<ItemScript>().GetIcon(), weapon2Source);
            GUI.DrawTextureWithTexCoords(weapon3Temp, turrets[2].GetComponent<ItemScript>().GetIcon(), weapon3Source);
            GUI.DrawTextureWithTexCoords(weapon4Temp, turrets[3].GetComponent<ItemScript>().GetIcon(), weapon4Source);
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
                List<Player> players = m_gameStateController.GetComponent<GameStateController>().m_connectedPlayers;
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
            GUI.Label(new Rect(550, 150, 500, 100), "Time remaining: " + System.Math.Round(currEventSc.m_timer, 0));
            
            //Draw each of the buttons
            for (int i = 0; i < currEventSc.m_possibleOptions.Length; i++)
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
                        if (GUI.Button(new Rect(450, 400 + (i * 100), 700, 80), currEventSc.m_possibleOptions[i].m_optionText + ": #" + currEventSc.m_optionVotes[i]))
                        {
                            eventText = currEventSc.ActivateOption(i);
                            m_eventIsOnOutcome = true;
                            m_hostShouldSelectTiebreaker = false;
                        }
                    }
                    else
                    {
                        if (GUI.Button(new Rect(450, 400 + (i * 100), 700, 80), currEventSc.m_possibleOptions[i].m_optionText + ": #" + currEventSc.m_optionVotes[i]))
                        {
                            currEventSc.VoteForOption(i);
                        }
                    }
                }
            }
        }
    }
    
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
                    (m_thisPlayerHP.GetComponent<PlayerWeaponScript>().FindAttachedWeapon().GetComponent<WeaponScript>().GetBulletMaxDistance() * 0.5f).Squared());
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
            case GameState.ClientConnectingMenu:
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
            case GameState.ClientConnectingMenu:
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
        m_lockonTime = 0.0f;
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
    
    void HostButtonActivate()
    {
        if (m_username != "Name" && m_username != "")
        {
            m_gameStateController.GetComponent<GameStateController>().PlayerRequestsToHostGame(m_username, m_hostShouldStartSpec);
            Time.timeScale = 1.0f;
            PlayerPrefs.SetString("LastUsername", m_username);
        }
    }
    void JoinButtonActivate()
    {
        if (m_username != "Name" && m_username != "")
        {
            m_gameStateController.GetComponent<GameStateController>().SwitchToJoinScreen();
            m_IPField = PlayerPrefs.GetString("LastIP");
            PlayerPrefs.SetString("LastUsername", m_username);
        }
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
        m_gameTimer = 0;
        GameStateController gsc = m_gameStateController.GetComponent<GameStateController>();
        List<Player> playersL = gsc.m_connectedPlayers;
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
    
    void ClientConnectJoinActivate()
    {
        m_gameStateController.GetComponent<GameStateController>().PlayerRequestsToJoinGame(m_IPField, m_username, 6677);
        PlayerPrefs.SetString("LastIP", m_IPField);
        Time.timeScale = 1.0f;
    }
    void ClientConnectBackActivate()
    {
        m_gameStateController.GetComponent<GameStateController>().BackToMenu();
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

    [RPC] void TellOtherPlayersPlayerHasLeft()
    {
        m_playerHasAlreadyLeft = true;
        m_PlayerRequestsRound = true;
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
   
    Vector2 WorldToMapPos(Vector3 worldPos)
    {
        //gap is 24px, this as a percentage of the screen changes dependant upon screen size
        //if the screen is 900 high, the gap of 24 as a percentage is 2.666666667% or 0.0266666666667
        float pixelGapPercent = 53.0f / (Screen.height);

        Vector2 output = Vector2.zero;
        //World is still -275 -> 275
        //Map is now:
        //X:		(Screen.width * 0.5f) - (Screen.height * (0.5f - pixelGapPercent)) -> (Screen.width * 0.5f + (Screen.height * (0.5f - pixelGapPercent))
        //Y:		(pixelGapPercent * Screen.height) -> (Screen.height - (pixelGapPercent * Screen.height))

        //x
        output.x = (Screen.width * 0.5f) + ((worldPos.x / 275.0f) * (Screen.height * (0.5f - pixelGapPercent)));

        //y
        output.y = (Screen.height * 0.5f) - ((worldPos.y / 275.0f) * (Screen.height * (0.5f - pixelGapPercent)));
        return output;
    }
    Vector2 WorldToSmallMapPos(Vector3 worldPos)
    {
        float pixelGapPercent = (53.0f * 0.25f) / (Screen.height);
        //World is still -275 -> 275
        //Map is now:
        //X:		0 -> (Screen.height * 0.25f)
        //Y:		(Screen.height * 0.25f) * 3.0f -> Screen.height
        Vector2 output = Vector2.zero;

        //X:
        //output.x = (worldPos.x / 275.0f) * (Screen.height * 0.25f);
        output.x = (Screen.height * 0.125f) + ((worldPos.x / 275.0f) * (Screen.height * (0.125f - pixelGapPercent)));

        //Y:
        //output.y = ((Screen.height * 0.25f) * 3.0f) + ((worldPos.y / 275.0f) * Screen.height);
        output.y = ((Screen.height * 0.125f) * 7.0f) - ((worldPos.y / 275.0f) * (Screen.height * (0.125f - pixelGapPercent)));

        return output;
    }

    bool IsEnemyInViewableRange(Vector3 position)
    {
        float maxDist = 65.0f;
        //If the enemy is within distance X of any player of CShip, then return true

        if (m_thisPlayerHP != null)
        {
            float distToPlayer = Vector3.Distance(position, m_thisPlayerHP.transform.position);
            if (distToPlayer <= maxDist)
                return true;
        }
        else
        {
            float distToCam = Vector2.Distance(new Vector2(position.x, position.y), new Vector2(Camera.main.transform.position.x, Camera.main.transform.position.y));
            if (distToCam <= maxDist)
                return true;
        }

        if (m_cShipGameObject != null)
        {
            float distToCShip = Vector3.Distance(position, m_cShipGameObject.transform.position);
            if (distToCShip <= maxDist)
                return true;
        }

        if (m_playerShips != null)
        {
            for (int i = 0; i < m_playerShips.Length; i++)
            {
                float dist = Vector3.Distance(position, m_playerShips[i].transform.position);
                if (dist <= maxDist)
                    return true;
            }
        }

        return false;
    }
    
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

	bool IsMouseDownZero()
	{
		return !m_previousMouseZero && m_mouseZero;
	}

	bool IsMouseUpZero()
	{
		return m_previousMouseZero && !m_mouseZero;
	}

    void HandleItemDrop(bool isLeftPanel, Vector2 mousePos)
    {
        NetworkInventory inventory = m_cShipGameObject.GetComponent<NetworkInventory>();

        //Depending on where the cursor is when the mouse is released, decide what happens to the item
        if (isLeftPanel)
        {
            //If over player inventory, try to store there.
            if (m_LeftPanelPlayerRect.Contains(mousePos))
            {
                //If the item was originally from here, we don't need to do anything
                if (!m_currentDraggedItemIsFromPlayerInv)
                {
                    if (!m_thisPlayerHP.GetComponent<PlayerControlScript>().InventoryIsFull())
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
                    if (!m_thisPlayerHP.GetComponent<PlayerControlScript>().InventoryIsFull())
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
		if (m_currentDraggedItem.GetComponent<ItemScript>().m_typeOfItem == checkIs)
		{
			//if the item is a weapon, equip plz!
			if (m_currentDraggedItemIsFromPlayerInv)
			{
				m_thisPlayerHP.GetComponent<PlayerControlScript>().EquipItemInSlot (m_currentDraggedItemInventoryId);
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
		if (m_currentDraggedItem.GetComponent<ItemScript>().m_typeOfItem == ItemType.CapitalWeapon)
		{
			if (m_currentDraggedItemIsFromPlayerInv)
			{
				CapitalShipScript cShip = m_cShipGameObject.GetComponent<CapitalShipScript>();
				GameObject oldTurret = cShip.GetAttachedTurrets()[turretID - 1];
				PlayerControlScript player = m_thisPlayerHP.GetComponent<PlayerControlScript>();						
				
				cShip.TellServerEquipTurret (turretID, m_currentDraggedItem.gameObject);
				player.RemoveItemFromInventory (m_currentDraggedItem.gameObject);					
				
				if (player.InventoryIsFull())
				{
					// Move the item to the CShip inventory since the players inventory is full
					inventory.RequestServerAdd (oldTurret.GetComponent<ItemScript>());
					StartCoroutine (AwaitTicketRequestResponse (inventory, RequestType.ItemAdd, ItemOwner.CShipEquipment, ItemOwner.NetworkInventory));
				}
				
				else
				{
					player.AddItemToInventory (oldTurret);
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

    void DrawHoverText(string text, Vector2 mousePos)
    {
        float width = 200;
        float height = m_hoverBoxTextStyle.CalcHeight(new GUIContent(text), 200);
        GUI.Label(new Rect(mousePos.x + 10, mousePos.y - 5, width, height), text, m_hoverBoxTextStyle);
    }

	private enum ItemOwner
	{
		PlayerInventory = 1,
		NetworkInventory = 2,
		PlayerEquipment = 3,
		CShipEquipment = 4
	}

	// WARNING! EXTREMELY HAZARDOUS CODE LIES BEYOND THIS POINT! DO NOT LOOK AT THIS FUNCTION OR YOU MAY TURN BLIND!
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
						m_thisPlayerHP.GetComponent<PlayerControlScript>().RemoveItemFromInventory(GameObject.FindGameObjectWithTag("ItemManager").GetComponent<ItemIDHolder>().GetItemWithID(itemID));
					}
                }
                else
                    StartCoroutine(CountdownTransferFailedPopup(true));           

	            break;
	        }
            case RequestType.ItemTake:
	        {
	            m_currentTicket = inventory.GetItemRequestResponse();
	            //Debug.Log("Ticket: " + m_currentTicket.uniqueID + " with ID: " + m_currentTicket.itemID + " at index: " + m_currentTicket.itemIndex + ".");
                
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
										m_thisPlayerHP.GetComponent<PlayerControlScript>().AddItemToInventory (item.gameObject);
                            
                                        if(fromShop)
                                        {
                                            m_shopConfirmBuy = false;
                                            m_confirmBuyItem = null;
                                            m_thisPlayerHP.GetComponent<PlayerControlScript>().RemoveSpaceBucks(costIfShop);
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
									if (item.m_typeOfItem == ItemType.CapitalWeapon && inventory.RemoveItemFromServer (m_currentTicket))
									{
										PlayerControlScript player = m_thisPlayerHP.GetComponent<PlayerControlScript>();
										CapitalShipScript cShip = m_cShipGameObject.GetComponent<CapitalShipScript>();
										GameObject oldTurret = cShip.GetAttachedTurrets()[equipmentSlot - 1];

										cShip.TellServerEquipTurret (equipmentSlot, item.gameObject);
										
										if (player.InventoryIsFull())
										{
											// Move the item to the CShip inventory since the players inventory is full
											inventory.RequestServerAdd (oldTurret.GetComponent<ItemScript>());
											StartCoroutine (AwaitTicketRequestResponse (inventory, RequestType.ItemAdd, to, from));
										}

										else
										{
											player.AddItemToInventory (oldTurret);
										}
									}

									break;
								}
								case ItemOwner.PlayerEquipment:
								{	
									if (item.m_typeOfItem != ItemType.CapitalWeapon && inventory.RemoveItemFromServer (m_currentTicket))
									{
										PlayerControlScript player = m_thisPlayerHP.GetComponent<PlayerControlScript>();
										GameObject oldEquipment = player.GetEquipmentFromSlot (equipmentSlot);
                                        
										if (player.InventoryIsFull())
										{
											GameObject lastItem = player.m_playerInventory[player.m_playerInventory.Count - 1];

											// This little wonder makes me want to vomit and should only be used until the demo night
											// Remove the last item from the players inventory then equip the new item from that
											player.RemoveItemFromInventory (lastItem);
											player.AddItemToInventory (item.gameObject);
											player.EquipItemInSlot (player.m_playerInventory.Count - 1);
											player.RemoveItemFromInventory (oldEquipment);
											player.AddItemToInventory (lastItem);

											// Move the item to the CShip inventory since the players inventory is full
											inventory.RequestServerAdd (oldEquipment.GetComponent<ItemScript>());
											StartCoroutine (AwaitTicketRequestResponse (inventory, RequestType.ItemAdd, to, from));
										}
										else
										{
											player.AddItemToInventory (item.gameObject);
											player.EquipItemInSlot (player.m_playerInventory.Count - 1);
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
            m_gameStateController.GetComponent<GameStateController>().RequestFastSpawnOfPlayer(player);
    }
    
    [RPC] void PropagateRespawnRequest(NetworkPlayer player)
    {
        m_gameStateController.GetComponent<GameStateController>().RequestFastSpawnOfPlayer(player);
    }

    public void SetActiveEvent(GameObject currEvent, NetworkPlayer causer)
    {
        //Set up event vars
        m_eventIsActive = true;
        m_eventIsOnOutcome = false;
        currEventSc = currEvent.GetComponent<EventScript>();
        eventText = currEventSc.m_EventText;
        eventTriggerer = m_gameStateController.GetComponent<GameStateController>().GetNameFromNetworkPlayer(causer);

        //Freeze all the baddies
        FreezeAllEnemies();

        //Freeze player control
        m_thisPlayerHP.gameObject.GetComponent<PlayerControlScript>().TellShipStopRecievingInput();
        m_thisPlayerHP.gameObject.rigidbody.isKinematic = true;

        //Stop CShip from moving
        m_cShipGameObject.GetComponent<CapitalShipScript>().shouldStart = false;
        m_cShipGameObject.rigidbody.constraints = RigidbodyConstraints.FreezePosition | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY;

        //Stop spawners from spawning
        m_gameStateController.GetComponent<GameStateController>().RequestSpawnerPause();

        //Init #votes required
        m_playerVotes = new int[m_gameStateController.GetComponent<GameStateController>().m_connectedPlayers.Count];
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
        m_cShipGameObject.GetComponent<CapitalShipScript>().shouldStart = true;

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
        int numPlayers = m_gameStateController.GetComponent<GameStateController>().m_connectedPlayers.Count;
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
        currEventSc.selectedPlayer = player;
    }

    [RPC] void VoteForContinue(NetworkMessageInfo info)
    {
        continueVotes++;
        Debug.Log("Recieved vote from player: " + info.sender + ", bringing total to: " + continueVotes);
        if (continueVotes >= m_gameStateController.GetComponent<GameStateController>().m_connectedPlayers.Count)
            networkView.RPC("PropagateContinueComplete", RPCMode.All);
    }
    void VoteForContinue()
    {
        continueVotes++;
        Debug.Log("Recieved vote from host, bringing total to: " + continueVotes);
        if (continueVotes >= m_gameStateController.GetComponent<GameStateController>().m_connectedPlayers.Count)
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
        m_playerVotes = new int[m_gameStateController.GetComponent<GameStateController>().m_connectedPlayers.Count];
        eventText = text;
        m_playerSelectTimer = 20.0f;
        m_eventIsOnPlayerSelect = true;
        m_lastVote = "";
        networkView.RPC("PropagateEventPlayerSelectionText", RPCMode.Others, text);
    }
    [RPC] void PropagateEventPlayerSelectionText(string text)
    {
        m_playerVotes = new int[m_gameStateController.GetComponent<GameStateController>().m_connectedPlayers.Count];
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
                    m_gameTimer = 0.0f;
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
            case GameState.ClientConnectingMenu:
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
        m_shouldShowDisconnectedSplash = true;
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
        m_ArenaClearOfEnemies = false;
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

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum GameState
{
    MainMenu = 0,
    HostMenu = 1,
    ClientInputIP = 2,
    ClientMenu = 3,
    MapMenu = 4,
    OptionMenu = 5,
    AttemptingConnect = 6,
    FailedConnectName = 7,
    LoadingScreen = 8,
    InGame = 10,
    InGameConnectionLost = 11,
    InGameCShipDock = 12,
    InGameShopDock = 13,
    InGameGameOver = 14,
    InGameMenu = 15
}

[System.Serializable]
public class Player
{
    public NetworkPlayer m_netPlayer;
    public string m_name;

    public Player(NetworkPlayer nP, string name)
    {
        m_netPlayer = nP;
        m_name = name;
    }
}

[System.Serializable]
public class DeadPlayer
{
    public Player m_playerObject;
    public float m_deadTimer;
    public bool m_needsChecking = true;

    public DeadPlayer(Player player)
    {
        m_playerObject = player;
        m_deadTimer = 45.0f;
    }
}

/// <summary>
/// This class handles the current state of the game. I.E., whether the game is on menu, in game, which map, etc.
/// </summary>
public class GameStateController : MonoBehaviour
{

    class LossConfirmation
    {
        public NetworkPlayer player;
        public bool confirmed;
    }

    class LossCamConfirmation
    {
        public NetworkPlayer player;
        public bool camInPositionConfirmed;
    }
    
    class SceneLoadingConfirmation
    {
        public NetworkPlayer player;
        public bool readyToLoad;
    }

    [SerializeField] List<Player> m_connectedPlayers;
    
    [SerializeField] List<DeadPlayer> m_deadPlayers;

    [SerializeField] GameObject m_SpawnManager;
    [SerializeField] GUIBaseMaster m_GUIManager;
    [SerializeField] GameObject[] m_AsteroidManagers;

    [SerializeField] GameState m_currentGameState = GameState.MainMenu;
    [SerializeField] GameObject m_playerShip;
    [SerializeField] GameObject m_capitalShip;

    [SerializeField] GameObject m_ingameCapitalShip;

    [SerializeField] bool m_shouldCheckForFinished = false;

    [SerializeField] float m_waveTimer = 0;

    [SerializeField] bool m_gameStopped = true;


    int m_numDeadPCs = 0;

    string m_ownName;

    float m_volumeHolder = 1.0f;

    bool m_shouldSpawnRoids = true;
    bool m_inGameMenuIsOpen = false;

    float m_capitalDamageTimer = 0;

    List<LossConfirmation> m_lossConfirmList;
    List<LossCamConfirmation> m_lossCameraConfirmList;
    List<SceneLoadingConfirmation> m_sceneLoadedConfirmList;

    float m_gameTimer = 0;
    bool m_lossTimerBegin = false;
    float m_lossTimer = 0.0f;
    bool m_cshipIsDying = false;
    bool m_isLoadingLevel = false;

    static GameStateController instance;
    

    GameObject m_localPlayer;
    AsyncOperation m_levelChangeOperation;


    #region getset

    public float GetGameTimer()
    {
        return m_gameTimer;
    }
    
    public void SetGameTimer(float gameTimer_)
    {
        m_gameTimer = gameTimer_;
    }

    public List<Player> GetConnectedPlayers()
    {
        return m_connectedPlayers;
    }

    public List<DeadPlayer> GetDeadPlayers()
    {
        return m_deadPlayers;
    }

    public Player GetPlayerObjectFromNP(NetworkPlayer np)
    {
        for (int i = 0; i < m_connectedPlayers.Count; i++)
        {
            if (m_connectedPlayers[i].m_netPlayer == np)
                return m_connectedPlayers[i];
        }

        Debug.LogWarning("Couldn't find Network Player: " + np.ToString());
        return null;
    }

    public string GetNameFromNetworkPlayer(NetworkPlayer np)
    {
        for (int i = 0; i < m_connectedPlayers.Count; i++)
        {
            if (m_connectedPlayers[i].m_netPlayer == np)
                return m_connectedPlayers[i].m_name;
        }

        Debug.LogWarning("Couldn't find Network Player: " + np.ToString());
        return null;
    }

    public string GetNameFromID(int id)
    {
        return m_connectedPlayers[id].m_name;
    }

    public int GetIDFromNetworkPlayer(NetworkPlayer np)
    {
        for (int i = 0; i < m_connectedPlayers.Count; i++)
        {
            if (m_connectedPlayers[i].m_netPlayer == np)
                return i;
        }

        Debug.LogWarning("Couldn't find Network Player: " + np.ToString());
        return -1;
    }

    public int GetIDFromName(string name)
    {
        for (int i = 0; i < m_connectedPlayers.Count; i++)
        {
            if (m_connectedPlayers[i].m_name == name)
                return i;
        }

        Debug.LogWarning("Couldn't find player with name '" + name + "'.");
        return -1;
    }

    public NetworkPlayer GetNetworkPlayerFromID(int id)
    {
        return m_connectedPlayers[id].m_netPlayer;
    }

    public GameObject GetPlayerFromNetworkPlayer(NetworkPlayer np)
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject player in players)
        {
            if (player.GetComponent<PlayerControlScript>().GetOwner() == np)
                return player;
        }

        //Debug.LogWarning ("Couldn't find player with owner: " + np.ToString());
        return null;
    }

    public float GetDeathTimerFromNetworkPlayer(NetworkPlayer np)
    {
        foreach (DeadPlayer player in m_deadPlayers)
        {
            if (player.m_playerObject.m_netPlayer == np)
            {
                return player.m_deadTimer;
            }
        }

        Debug.LogWarning("Couldn't find dead player: " + GetNameFromNetworkPlayer(np) + ".");
        return 0.0f;
    }

    public GameObject GetCapitalShip()
    {
        return m_ingameCapitalShip;
    }

    public GameState GetCurrentGameState()
    {
        return m_currentGameState;
    }

    public void SetCurrentGameState(GameState state_)
    {
        m_currentGameState = state_;
    }

    public static GameStateController Instance()
    {
        return instance;
    }

    #endregion getset

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        instance = this;
    }

    void Start()
    {
        m_volumeHolder = PlayerPrefs.GetFloat("MusicVolume", 1.0f);
        this.audio.volume = m_volumeHolder;
        m_connectedPlayers = new List<Player>();
        m_deadPlayers = new List<DeadPlayer>();
    }

    
    void Update()
    {   
        //If at any time the GUI manager is null, immediately find one in the scene
        if(m_GUIManager == null && m_isLoadingLevel)
        {
            Debug.Log("GUI reference is null, finding new GUIMaster in scene...");
            m_GUIManager = GameObject.FindGameObjectWithTag("GUIManager").GetComponent<GUIBaseMaster>();
        }
    
        if (m_volumeHolder != PlayerPrefs.GetFloat("MusicVolume"))
        {
            m_volumeHolder = PlayerPrefs.GetFloat("MusicVolume");
            this.audio.volume = m_volumeHolder;
        }

        if (m_lossTimerBegin)
        {
            //Wait 5 seconds after sending loss state. If we haven't heard back from any clients by then, resend the loss state
            m_lossTimer += Time.deltaTime;
            if (m_lossTimer > 5.0f)
            {
                ResendLossState();
            }
        }

        if (!m_gameStopped)
        {
            m_gameTimer += Time.deltaTime;
        
            if (Network.isServer)
            {
                if (m_capitalDamageTimer < 5.0f)
                    m_capitalDamageTimer += Time.deltaTime;

                //Purely related to enemy spawning
                if (m_shouldCheckForFinished && GameObject.FindGameObjectsWithTag("Enemy").Length == 0)
                {
                    //No enemies left!
                    AlertArenaIsClear();
                }

                if (m_waveTimer < 45.0f)
                {
                    m_waveTimer += Time.deltaTime;
                    if (m_waveTimer >= 45.0f)
                    {
                        //Spawn next wave
                        PlayerRequestsRoundStart();
                    }
                }
            }

            //Debug.Log ("[GSC]: Checking which dead players need to be updated");

            //foreach(DeadPlayer deadP in m_deadPlayers)
            for (int i = 0; i < m_deadPlayers.Count; i++)
            {
                m_deadPlayers[i].m_deadTimer = Mathf.Max(0f, m_deadPlayers[i].m_deadTimer - Time.deltaTime);
                if (Network.isServer)
                {
                    if (m_deadPlayers[i].m_deadTimer <= 0)
                    {
                        //Try to respawn the player
                        CapitalShipScript cshipSc = m_ingameCapitalShip.GetComponent<CapitalShipScript>();
                        if (cshipSc.HasEnoughCash (500))
                        {
                            //Debug.Log ("[GSC]: Player: " + m_deadPlayers[i].m_playerObject.m_name + " is respawning...");

                            //Spend the respawn cost
                            cshipSc.AlterCash (-500);

                            //Tell the NetworkPlayer to spawn a new ship for themselves

                            //If the server is the dead one, then use the local version
                            if (m_deadPlayers[i].m_playerObject.m_netPlayer == Network.player)
                            {
                                Debug.Log("Host respawned themselves.");
                                SpawnAShip(Network.player);
                                ChangeToInGame();
                            }
                            else
                            {
                                Debug.Log("Telling remote player to respawn themselves.");
                                networkView.RPC("SpawnAShip", m_deadPlayers[i].m_playerObject.m_netPlayer, m_deadPlayers[i].m_playerObject.m_netPlayer);
                                networkView.RPC("ChangeToInGame", m_deadPlayers[i].m_playerObject.m_netPlayer);
                            }

                            //Remove them from the deadList
                            networkView.RPC("PropagateNonDeadPlayer", RPCMode.Others, m_deadPlayers[i].m_playerObject.m_netPlayer);
                            m_deadPlayers.RemoveAt(i--);
                        }
                        else
                        {
                            if (m_deadPlayers[i].m_needsChecking)
                            {
                                //Alert the GUI that there are insufficient funds to respawn, then stop checking if we can afford
                                TellPlayerNoMoneyToRespawn(m_deadPlayers[i].m_playerObject.m_netPlayer);
                                m_deadPlayers[i].m_needsChecking = false;
                            }
                        }
                    }
                }

            }
        }
    }
    
    /* Custom Functions */
    
    void TellPlayerNoMoneyToRespawn(NetworkPlayer player)
    {
        if(player == Network.player)
            networkView.RPC ("SetNotEnoughRespawnMoney", player, true);
        else
            SetNotEnoughRespawnMoney(true);
    }
    void TellPlayerIsMoneyToRespawn(NetworkPlayer player)
    {
        if(player == Network.player)
            networkView.RPC ("SetNotEnoughRespawnMoney", player, false);
        else
            SetNotEnoughRespawnMoney(false);
    }
    [RPC] void SetNotEnoughRespawnMoney(bool state)
    {
        m_GUIManager.GetComponent<GUIInGameMaster>().SetInsufficientRespawnCash(state);
    }
    

    bool ListContainsName(string name)
    {
        for (int i = 0; i < m_connectedPlayers.Count; i++)
        {
            if (m_connectedPlayers[i].m_name == name)
                return true;
        }

        return false;
    }

    public void AlertMoneyAboveRespawn()
    {
        TellPlayerIsMoneyToRespawn(Network.player);
        foreach (DeadPlayer deadP in m_deadPlayers)
        {
            deadP.m_needsChecking = true;
            TellPlayerIsMoneyToRespawn(deadP.m_playerObject.m_netPlayer);
        }
    }

    public void RequestFastSpawnOfPlayer(NetworkPlayer player)
    {
        if (Network.isServer)
        {
            Debug.Log("Player requested fast respawn of player: " + GetNameFromNetworkPlayer(player));

            // We need to check if we are respawning the host
            if (player == Network.player)
            {
                SpawnAShip(player);
                ChangeToInGame();
            }

            else
            {
                networkView.RPC("SpawnAShip", player, player);
                networkView.RPC("ChangeToInGame", player);
            }

            networkView.RPC("PropagateNonDeadPlayer", RPCMode.Others, player);
            for (int i = 0; i < m_deadPlayers.Count; i++)
            {
                if (m_deadPlayers[i].m_playerObject.m_netPlayer == player)
                {
                    m_deadPlayers.RemoveAt(i);
                    break;
                }
            }
        }
    }

    void OnDisconnectedFromServer(NetworkDisconnection info)
    {
        //Debug.Log ("Disconnected from server has fired");
        if (Network.isClient)
        {
            Debug.Log("Lost connection to host...");
            SwitchToInGameConnLost();
            //m_GUIManager.GetComponent<GUIManager>().ShowDisconnectedSplash();
            
        }
    }

    public void AlertGameControllerBeginSpawning()
    {
        m_SpawnManager.GetComponent<EnemySpawnManagerScript>().BeginSpawning();
        m_ingameCapitalShip.GetComponent<CapitalShipScript>().SetShouldMove(true);
    }

    public void StartGameFromMenu(bool isSpecMode)
    {
        //TODO: Change this to begin the level load sequence
        m_sceneLoadedConfirmList = new List<SceneLoadingConfirmation>();
        for (int i = 0; i < m_connectedPlayers.Count; i++)
        {
            m_sceneLoadedConfirmList.Add(new SceneLoadingConfirmation());
            m_sceneLoadedConfirmList[i].player = m_connectedPlayers[i].m_netPlayer;
            m_sceneLoadedConfirmList[i].readyToLoad = false;
        }
        
        //Non-async
        networkView.RPC ("BeginLoadScreen", RPCMode.All);
        //ASync
        //networkView.RPC("BeginLoadingInGameScene", RPCMode.All);
        
        //SwitchToLoadingScreen();
        
        //TODO: Move this stuff to post 
        /*SpawnCapitalShip();

        foreach (NetworkPlayer player in Network.connections)
        {
            Debug.Log("Telling player #" + player.ToString() + " to spawn ship.");
            networkView.RPC("SpawnAShip", player, player);
            networkView.RPC("ChangeToInGame", player);
        }

        if (!isSpecMode)
        {
            SpawnAShip(Network.player);
        }
        ChangeToInGame();

        //Begin the game!
        m_gameStopped = false;
        networkView.RPC("TellLocalGSCGameHasBegun", RPCMode.Others);

        //Once everyone has been told to do stuff, alert camera that it's specmode time 
        if (isSpecMode)
            Camera.main.GetComponent<CameraScript>().TellCameraBeginSpectatorMode();*/
    }
    
    /* Non-ASync Method */
    [RPC] void BeginLoadScreen()
    {
        SwitchToLoadingScreen();
        Application.LoadLevel(1);
        
        StartCoroutine(AwaitLoadComplete());
    }
    
    IEnumerator AwaitLoadComplete()
    {
        while(Application.loadedLevel != 1)
            yield return 0;
    
        if(Network.isServer)
            HostSetUpGame();
        else
        {
            m_GUIManager = GameObject.FindGameObjectWithTag("GUIManager").GetComponent<GUIBaseMaster>();
            SwitchToDockedAtCShip();
        }
    }
    
    /* ASync Method */
    [RPC] void BeginLoadingInGameScene()
    {
        Debug.Log ("Beginning scene load...");
        SwitchToLoadingScreen();
        StartCoroutine(AwaitSceneLoadCompletion());
    }
    
    IEnumerator AwaitSceneLoadCompletion()
    {
        m_levelChangeOperation = Application.LoadLevelAsync(1);
        m_levelChangeOperation.allowSceneActivation = false;
        
        while(m_levelChangeOperation.progress < 0.9f)
        {
            Debug.Log ("Load operation is not yet completed! Current progress: " + m_levelChangeOperation.progress);
            yield return new WaitForSeconds(1.0f);
        }
        
        /*while(!m_levelChangeOperation.isDone)
        {
            Debug.Log ("Load operation is not yet completed!");
            yield return m_levelChangeOperation;
        }*/
        
        Debug.Log ("Load operation has completed, alerting host");
        
        //Alert the host that we're ready to change to the new level whenever everyone else is
        if(Network.isServer)
            HostIsReadyToLoad();
        else
            networkView.RPC ("AlertHostClientIsReadyToLoad", RPCMode.Server);
    }
    
    [RPC] void AlertHostClientIsReadyToLoad(NetworkMessageInfo info)
    {
        Debug.Log ("Client #" + info.sender + " is ready to load");
        for(int i = 0; i < m_sceneLoadedConfirmList.Count; i++)
        {
            if(m_sceneLoadedConfirmList[i].player == info.sender)
            {
                m_sceneLoadedConfirmList[i].readyToLoad = true;
                TestSceneCanBeLoaded();
                return;
            }
        }
    }
    void HostIsReadyToLoad()
    {
        Debug.Log ("Host is ready to load");
        for(int i = 0;i < m_sceneLoadedConfirmList.Count; i++)
        {
            if(m_sceneLoadedConfirmList[i].player == Network.player)
            {
                m_sceneLoadedConfirmList[i].readyToLoad = true;
                TestSceneCanBeLoaded();
                return;
            }
        }
    }
    
    void TestSceneCanBeLoaded()
    {
        for(int i = 0; i < m_sceneLoadedConfirmList.Count; i++)
        {
            if(!m_sceneLoadedConfirmList[i].readyToLoad)
            {
                Debug.Log ("Not everyone is ready yet!");
                return;
            }
        }
        
        Debug.Log ("Everyone is ready!");
        //If we've gotten here, it means everyone is ready to go
        //networkView.RPC ("TellPlayersLoadSceneNow", RPCMode.All);
        TellPlayersSwapSceneNow();
        StartCoroutine(AwaitSceneTransitionBeforeSetup());
    }
    
    IEnumerator AwaitSceneTransitionBeforeSetup()
    {
        while(!m_levelChangeOperation.isDone)
        {
            yield return 0;
        }
        
        HostSetUpGame();
    }
    [RPC] void TellPlayersSwapSceneNow()
    {
        Debug.Log ("Recieved request to finalise scene switch");
        m_levelChangeOperation.allowSceneActivation = true;
        
    }
    void HostSetUpGame()
    {   
        //Uncomment this to return to async
        //networkView.RPC ("TellPlayersSwapSceneNow", RPCMode.Others);
    
        //Update our scene references to the new scene
        m_GUIManager = GameObject.FindGameObjectWithTag("GUIManager").GetComponent<GUIBaseMaster>();
        UpdateAttachedAsteroidManagers();
        
        //Do game setup
        SpawnCapitalShip();
        
        foreach (NetworkPlayer player in Network.connections)
        {
            Debug.Log("Telling player #" + player.ToString() + " to spawn ship.");
            networkView.RPC("SpawnAShip", player, player);
            networkView.RPC("ChangeToInGame", player);
        }
        
        SpawnAShip(Network.player);
        ChangeToInGame();
        
        //Begin the game!
        m_gameStopped = false;
        networkView.RPC("TellLocalGSCGameHasBegun", RPCMode.Others);
        
        networkView.RPC ("ForceRemoteStateChange", RPCMode.All, (int)GameState.InGameCShipDock);
    }
    
    [RPC] void ForceRemoteStateChange(int state)
    {
        ChangeGameState((GameState)state);
    }

    [RPC] void SpawnAShip(NetworkPlayer player)
    {
        if (Network.isClient)
        {
            m_ingameCapitalShip = GameObject.FindGameObjectWithTag("Capital");
        }

        Vector3 pos = m_ingameCapitalShip.transform.position;
        pos.z = 10.75f;
        GameObject ship = (GameObject)Network.Instantiate(m_playerShip, pos, m_ingameCapitalShip.transform.rotation, 0);
        ship.GetComponent<PlayerControlScript>().InitPlayerOnCShip(m_ingameCapitalShip);
        m_localPlayer = ship;
        ship.GetComponent<PlayerControlScript>().SetInputMethod((PlayerPrefs.GetInt("UseControl") == 1));

        //m_GUIManager.GetComponent<GUIManager>().AlertGUIPlayerHasRespawned();
        ship.GetComponent<PlayerControlScript>().TellPlayerWeAreOwner(player);
    }

    void SpawnCapitalShip()
    {
        GameObject spawnPoint = GameObject.FindGameObjectWithTag("CSStart");
        GameObject capital = (GameObject)Network.Instantiate(m_capitalShip, spawnPoint.transform.position, spawnPoint.transform.rotation, 0);
        capital.GetComponent<HealthScript>().SetGameStateController(this.gameObject);
        m_ingameCapitalShip = capital;
        //capital.GetComponent<Ship>()
        networkView.RPC("SendCShipRefToClients", RPCMode.All);
    }

    [RPC] void SendCShipRefToClients()
    {
        GameObject cship = GameObject.FindGameObjectWithTag("Capital");
        m_GUIManager.GetComponent<GUIInGameMaster>().PassThroughCShipReference(cship);
    }
    
    void UpdateAttachedAsteroidManagers()
    {
        m_AsteroidManagers = GameObject.FindGameObjectsWithTag("AsteroidManager");
    }
    public void UpdateShopReferences()
    {
        m_GUIManager.GetComponent<GUIInGameMaster>().PassThroughUpdateShopReferences();
    }

    [RPC] void ChangeToInGame()
    {
        if (Network.isServer && m_shouldSpawnRoids)
        {
            foreach (GameObject am in m_AsteroidManagers)
            {
                am.GetComponent<AsteroidManager>().SpawnAsteroids();
            }
            m_shouldSpawnRoids = false;
        }
        
        SwitchToInGame();
    }

    public void WipeConnectionInfo()
    {
        m_connectedPlayers = new List<Player>();
        m_deadPlayers = new List<DeadPlayer>();
    }

    public void PlayerRequestsToHostGame(string name)
    {
        m_ownName = name;
        Debug.Log("Starting server on port: 6677");
        Network.InitializeServer(10, 6677, false);
        m_connectedPlayers.Add(new Player(Network.player, m_ownName));
        SwitchToHostScreen();
    }

    public void PlayerRequestsToJoinGame(string IP, string name, int port)
    {
        m_ownName = name;
        Debug.Log("Attempting to connect to server at: " + IP.ToString() + ", at port: " + port);
        NetworkConnectionError error = Network.Connect(IP, port);
        SwitchToAttemptingConn();

        if (error != NetworkConnectionError.NoError)
        {
            Debug.LogWarning("Couldn't Connect: " + error.ToString());
        }
    }

    public void PlayerCancelsConnect()
    {
        Debug.Log("Aborting connection.");
        Network.Disconnect();
        SwitchToIPInput();
    }

    public void RemovePlayerFromConnectedList(NetworkPlayer player)
    {
        for (int i = 0; i < m_connectedPlayers.Count; i++)
        {
            if (m_connectedPlayers[i].m_netPlayer == player)
            {
                m_connectedPlayers.RemoveAt(i);
                break;
            }
        }

        networkView.RPC("PropagatePlayerRemoval", RPCMode.Others, player);
    }

    [RPC] void PropagatePlayerRemoval(NetworkPlayer player)
    {
        for (int i = 0; i < m_connectedPlayers.Count; i++)
        {
            if (m_connectedPlayers[i].m_netPlayer == player)
            {
                Debug.Log("Removed player.");
                m_connectedPlayers.RemoveAt(i);
                break;
            }
        }
    }

    void OnPlayerDisconnected(NetworkPlayer player)
    {
        RemovePlayerFromConnectedList(player);
    }

    [RPC] void ClientAsksHostToSpreadName(string name, NetworkMessageInfo info)
    {
        Debug.Log("Server recieved request to propagate name '" + name + "', for client " + info.sender);
        m_connectedPlayers.Add(new Player(info.sender, name));
        networkView.RPC("ServerSpreadsName", RPCMode.Others, name, info.sender);
    }

    [RPC] void ServerSpreadsName(string name, NetworkPlayer player)
    {
        Debug.Log("Recieved client name from server, it reads: Player " + player + " should have name: '" + name + "'");
        m_connectedPlayers.Add(new Player(player, name));
    }

    [RPC] void PlayerSendsNameToOthers(string name, NetworkMessageInfo info)
    {
        //Actually only sends to host right now
        Debug.Log("Received a name storage message from " + info.sender + " with name: '" + name + "'");

        //Check if name already exists
        if (!ListContainsName(name))
        {
            Debug.Log("Name: '" + name + "' is new, allowing connection.");
            //Host sends it's name back to connecting client
            //networkView.RPC ("PlayerPassbackName", info.sender, ownName);

            //Send all the connect player info we have back to the connecting client
            for (int i = 0; i < m_connectedPlayers.Count; i++)
            {
                Debug.Log("Sending back player " + m_connectedPlayers[i].m_netPlayer + ", with name '" + m_connectedPlayers[i].m_name + "'");
                networkView.RPC("ServerPassOtherClientName", info.sender, m_connectedPlayers[i].m_name, m_connectedPlayers[i].m_netPlayer);
                Debug.Log("Sending newly connected player to player: " + m_connectedPlayers[i].m_name);
                networkView.RPC("ServerPassOtherClientName", m_connectedPlayers[i].m_netPlayer, name, info.sender);
            }

            //Finally record the connecting client
            m_connectedPlayers.Add(new Player(info.sender, name));
        }
        else
        {
            Debug.LogError("ERROR: Connecting client has same name as existing client!");

            //Now kick the new player
            networkView.RPC("CancelClientConnect", info.sender);

            //Finally d/c them
            //Network.CloseConnection(info.sender, false);
        }
    }

    [RPC] void CancelClientConnect()
    {
        SwitchToConnFailed();
        WipeConnectionInfo();
        Network.Disconnect();
        Debug.LogWarning("Kicked from server due to conflicting name");
    }

    [RPC] void ServerPassOtherClientName(string name, NetworkPlayer player)
    {
        Debug.Log("Recieved name storage request: Player " + player + " with name: '" + name + "'");
        m_connectedPlayers.Add(new Player(player, name));
    }

    [RPC] void PlayerPassbackName(string name, NetworkMessageInfo info)
    {
        Debug.Log("Recieved a name passback from player " + info.sender + ", asking to record name '" + name + "'");
        m_connectedPlayers.Add(new Player(info.sender, name));
    }
    
    // TODO: this shit needs sorting out as it looks like its fucked
    public void PlayerRequestsRoundStart()
    {
        m_waveTimer = 0;
        m_gameStopped = false;
        networkView.RPC("TellLocalGSCGameHasBegun", RPCMode.Others);
        Debug.Log("Player has requested round start.");
        //networkView.RPC ("TellHostBeginSpawns", RPCMode.Server);
        //networkView.RPC ("TellHostBeginSpawns", RPCMode.All);


        m_shouldCheckForFinished = false;
        //m_GUIManager.GetComponent<GUIManager>().m_ArenaClearOfEnemies = false;
        m_ingameCapitalShip.GetComponent<CapitalShipScript>().SetShouldMove(true);
    }

    [RPC] void TellLocalGSCGameHasBegun()
    {
        m_gameStopped = false;
    }

    [RPC] void TellAllDeadPlayersRespawn()
    {
        if (m_localPlayer == null)
        {
            //LocalPlayerRequestsRespawn();
        }
    }

    #region ExternalScriptAccess
    public void ToggleMainMenu()
    {
        m_inGameMenuIsOpen = !m_inGameMenuIsOpen;
        
        if (m_inGameMenuIsOpen)
        {
            Screen.showCursor = true;
            if (m_localPlayer != null)
                m_localPlayer.GetComponent<PlayerControlScript>().TellShipStopRecievingInput();
                
            SwitchToEscMenu();
        }
        else
        {
            if (!(m_currentGameState == GameState.InGameCShipDock || m_currentGameState == GameState.InGameShopDock))
            {
                Screen.showCursor = false;
                if (m_localPlayer != null)
                    m_localPlayer.GetComponent<PlayerControlScript>().TellShipStartRecievingInput();
            }
            
            SwitchToInGame();
        }
    }
    #endregion

    #region Value Passing To GUI
    public void ToggleBigMapState()
    {
        m_GUIManager.GetComponent<GUIInGameMaster>().ToggleBigMapState();
    }
    public void ToggleSmallMapState()
    {
        m_GUIManager.GetComponent<GUIInGameMaster>().ToggleSmallMapState();
    }
    public void PassNewWeaponReferenceToGUI(GameObject weapon)
    {
        m_GUIManager.GetComponent<GUIInGameMaster>().PassThroughPlayerWeaponReference(weapon);
    }
    #endregion

    #region GameState Changing
    //Menu Screens
    public void SwitchToMainMenu()
    {
        ChangeGameState(GameState.MainMenu);
    }
    
    public void SwitchToHostScreen()
    {
        ChangeGameState(GameState.HostMenu);
    }
    
    public void SwitchToIPInput()
    {
        ChangeGameState(GameState.ClientInputIP);
    }
    
    public void SwitchToClientScreen()
    {
        ChangeGameState(GameState.ClientMenu);
    }

    public void SwitchToOptions()
    {
        ChangeGameState(GameState.OptionMenu);
    }
    
    public void SwitchToAttemptingConn()
    {
        ChangeGameState(GameState.AttemptingConnect);
    }

    public void SwitchToConnFailed()
    {
        ChangeGameState(GameState.FailedConnectName);
    }
    
    public void SwitchToLoadingScreen()
    {
        ChangeGameState(GameState.LoadingScreen);
    }
    
    //In-Game Screens
    public void SwitchToInGame()
    {
        ChangeGameState(GameState.InGame);
    }
    
    public void SwitchToInGameConnLost()
    {
        ChangeGameState(GameState.InGameConnectionLost);
    }
    
    public void SwitchToDockedAtCShip()
    {
        ChangeGameState(GameState.InGameCShipDock);
    }
    
    public void SwitchToDockedAtShop()
    {
        ChangeGameState(GameState.InGameShopDock);
    }
    
    public void SwitchToGameOver()
    {
        ChangeGameState(GameState.InGameGameOver);
    }
    
    public void SwitchToEscMenu()
    {
        ChangeGameState(GameState.InGameMenu);
    }
    #endregion

    void OnPlayerConnected(NetworkPlayer player)
    {

    }

    void OnConnectedToServer()
    {
        //Called on client
        Debug.Log("Successfully connected to server.");
        m_connectedPlayers.Add(new Player(Network.player, m_ownName));
        Debug.Log("Sending our name: '" + m_ownName + "' to other clients.");
        networkView.RPC("PlayerSendsNameToOthers", RPCMode.Server, m_ownName);
        //Debug.Log ("Asking host to propagate our name to other clients");
        //networkView.RPC ("ClientAsksHostToSpreadName", RPCMode.Server, ownName);
        SwitchToClientScreen();
    }

    void ChangeGameState(GameState newState)
    {
        if (m_currentGameState != newState)
        {
            m_currentGameState = newState;
            //m_GUIManager.GetComponent<GUIManager>().UpdateCurrentState(m_currentGameState);
            m_GUIManager.ChangeGameState(newState);
        }
    }

    void AlertArenaIsClear()
    {
        //Uncomment this to allow players to delay starting next wave
        //m_GUIManager.GetComponent<GUIManager>().m_ArenaClearOfEnemies = true;

        //Use this to force start new wave when one has completed
        //Whenever we call respawn order, reset count to 0
        //m_numDeadPCs = 0;
        //networkView.RPC("TellAllDeadPlayersRespawn", RPCMode.All);
        //PlayerRequestsRoundStart();
    }

    
    public void NotifyLocalPlayerHasDied(GameObject player)
    {
        //Remeber to catch all the player's upgrades here
        m_localPlayer = null;
        Camera.main.GetComponent<CameraScript>().NotifyPlayerHasDied();
        //m_GUIManager.GetComponent<GUIManager>().m_PlayerHasDied = true;
        m_GUIManager.GetComponent<GUIInGameMaster>().SetPlayerDead(true);
        if (Network.isClient)
        {
            networkView.RPC("AlertHostPlayerHasDied", RPCMode.Server, false);
            //m_deadPlayers.Add (new DeadPlayer(GetPlayerObjectFromNP(Network.player)));
        }
        else
        {
            AlertHostPlayerHasDied(true, new NetworkMessageInfo());
        }
    }

    [RPC] void AlertHostPlayerHasDied(bool isHost, NetworkMessageInfo info)
    {
        if (Network.isServer)
        {
            NetworkPlayer player = isHost ? Network.player : info.sender;

            m_numDeadPCs++;

            m_deadPlayers.Add(new DeadPlayer(GetPlayerObjectFromNP(player)));
            Debug.Log("Host has been informed that player '" + GetNameFromNetworkPlayer(player) + "' has died.");
            networkView.RPC("PropagateDeadPlayer", RPCMode.Others, player);
        }

        //Let dead players remain dead. If the CShip gets through without them, good job!
        /*if(numDeadPCs >= Network.connections.Length + 1)
        {
            //All players have died :(
            networkView.RPC ("NotifyClientsPlayersHaveDied", RPCMode.All);
        }*/
    }

    [RPC] void PropagateDeadPlayer(NetworkPlayer np)
    {
        //DeadPlayer deadP = new DeadPlayer(GetPlayerObjectFromNP(np));
        //if(!m_deadPlayers.Contains(deadP))
        //m_deadPlayers.Add(deadP);
        string name = GetNameFromNetworkPlayer(np);
        Debug.Log("Recieved notification that player: " + np + ", aka '" + name + "' has died.");
        DeadPlayer deadP = new DeadPlayer(GetPlayerObjectFromNP(np));
        m_deadPlayers.Add(deadP);

        if (GetPlayerFromNetworkPlayer(np) != null)
        {
            GameObject player = GetPlayerFromNetworkPlayer(np);
            Debug.Log ("GSC destroyed " + player.name);
            Destroy(player);
        }
    }

    [RPC] void PropagateNonDeadPlayer(NetworkPlayer np)
    {
        Debug.Log("Received notification that player: " + np + ", aka '" + GetNameFromNetworkPlayer(np) + "' has respawned.");
        string name = GetNameFromNetworkPlayer(np);

        for (int i = 0; i < m_deadPlayers.Count; i++)
        {
            if (m_deadPlayers[i].m_playerObject.m_name == name)
            {
                m_deadPlayers.RemoveAt(i);
                break;
            }
        }
    }

    [RPC] void NotifyClientsPlayersHaveDied()
    {
        //Hijack the CShip destruction func.
        //CapitalShipHasBeenDestroyed();
        TellAllClientsCapitalShipHasBeenDestroyed();
    }

    public void LocalPlayerRequestsRespawn()
    {
        SpawnAShip(Network.player);
        Camera.main.GetComponent<CameraScript>().NotifyPlayerHasRespawned();
        //localPlayer.GetComponent<PlayerControlScript>().Respawn();
        //localPlayer = null;
    }

    public void TellEveryoneCapitalShipArrivesAtVictoryPoint()
    {
        networkView.RPC("CapitalShipArrivesAtVictoryPoint", RPCMode.All);
    }

    [RPC] void CapitalShipArrivesAtVictoryPoint()
    {
        //Old way
        //Stop all enemies
        /*GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in enemies)
        {
            enemy.GetComponent<EnemyScript>().OnPlayerWin();
        }

        //Stop CShip from moving
        m_ingameCapitalShip.GetComponent<CapitalShipScript>().SetShouldMove(false);

        //Tell players to stop recieving input (RPC) <- no longer needs to be rpc'd!

        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject player in players)
            player.GetComponent<PlayerControlScript>().TellShipStopRecievingInput();

        //Tell GUI to display victory splash
        //m_GUIManager.GetComponent<GUIManager>().ShowVictorySplash();
        m_gameStopped = true;*/
        
        //New way
        m_GUIManager.GetComponent<GUIInGameMaster>().PassThroughNewGUIAlert("Capital Ship ready to jump!", 999999.0f);
    }
    
    public void CapitalShipHasTakenDamage()
    {
        //Debug.Log ("Capital ship taking damage");
        if (m_capitalDamageTimer >= 5.0f)
        {
            networkView.RPC("PropagateCapitalShipUnderFire", RPCMode.All);
        }
    }

    [RPC] void PropagateCapitalShipUnderFire()
    {
        m_GUIManager.GetComponent<GUIInGameMaster>().PassThroughNewGUIAlert("Capital Ship is under attack!", 1.5f);
    }

    void ResendLossState()
    {
        for (int i = 0; i < m_lossConfirmList.Count; i++)
        {
            if (!m_lossConfirmList[i].confirmed)
            {
                networkView.RPC("CapitalShipHasBeenDestroyed", m_lossConfirmList[i].player);
                //float timer = m_GUIManager.GetComponent<GUIManager>().GetGameTimer();
                //networkView.RPC("SendTimerToClients", m_lossConfirmList[i].player, timer);
            }
        }
    }

    public void TellAllClientsCapitalShipHasBeenDestroyed()
    {
        if (!m_cshipIsDying)
        {
            m_lossTimer = 0.0f;
            m_lossTimerBegin = true;
            m_lossConfirmList = new List<LossConfirmation>();
            m_lossCameraConfirmList = new List<LossCamConfirmation>();
            for (int i = 0; i < m_connectedPlayers.Count; i++)
            {
                m_lossConfirmList.Add(new LossConfirmation());
                m_lossCameraConfirmList.Add(new LossCamConfirmation());
            }

            for (int i = 0; i < m_connectedPlayers.Count; i++)
            {
                m_lossConfirmList[i].player = m_connectedPlayers[i].m_netPlayer;
                m_lossConfirmList[i].confirmed = false;

                m_lossCameraConfirmList[i].player = m_connectedPlayers[i].m_netPlayer;
                m_lossCameraConfirmList[i].camInPositionConfirmed = false;
            }

            networkView.RPC("CapitalShipHasBeenDestroyed", RPCMode.All);
            //float timer = m_GUIManager.GetComponent<GUIManager>().GetGameTimer();
            //networkView.RPC("SendTimerToClients", RPCMode.Others, timer);
            BeginBuildupDestructionSequence();

            m_cshipIsDying = true;
        }
    }

    [RPC] void SendTimerToClients(float time)
    {
        //m_GUIManager.GetComponent<GUIManager>().SetGameTimer(time);
    }

    [RPC] void CapitalShipHasBeenDestroyed()
    {
        //TODO:
        //Add loss state
        //On loss:


        //Stop all enemies
        /*if(Network.isServer)
        {
            GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
            foreach(GameObject enemy in enemies)
            {
                enemy.GetComponent<EnemyScript>().OnPlayerLoss();
            }
        }*/

        //CShip is gone, don't worry about that

        //Stop player input
        /*GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach(GameObject player in players)
            player.GetComponent<PlayerControlScript>().TellShipStopRecievingInput();*/

        //Tell GUI to show loss splash
        //m_GUIManager.GetComponent<GUIManager>().ShowLossSplash();

        //New CShip death procedure:

        //Tell the gui what's happening.
        //This will handle freezing all enemies, stopping input, and alerting the camera
        //TODO: Make the GSC handle all this
        //m_GUIManager.GetComponent<GUIManager>().AlertGUIDeathSequenceBegins();

        if (Network.isServer)
        {
            HostConfirmsGameOver();
        }
        else
        {
            //Notify server we've recieved the gameOver request
            networkView.RPC("PlayerConfirmsGameOver", RPCMode.Server);
        }

        m_gameStopped = true;
    }

    [RPC] void PlayerConfirmsGameOver(NetworkMessageInfo info)
    {
        for (int i = 0; i < m_lossConfirmList.Count; i++)
        {
            if (m_lossConfirmList[i].player == info.sender)
            {
                m_lossConfirmList[i].confirmed = true;
                return;
            }
        }
    }

    void HostConfirmsGameOver()
    {
        for (int i = 0; i < m_lossConfirmList.Count; i++)
        {
            if (m_lossConfirmList[i].player == Network.player)
            {
                m_lossConfirmList[i].confirmed = true;
                return;
            }
        }
    }

    public void TellServerCameraInCorrectPosition()
    {
        if (Network.isClient)
            networkView.RPC("PlayerConfirmsCameraInCorrectPosition", RPCMode.Server);
        else
            HostConfirmsCameraInCorrectPosition();
    }

    [RPC] void PlayerConfirmsCameraInCorrectPosition(NetworkMessageInfo info)
    {
        for (int i = 0; i < m_lossCameraConfirmList.Count; i++)
        {
            if (m_lossCameraConfirmList[i].player == info.sender)
            {
                m_lossCameraConfirmList[i].camInPositionConfirmed = true;
                break;
            }
        }

        CheckAllPlayersInPosition();
    }

    void HostConfirmsCameraInCorrectPosition()
    {
        for (int i = 0; i < m_lossCameraConfirmList.Count; i++)
        {
            if (m_lossCameraConfirmList[i].player == Network.player)
            {
                Debug.Log("Set own confirm state to true.");
                m_lossCameraConfirmList[i].camInPositionConfirmed = true;
                break;
            }
        }

        CheckAllPlayersInPosition();
    }

    void CheckAllPlayersInPosition()
    {
        Debug.Log("Checking if all players are in position...");
        for (int i = 0; i < m_lossCameraConfirmList.Count; i++)
        {
            if (!m_lossCameraConfirmList[i].camInPositionConfirmed)
            {
                Debug.Log("Player: " + m_lossCameraConfirmList[i].player + " is not in position.");
                return;
            }
        }

        Debug.Log("All players in position for destruction, activating...");
        BeginFinalDestructSequence();
    }

    void BeginBuildupDestructionSequence()
    {
        networkView.RPC("PropagateBuildupDestruct", RPCMode.All);
    }

    [RPC] void PropagateBuildupDestruct()
    {
        m_ingameCapitalShip.GetComponent<CapitalShipScript>().BeginDeathBuildUpAnim();
    }

    void BeginFinalDestructSequence()
    {
        networkView.RPC("PropagateFinalDestruct", RPCMode.All);
    }

    [RPC] void PropagateFinalDestruct()
    {
        m_ingameCapitalShip.GetComponent<CapitalShipScript>().BeginDeathFinalAnim();
    }

    public void NotifyLocalPlayerHasDockedAtCShip()
    {
        //m_GUIManager.GetComponent<GUIManager>().CloseMap();
        //m_GUIManager.GetComponent<GUIManager>().SetPlayerHasDockedAtCapital(true);
        Screen.showCursor = true;
        ChangeGameState(GameState.InGameCShipDock);
        Camera.main.GetComponent<CameraScript>().TellCameraPlayerIsDocked();
    }

    public void NotifyLocalPlayerHasDockedAtShop(GameObject shop)
    {
        //m_GUIManager.GetComponent<GUIManager>().CloseMap();
        //m_GUIManager.GetComponent<GUIManager>().SetPlayerHasDockedAtShop(true);
        Screen.showCursor = true;
        ChangeGameState(GameState.InGameShopDock);
        m_GUIManager.GetComponent<GUIInGameMaster>().SetDockedShop(shop);
    }

    public void RequestSpawnerPause()
    {
        m_SpawnManager.GetComponent<EnemySpawnManagerScript>().PauseSpawners(true);
    }

    public void RequestSpawnerUnPause()
    {
        m_SpawnManager.GetComponent<EnemySpawnManagerScript>().PauseSpawners(false);
    }
}

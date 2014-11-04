using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CameraScript : MonoBehaviour 
{
    /* Serialized members */
	[SerializeField]    GameObject m_backgroundHolder;  // Reference to the background in the scene. Allows for parallax

    /* Internal members */
    GameObject m_currentPlayer;
	bool m_allowDirectCameraControl = false;
	bool m_shouldParallax = false;

    // Specmode stuff
    Vector3 m_SpecOffset = new Vector3(0, -3.5f, -60.0f);
    bool m_isInSpectatorMode = false;
    bool m_isInFollowMode = false;
    int m_trackedPlayerID = -1;
    GameObject[] m_players;
    
    // Camera vars
    Vector2 m_localStretchOffset = Vector2.zero;
    Vector2 m_targetStretchOffset = Vector2.zero;
    float m_currentOrthoSize;
    bool m_playerIsDocked = false;
    bool m_shouldAllowInput = true;
    
    // Death Sequence stuff
    public Vector3 m_targetPoint;
    bool m_hasCompletedDeathSequenceMovement = false;
        	
	/* Cached members */
	GameStateController m_gameController; 	            // A reference to the GameStateController
    GameObject m_capitalShip;                           // A reference to the CShip
    GUIBaseMaster m_gui;						        // A reference to the GUIManager

	/* Unity functions */
    
	void Start () 
	{
		m_currentOrthoSize = camera.orthographicSize;

		// Assign a reference to the GameStateController
        m_gameController = GameStateController.Instance();

        if (m_gameController == null)
		{
			Debug.LogError ("Unable to find GameStateController in CameraScript.");
		}


		/*GameObject guiManager = GameObject.FindGameObjectWithTag ("GUIManager");

		if (guiManager == null || !(m_gui = guiManager.GetComponent<GUIManager>()))
		{
			Debug.LogError ("Unable to find GUIManager in CameraScript.");
		}*/
	}

    // Late update is used for spectator mode
    void LateUpdate()
    {
        Vector3 startPos = this.transform.position;
        
        if(m_shouldAllowInput)
        {
            if(m_isInSpectatorMode)
            {
                if(m_isInFollowMode)
                {
                    if(m_players[m_trackedPlayerID] == null)
                    {
                        //If this player is null, try to find it in the scene again
                        m_players[m_trackedPlayerID] = m_gameController.GetPlayerFromNetworkPlayer (m_gameController.GetNetworkPlayerFromID (m_trackedPlayerID));
                    }
                    
                    //NOTE: Do NOT make else if!
                    if(m_players[m_trackedPlayerID] != null)
                    {
                        Vector3 pos = m_players[m_trackedPlayerID].transform.position;
                        this.transform.position = pos + m_SpecOffset;
                    }
                    
                    //Switch to manual control
                    if(Input.GetKey(KeyCode.W) || Input.GetKey (KeyCode.S) || Input.GetKey (KeyCode.A) || Input.GetKey (KeyCode.D))
                    {
                        m_isInFollowMode = false;
                        m_trackedPlayerID = -1;
                        //m_gui.RecieveActivePlayerSpec(m_trackedPlayerID);   
                    }
                }
                else
                {
                    if(Input.GetKey(KeyCode.W))
                    {
                        if(Input.GetKey(KeyCode.LeftShift))
                            this.transform.position += this.transform.up * 30.0f * Time.deltaTime;
                        else
                            this.transform.position += this.transform.up * 15.0f * Time.deltaTime;
                    }
                    if(Input.GetKey(KeyCode.S))
                    {
                        if(Input.GetKey(KeyCode.LeftShift))
                            this.transform.position -= this.transform.up * 30.0f * Time.deltaTime;
                        else
                            this.transform.position -= this.transform.up * 15.0f * Time.deltaTime;
                    }
                    
                    if(Input.GetKey(KeyCode.A))
                    {
                        if(Input.GetKey(KeyCode.LeftShift))
                            this.transform.position -= this.transform.right * 30.0f * Time.deltaTime;
                        else
                            this.transform.position -= this.transform.right * 15.0f * Time.deltaTime;
                    }
                    if(Input.GetKey(KeyCode.D))
                    {
                        if(Input.GetKey(KeyCode.LeftShift))
                            this.transform.position += this.transform.right * 30.0f * Time.deltaTime;
                        else
                            this.transform.position += this.transform.right * 15.0f * Time.deltaTime;
                    }
                }
                
                if(Input.GetKeyDown(KeyCode.Alpha1))
                {
                    if(m_players[0] != null)
                    {
                        m_isInFollowMode = true;
                        m_trackedPlayerID = 0;
                        //m_gui.RecieveActivePlayerSpec (m_trackedPlayerID);
                    }
                }
                if(Input.GetKeyDown(KeyCode.Alpha2))
                {
                    if(m_players[1] != null)
                    {
                        m_isInFollowMode = true;
                        m_trackedPlayerID = 1;
                        //m_gui.RecieveActivePlayerSpec (m_trackedPlayerID);
                    }
                }
                if(Input.GetKeyDown(KeyCode.Alpha3))
                {
                    if(m_players[2] != null)
                    {
                        m_isInFollowMode = true;
                        m_trackedPlayerID = 2;
                        //m_gui.RecieveActivePlayerSpec (m_trackedPlayerID);
                    }
                }
                if(Input.GetKeyDown(KeyCode.Alpha4))
                {
                    if(m_players[3] != null)
                    {
                        m_isInFollowMode = true;
                        m_trackedPlayerID = 3;
                        //m_gui.RecieveActivePlayerSpec (m_trackedPlayerID);
                    }
                }
                
                if(Input.GetKeyDown(KeyCode.Tab))
                {
                    m_gameController.ToggleBigMapState();
                }
                if(Input.GetKeyDown (KeyCode.Z))
                {
                    // Toggle the map type
                    m_gameController.ToggleSmallMapState();
                }
                
                if(Input.GetKeyDown(KeyCode.LeftControl))
                {
                    m_isInFollowMode = false;
                    if(m_capitalShip == null)
                        m_capitalShip = GameObject.FindGameObjectWithTag("Capital");
                    transform.position = m_capitalShip.transform.position + m_SpecOffset;
                    m_trackedPlayerID = -1;
                    //m_gui.RecieveActivePlayerSpec (m_trackedPlayerID);
                }
                
                if(Input.GetKeyDown (KeyCode.Space))
                {
                    m_isInFollowMode = true;
                    m_trackedPlayerID++;
                    if(m_trackedPlayerID >= m_players.Length)
                        m_trackedPlayerID = 0;
                    else if(m_trackedPlayerID < 0)
                        m_trackedPlayerID = 0;
                    
                    //m_gui.RecieveActivePlayerSpec (m_trackedPlayerID);
                }
                
                if (m_gameController.GetCurrentGameState() == GameState.InGame)
                {
                    
                    float scroll = Input.GetAxis("Mouse ScrollWheel");
                    if(scroll > 0 || Input.GetButtonDown("X360LeftBumper"))
                    {
                        //camera.orthographicSize -= 0.5f * Time.deltaTime;
                        m_currentOrthoSize -= 0.5f;
                        if(m_currentOrthoSize < 1)
                            m_currentOrthoSize = 1;
                        
                        m_SpecOffset.y = -m_currentOrthoSize / 4.285f;
                    }
                    else if(scroll < 0 || Input.GetButtonDown("X360RightBumper"))
                    {
                        //camera.orthographicSize += 0.5f * Time.deltaTime;
                        m_currentOrthoSize += 0.5f;
                        if(m_currentOrthoSize > 15)
                            m_currentOrthoSize = 15;
                        
                        m_SpecOffset.y = -m_currentOrthoSize / 4.285f;
                    }
                }
                camera.orthographicSize = Mathf.Lerp(camera.orthographicSize, m_currentOrthoSize, Time.deltaTime);
            }
        }
        
        Vector3 endPos = transform.position;
        if(m_backgroundHolder != null && m_shouldParallax)
            m_backgroundHolder.GetComponent<ParallaxHolder>().Move(endPos - startPos, transform.position);
    }
    
    //Fixed update is called players, alive or dead, to update the background
    void FixedUpdate () 
    {
        Vector3 startPos = this.transform.position;
        if(m_shouldAllowInput)
        {
            if(!m_isInSpectatorMode)
            {
                //Keep track of the player
                if(m_currentPlayer != null && m_currentPlayer.activeSelf)
                {
                    Vector3 pos = m_currentPlayer.transform.position;
                    pos.z = -10;
                    this.transform.position = pos + new Vector3(m_localStretchOffset.x, m_localStretchOffset.y, 0.0f);
                }
                else
                {
                    //Player is probably dead, allow them to free roam
                    if(m_allowDirectCameraControl)
                    {
                        if(Input.GetKey(KeyCode.W))
                        {
                            this.transform.position += this.transform.up * 10.0f * Time.deltaTime;
                        }
                        if(Input.GetKey(KeyCode.S))
                        {
                            this.transform.position -= this.transform.up * 10.0f * Time.deltaTime;
                        }
                        
                        if(Input.GetKey(KeyCode.A))
                        {
                            this.transform.position -= this.transform.right * 10.0f * Time.deltaTime;
                        }
                        if(Input.GetKey(KeyCode.D))
                        {
                            this.transform.position += this.transform.right * 10.0f * Time.deltaTime;
                        }
                    }
                    
                    if(Input.GetKeyDown(KeyCode.Tab))
                    {
                        m_gameController.ToggleBigMapState();
                    }
                    if(Input.GetKeyDown (KeyCode.Z))
                    {
                        // Toggle the map type
                        m_gameController.ToggleSmallMapState();
                    }
                    
                    if(Input.GetButtonDown("X360Start") || Input.GetKeyDown(KeyCode.Escape))
                    {
                        m_gameController.ToggleMainMenu();
                    }
                }
                
                //Listen for camera input
                if(!m_playerIsDocked && m_gameController.GetCurrentGameState() == GameState.InGame)
                {
                    float scroll = Input.GetAxis("Mouse ScrollWheel");
                    if(scroll > 0 || Input.GetButton("X360LeftBumper"))
                    {
                        //camera.orthographicSize -= 0.5f * Time.deltaTime;
                        m_currentOrthoSize -= 0.5f;
                        if(m_currentOrthoSize < 1)
                            m_currentOrthoSize = 1;
                    }
                    else if(scroll < 0 || Input.GetButton("X360RightBumper"))
                    {
                        //camera.orthographicSize += 0.5f * Time.deltaTime;
                        m_currentOrthoSize += 0.5f;
                        if(m_currentOrthoSize > 15)
                            m_currentOrthoSize = 15;
                    }
                }
                camera.orthographicSize = Mathf.Lerp(camera.orthographicSize, m_currentOrthoSize, Time.deltaTime);
                
                //Do stretchy-cam
                m_localStretchOffset = Vector2.Lerp(m_localStretchOffset, m_targetStretchOffset, Time.deltaTime);
            }
        }
        else
        {
            //Move towards targetPoint
            float distance = Vector3.Distance(m_targetPoint, this.transform.position);
            if(distance > 2.0f)
            {
                Vector3 dir = m_targetPoint - this.transform.position;
                dir.Normalize();
                this.transform.position += dir * 26.5f * Time.deltaTime;
            }
            else
            {
                if(!m_hasCompletedDeathSequenceMovement)
                {
                    m_gameController.TellServerCameraInCorrectPosition();
                    m_hasCompletedDeathSequenceMovement = true;
                }
            }
            
            if(camera.orthographicSize < 15.0f)
            {
                camera.orthographicSize += 2.0f * Time.deltaTime;
                if(camera.orthographicSize > 15.0f)
                    camera.orthographicSize = 15.0f;
            }
        }
        
        Vector3 endPos = transform.position;
        if(m_backgroundHolder != null && m_shouldParallax)
            m_backgroundHolder.GetComponent<ParallaxHolder>().Move(endPos - startPos, transform.position);
    }

    /* Custom functions */
    public void SetTargetStretchOffset(Vector2 offset)
    {
        m_targetStretchOffset = offset * camera.orthographicSize;
    }
    public void InitPlayer(GameObject player)
    {
        m_currentPlayer = player;
        if(m_backgroundHolder != null)
        {
            m_backgroundHolder.transform.position = this.transform.position + new Vector3(0, 0, 21);
            m_backgroundHolder.GetComponent<ParallaxHolder>().ResetLayers();
        }
        m_shouldParallax = true;
    }

	public void TellCameraPlayerIsDocked()
	{
		m_playerIsDocked = true;
		m_currentOrthoSize = 12;
	}
	public void TellCameraPlayerIsUnDocked()
	{
		m_playerIsDocked = false;
		m_currentOrthoSize = 5.5f;
	}

	public void TellCameraBeginSpectatorMode()
	{
		//Stop typical cam behaviour
		m_isInSpectatorMode = true;

		//Init player array
		m_gameController.RemovePlayerFromConnectedList(Network.player);
		List<Player> players = m_gameController.GetConnectedPlayers();
		m_players = new GameObject[players.Count];
		for(int i = 0; i < players.Count; i++)
		{
			m_players[i] = m_gameController.GetPlayerFromNetworkPlayer (m_gameController.GetNetworkPlayerFromID(i));
		}

		//Alert gui we're in specmode
		//m_gui.BeginSpecModeGameTimer();
		m_shouldParallax = true;

		//Jump to CShip
		if(m_capitalShip == null)
            m_capitalShip = GameObject.FindGameObjectWithTag("Capital");
        
		this.transform.position = m_capitalShip.transform.position + new Vector3(0, 0, -24.0f);
	}

	public void NotifyPlayerHasDied()
	{
		m_allowDirectCameraControl = true;
	}
	public void NotifyPlayerHasRespawned()
	{
		m_allowDirectCameraControl = false;
	}

	public void TellCameraBeginDeathSequence()
	{
		m_shouldAllowInput = false;
        if(m_capitalShip == null)
            m_capitalShip = GameObject.FindGameObjectWithTag("Capital");
            
		Vector3 capitalPos = m_capitalShip.transform.position;
		Vector3 cameraTargetPos = new Vector3(capitalPos.x, capitalPos.y, -54);
		m_targetPoint = cameraTargetPos;
	}
}

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CameraScript : MonoBehaviour 
{
	GameObject m_currentPlayer;
	[SerializeField]
	GameObject m_backgroundHolder;

	bool m_allowDirectCameraControl = false;
	bool m_shouldParallax = false;
	
	// Cache
	GameStateController m_gameController; 	// A reference to the GameStateController
	GUIManager m_gui;						// A reference to the GUIManager

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

	// Use this for initialization
	void Start () 
	{
		m_currentOrthoSize = camera.orthographicSize;

		// Assign a reference to the GameStateController
		GameObject gameController = GameObject.FindGameObjectWithTag ("GameController");

		if (!gameController || !(m_gameController = gameController.GetComponent<GameStateController>()))
		{
			Debug.LogError ("Unable to find GameStateController in CameraScript.");
		}

		GameObject guiManager = GameObject.FindGameObjectWithTag ("GUIManager");

		if (!guiManager || !(m_gui = guiManager.GetComponent<GUIManager>()))
		{
			Debug.LogError ("Unable to find GUIManager in CameraScript.");
		}

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

	//Specmode stuff

	bool m_isInSpectatorMode = false;
	bool m_isInFollowMode = false;
	int m_trackedPlayerID = -1;
	[SerializeField]
	GameObject[] m_players;

	public void TellCameraBeginSpectatorMode()
	{
		//Stop typical cam behaviour
		m_isInSpectatorMode = true;

		//Init player array
		//instead of trying to find them in the scene, get them from GSC, then they are ordered
		m_gameController.RemovePlayerFromConnectedList(Network.player);
		List<Player> players = m_gameController.m_connectedPlayers;
		m_players = new GameObject[players.Count];
		for(int i = 0; i < players.Count; i++)
		{
			m_players[i] = m_gameController.GetPlayerFromNetworkPlayer (m_gameController.GetNetworkPlayerFromID(i));
		}

		//Alert gui we're in specmode
		m_gui.BeginSpecModeGameTimer();

		m_shouldParallax = true;

		//Jump to CShip
		//TODO: Parametise this into finding either cship or cshipstartpos
		this.transform.position = new Vector3(-70, 50, -10);
	}

	//Camera vars
	float m_currentOrthoSize;
	bool m_playerIsDocked = false;
	bool m_shouldAllowInput = true;

	// Update is called once per frame
	void FixedUpdate () 
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
						pos.z = -10;
						this.transform.position = pos;
					}

					//Switch to manual control
					if(Input.GetKey(KeyCode.W) || Input.GetKey (KeyCode.S) || Input.GetKey (KeyCode.A) || Input.GetKey (KeyCode.D))
					{
						m_isInFollowMode = false;
						m_trackedPlayerID = -1;
						m_gui.RecieveActivePlayerSpec(m_trackedPlayerID);	
					}
				}
				else
				{
					if(Input.GetKey(KeyCode.W))
					{
						this.transform.position += this.transform.up * 15.0f * Time.deltaTime;
					}
					if(Input.GetKey(KeyCode.S))
					{
						this.transform.position -= this.transform.up * 15.0f * Time.deltaTime;
					}
					
					if(Input.GetKey(KeyCode.A))
					{
						this.transform.position -= this.transform.right * 15.0f * Time.deltaTime;
					}
					if(Input.GetKey(KeyCode.D))
					{
						this.transform.position += this.transform.right * 15.0f * Time.deltaTime;
					}
				}

				if(Input.GetKeyDown(KeyCode.Tab))
				{
					m_gui.ToggleMap();
				}
				if(Input.GetKeyDown (KeyCode.Z))
				{
					// Toggle the map type
					m_gui.m_isOnFollowMap  = !m_gui.m_isOnFollowMap;
				}

				if(Input.GetKeyDown (KeyCode.Space))
				{
					m_isInFollowMode = true;
					m_trackedPlayerID++;
					if(m_trackedPlayerID >= m_players.Length)
						m_trackedPlayerID = 0;
					else if(m_trackedPlayerID < 0)
						m_trackedPlayerID = 0;

					m_gui.RecieveActivePlayerSpec (m_trackedPlayerID);
				}

				if (m_gameController.m_currentGameState == GameState.InGame)
				{
					
					float scroll = Input.GetAxis("Mouse ScrollWheel");
					if(scroll > 0 || Input.GetButtonDown("X360LeftBumper"))
					{
						//camera.orthographicSize -= 0.5f * Time.deltaTime;
						m_currentOrthoSize -= 0.5f;
						if(m_currentOrthoSize < 1)
							m_currentOrthoSize = 1;
					}
					else if(scroll < 0 || Input.GetButtonDown("X360RightBumper"))
					{
						//camera.orthographicSize += 0.5f * Time.deltaTime;
						m_currentOrthoSize += 0.5f;
						if(m_currentOrthoSize > 15)
							m_currentOrthoSize = 15;
					}
				}

				camera.orthographicSize = Mathf.Lerp(camera.orthographicSize, m_currentOrthoSize, Time.deltaTime);
			}
			else
			{
				//Keep track of the player
				if(m_currentPlayer != null && m_currentPlayer.activeSelf)
				{
					Vector3 pos = m_currentPlayer.transform.position;
					pos.z = -10;
					this.transform.position = pos;
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
						m_gui.ToggleMap();
					}
					if(Input.GetKeyDown (KeyCode.Z))
					{
						// Toggle the map type
						m_gui.m_isOnFollowMap  = !m_gui.m_isOnFollowMap;
					}

					if(Input.GetButtonDown("X360Start") || Input.GetKeyDown(KeyCode.Escape))
					{
						GameObject.FindGameObjectWithTag("GUIManager").GetComponent<GUIManager>().ToggleMenuState();
					}
				}

				//Listen for camera input
				if(!m_playerIsDocked && m_gameController.m_currentGameState == GameState.InGame)
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
			}

		}

		Vector3 endPos = transform.position;
		if(m_backgroundHolder != null && m_shouldParallax)
			m_backgroundHolder.GetComponent<ParallaxHolder>().Move(endPos - startPos, transform.position);
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
		Vector3 capitalPos = GameObject.FindGameObjectWithTag("Capital").transform.position;
		Vector3 cameraTargetPos = new Vector3(capitalPos.x, capitalPos.y, -54);
		StartCoroutine(LerpCamera(cameraTargetPos));
	}
	IEnumerator LerpCamera(Vector3 target)
	{
		Vector3 oldPos = this.transform.position;
		float oldSize = this.camera.orthographicSize;
		float t = 0.0f;

		while(t < 1.0f)
		{
			t += Time.deltaTime;
			this.transform.position = Vector3.Lerp(oldPos, target, t);
			this.camera.orthographicSize = Mathf.Lerp(oldSize, 15.0f, t);
			yield return 0;
		}

		//When we're done, let the GSC know
		Debug.Log ("Camera has reached final position, alerting GSC.");
		GameObject.FindGameObjectWithTag("GameController").GetComponent<GameStateController>().TellServerCameraInCorrectPosition();
	}
}

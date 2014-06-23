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
	GUIManager guiRef;

	public void TellCameraBeginSpectatorMode()
	{
		//Stop typical cam behaviour
		m_isInSpectatorMode = true;

		//Init player array
		//instead of trying to find them in the scene, get them from GSC, then they are ordered
		GameStateController gsc = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameStateController>();
		gsc.RemovePlayerFromConnectedList(Network.player);
		List<Player> players = gsc.m_connectedPlayers;
		m_players = new GameObject[players.Count];
		for(int i = 0; i < players.Count; i++)
		{
			m_players[i] = gsc.GetPlayerFromNetworkPlayer(gsc.GetNetworkPlayerFromID(i));
		}

		//Alert gui we're in specmode
		guiRef = GameObject.FindGameObjectWithTag("GUIManager").GetComponent<GUIManager>();
		guiRef.BeginSpecModeGameTimer();

		m_shouldParallax = true;

		//Jump to CShip
		//TODO: Parametise this into finding either cship or cshipstartpos
		this.transform.position = new Vector3(-70, 50, -10);
	}

	//Camera vars
	float m_currentOrthoSize;
	bool m_playerIsDocked = false;

	// Update is called once per frame
	void FixedUpdate () 
	{
		Vector3 startPos = this.transform.position;
		if(m_isInSpectatorMode)
		{
			if(m_isInFollowMode)
			{
				if(m_players[m_trackedPlayerID] == null)
				{
					//If this player is null, try to find it in the scene again
					GameStateController gsc = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameStateController>();
					m_players[m_trackedPlayerID] = gsc.GetPlayerFromNetworkPlayer(gsc.GetNetworkPlayerFromID(m_trackedPlayerID));
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
					guiRef.RecieveActivePlayerSpec(m_trackedPlayerID);	
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
				GameObject.FindGameObjectWithTag("GUIManager").GetComponent<GUIManager>().ToggleMap();
			}

			if(Input.GetKeyDown (KeyCode.Space))
			{
				m_isInFollowMode = true;
				m_trackedPlayerID++;
				if(m_trackedPlayerID >= m_players.Length)
					m_trackedPlayerID = 0;
				else if(m_trackedPlayerID < 0)
					m_trackedPlayerID = 0;

				guiRef.RecieveActivePlayerSpec(m_trackedPlayerID);
			}

			float scroll = Input.GetAxis("Mouse ScrollWheel");
			if(scroll > 0)
			{
				//camera.orthographicSize -= 0.5f * Time.deltaTime;
				m_currentOrthoSize -= 0.5f;
				if(m_currentOrthoSize < 1)
					m_currentOrthoSize = 1;
			}
			else if(scroll < 0)
			{
				//camera.orthographicSize += 0.5f * Time.deltaTime;
				m_currentOrthoSize += 0.5f;
				if(m_currentOrthoSize > 15)
					m_currentOrthoSize = 15;
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
				
				//Listen for camera input
				if(!m_playerIsDocked)
				{
					float scroll = Input.GetAxis("Mouse ScrollWheel");
					if(scroll > 0)
					{
						//camera.orthographicSize -= 0.5f * Time.deltaTime;
						m_currentOrthoSize -= 0.5f;
						if(m_currentOrthoSize < 1)
							m_currentOrthoSize = 1;
					}
					else if(scroll < 0)
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
					GameObject.FindGameObjectWithTag("GUIManager").GetComponent<GUIManager>().ToggleMap();
				}
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
}

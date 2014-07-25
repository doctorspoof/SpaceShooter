using UnityEngine;
using System.Collections;

public class EngineScript : MonoBehaviour 
{
	public Vector3 GetOffset()
	{
		return m_requiredOffset;
	}
	[SerializeField]
	Vector3 m_requiredOffset;
	
	[SerializeField]
	float m_engineMoveSpeed;
	public float GetMoveSpeed()
	{
		return m_engineMoveSpeed;
	}

	[SerializeField]
	float m_engineTurnSpeed;
	public float GetTurnSpeed()
	{
		return m_engineTurnSpeed;
	}

	[SerializeField]
	float m_engineStrafeModifier;
	public float GetStrafeModifier()
	{
		return m_engineStrafeModifier;
	}

	// Use this for initialization
	void Start () 
	{
	
	}
	
	// Update is called once per frame
	void Update () 
	{
	
	}

	public void ParentToPlayer(string name)
	{
		networkView.RPC("ParentToPlayerOverNetwork", RPCMode.Others, name);
	}
	[RPC]
	void ParentToPlayerOverNetwork(string name)
	{
		GameStateController gsc = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameStateController>();
		
		GameObject playerGO = gsc.GetPlayerFromNetworkPlayer(gsc.GetNetworkPlayerFromID(gsc.GetIDFromName(name)));
		Debug.Log ("Attaching engine: " + this.name + " to gameObject: " + playerGO.name + ", through name: " + name);
		this.transform.parent = playerGO.transform;
		this.transform.localPosition = m_requiredOffset;
		playerGO.GetComponent<PlayerControlScript>().EquipEngineStats(m_engineMoveSpeed, m_engineTurnSpeed, m_engineStrafeModifier);
	}
}

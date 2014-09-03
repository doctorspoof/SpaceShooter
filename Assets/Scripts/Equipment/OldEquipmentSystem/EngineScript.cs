using UnityEngine;
using System.Collections;

public class EngineScript : MonoBehaviour 
{
	
	[SerializeField] Vector3 m_requiredOffset;
	
	[SerializeField] float m_engineMoveSpeed;

	[SerializeField] float m_engineTurnSpeed;

	[SerializeField] float m_engineStrafeModifier;
	

    #region getset

    public Vector3 GetOffset()
    {
        return m_requiredOffset;
    }

    public float GetMoveSpeed()
    {
        return m_engineMoveSpeed;
    }

    public float GetTurnSpeed()
    {
        return m_engineTurnSpeed;
    }

    public float GetStrafeModifier()
    {
        return m_engineStrafeModifier;
    }

    #endregion getset

	public void ParentToPlayer(string name)
	{
		networkView.RPC("ParentToPlayerOverNetwork", RPCMode.Others, name);
	}

	[RPC] void ParentToPlayerOverNetwork(string name)
	{
		GameStateController gsc = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameStateController>();
		
		GameObject playerGO = gsc.GetPlayerFromNetworkPlayer(gsc.GetNetworkPlayerFromID(gsc.GetIDFromName(name)));
		Debug.Log ("Attaching engine: " + this.name + " to gameObject: " + playerGO.name + ", through name: " + name);
		this.transform.parent = playerGO.transform;
		this.transform.localPosition = m_requiredOffset;
		playerGO.GetComponent<PlayerControlScript>().EquipEngineStats(m_engineMoveSpeed, m_engineTurnSpeed, m_engineStrafeModifier);
	}
}

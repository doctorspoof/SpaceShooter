using UnityEngine;
using System.Collections;

public class ShieldScript : MonoBehaviour 
{
	public Vector3 GetOffset()
	{
		return m_requiredOffset;
	}
	[SerializeField]
	Vector3 m_requiredOffset;


	[SerializeField]
	int m_shieldMaxCharge;
	public int GetShieldMaxCharge()
	{
		return m_shieldMaxCharge;
	}

	[SerializeField]
	int m_shieldRechargeRate;
	public int GetShieldRechargeRate()
	{
		return m_shieldRechargeRate;
	}

	[SerializeField]
	float m_shieldRechargeDelay;
	public float GetShieldRechargeDelay()
	{
		return m_shieldRechargeDelay;
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
		Debug.Log ("Attaching shield: " + this.name + " to gameObject: " + playerGO.name + ", through name: " + name);
		this.transform.parent = playerGO.transform;
		this.transform.localPosition = m_requiredOffset;

		//Now apply it's affects to the player
		playerGO.GetComponent<HealthScript>().EquipNewShield(m_shieldMaxCharge, m_shieldRechargeRate, m_shieldRechargeDelay);
		//playerGO.GetComponent<PlayerControlScript>();
	}
}

using UnityEngine;
using System.Collections;

public class ShieldScript : MonoBehaviour 
{
	
	[SerializeField] Vector3 m_requiredOffset;

	[SerializeField] int m_shieldMaxCharge;
	
	[SerializeField] int m_shieldRechargeRate;

	[SerializeField] float m_shieldRechargeDelay;

    #region getset

    public Vector3 GetOffset()
    {
        return m_requiredOffset;
    }

    public int GetShieldMaxCharge()
    {
        return m_shieldMaxCharge;
    }

    public int GetShieldRechargeRate()
    {
        return m_shieldRechargeRate;
    }

    public float GetShieldRechargeDelay()
	{
		return m_shieldRechargeDelay;
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
		Debug.Log ("Attaching shield: " + this.name + " to gameObject: " + playerGO.name + ", through name: " + name);
		this.transform.parent = playerGO.transform;
		this.transform.localPosition = m_requiredOffset;

		//Now apply it's affects to the player
		//playerGO.GetComponent<HealthScript>().EquipNewShield(m_shieldMaxCharge, m_shieldRechargeRate, m_shieldRechargeDelay);
		//playerGO.GetComponent<PlayerControlScript>();
	}
}

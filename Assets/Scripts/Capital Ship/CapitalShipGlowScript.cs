using UnityEngine;
using System.Collections;

public class CapitalShipGlowScript : MonoBehaviour 
{
	[SerializeField]
	int m_turretGlowID = 1;

	// Use this for initialization
	void Start () 
	{
	
	}
	
	// Update is called once per frame
	void Update () 
	{
	
	}

	public int GetGlowID()
	{
		return m_turretGlowID;
	}

	public void SetGlowIsActive(bool activeState)
	{
		networkView.RPC("PropagateActiveGlow", RPCMode.All, activeState);
	}
	[RPC]
	void PropagateActiveGlow(bool active)
	{
		this.renderer.enabled = active;
	}
}

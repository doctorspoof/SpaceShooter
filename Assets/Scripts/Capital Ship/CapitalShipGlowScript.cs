using UnityEngine;
using System.Collections;

public class CapitalShipGlowScript : MonoBehaviour 
{
    /* Serializable members */
	[SerializeField] int m_turretGlowID = 1;

    /* Getters/Setters */
    public int GetGlowID()
    {
        return m_turretGlowID;
    }
    public void SetGlowIsActive(bool activeState)
    {
        networkView.RPC("PropagateActiveGlow", RPCMode.All, activeState);
    }

	/* Unity functions *
    
	//None
    
    /*Custom functions */
	[RPC]
	void PropagateActiveGlow(bool active)
	{
		this.renderer.enabled = active;
	}
}

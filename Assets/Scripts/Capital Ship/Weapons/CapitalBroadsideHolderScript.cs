using UnityEngine;
using System.Collections;

public class CapitalBroadsideHolderScript : MonoBehaviour 
{
    /* Serialized members */
	[SerializeField]    GameObject[] m_turretLocationVersions;

    /* Internal members */
	int m_turretLocationAttachedTo = -1;
	GameObject m_attachedTurret = null;
    
    /* Cached members */
    GameObject m_cShip;

    /* Unity functions */
    void OnDestroy()
    {
        //Destroy the attached turret
        Network.Destroy (m_attachedTurret);
        
        if(m_cShip == null)
            m_cShip = GameObject.FindGameObjectWithTag("Capital");
        
        //Renable the glow for this section
        m_cShip.GetComponent<CapitalShipScript>().GetGlowForTurretByID(m_turretLocationAttachedTo).GetComponent<CapitalShipGlowScript>().SetGlowIsActive(true);
        
        //Renable the renderer for this THolder
        m_cShip.GetComponent<CapitalShipScript>().GetCTurretHolderWithID(m_turretLocationAttachedTo).GetComponent<CShipTurretHolder>().EnableRenderer();
    }

    /* Custom functions */
	public void SpawnLocationTurret(int id)
	{
		Debug.Log ("Spawning broadside turret with id #" + id);
        m_turretLocationAttachedTo = id;
        if(m_cShip == null)
            m_cShip = GameObject.FindGameObjectWithTag("Capital");
        GameObject turret = (GameObject)Network.Instantiate(m_turretLocationVersions[id - 1], this.transform.position, m_cShip.transform.rotation, 0);
		turret.transform.parent = this.transform;
		ParentThisWeaponToCShip(id);
        m_attachedTurret = turret;

		this.transform.localPosition = new Vector3(0, 0, 0.15f);
	}

	public void ParentThisWeaponToCShip(int location)
	{
		networkView.RPC ("PropagateParentToLocation", RPCMode.All, location);
	}
    
	[RPC] void PropagateParentToLocation(int location)
	{
        if(m_cShip == null)
            m_cShip = GameObject.FindGameObjectWithTag("Capital");
        this.transform.parent = m_cShip.GetComponent<CapitalShipScript>().GetCTurretHolderWithID(location).transform;
		this.transform.localPosition = Vector3.zero;
	}
}

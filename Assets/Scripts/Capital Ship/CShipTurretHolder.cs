using UnityEngine;
using System.Collections;

public class CShipTurretHolder : MonoBehaviour 
{
    [SerializeField]            int m_cShipTurretID;         // An assigned ID to indicate where on the CShip this turret is located
	[SerializeField]            bool m_forwardFacing;        // Whether or not the turret should face forwards by default

    /* Getters/Setters */
    public GameObject GetAttachedTurret()
    {
        return this.transform.GetChild(0).gameObject;
    }

	/* Unity Functions */
    void OnSerializeNetworkView(BitStream stream, NetworkMessageInfo info)
    {
        float zRot = this.transform.rotation.eulerAngles.z;
        
        if(stream.isWriting)
        {
            stream.Serialize(ref zRot);
        }
        else
        {
            stream.Serialize(ref zRot);
            
            this.transform.rotation = Quaternion.Euler(0, 0, zRot);
        }
    }

    /* Custom Functions */
	public void ReplaceAttachedTurret(GameObject turretRef)
	{
		if(this.transform.childCount > 0)
			Network.Destroy(this.transform.GetChild(0).gameObject);
		GameObject newTurr = (GameObject)Network.Instantiate(turretRef, this.transform.position, this.transform.rotation, 0);
		if(newTurr.GetComponent<CapitalWeaponScript>() != null)
			newTurr.GetComponent<CapitalWeaponScript>().ParentThisWeaponToCShip(m_cShipTurretID);
		else
		{
			//If we're spawning a broadside, we need to do a few extra things
			//First ensure rotation reset
			this.transform.localRotation = Quaternion.identity;

			//Then spawn the broadside
			newTurr.GetComponent<CapitalBroadsideHolderScript>().SpawnLocationTurret(m_cShipTurretID);

			//Turn off the holder's renderer
			DisableRenderer();

			//Turn off the appropriate glow too
			this.transform.parent.GetComponent<CapitalShipScript>().GetGlowForTurretByID(m_cShipTurretID).GetComponent<CapitalShipGlowScript>().SetGlowIsActive(false);
		}
        
		if(!m_forwardFacing)
		{
			if(newTurr.GetComponent<CapitalWeaponScript>() != null)
				newTurr.GetComponent<CapitalWeaponScript>().m_isForwardFacing = false;
		}
	}

	public void EnableRenderer()
	{
		this.renderer.enabled = true;
		networkView.RPC ("PropagateEnableRenderer", RPCMode.All);
	}
    [RPC] void PropagateEnableRenderer()
	{
		this.renderer.enabled = true;
	}
    
	public void DisableRenderer()
	{
		networkView.RPC ("PropagateDisableRenderer", RPCMode.All);
	}
    [RPC] void PropagateDisableRenderer()
	{
		this.renderer.enabled = false;
	}

	

}

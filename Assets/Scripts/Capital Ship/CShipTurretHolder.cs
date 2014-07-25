using UnityEngine;
using System.Collections;

public class CShipTurretHolder : MonoBehaviour 
{
	public int m_cShipTurretID;
	public bool m_forwardFacing;

	// Use this for initialization
	void Start () 
	{
	
	}
	
	// Update is called once per frame
	void Update () 
	{
	
	}

	public GameObject GetAttachedTurret()
	{
		return this.transform.GetChild(0).gameObject;
	}
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
			if(!m_forwardFacing)
			{
				//If we're not forward facing, rotate to forwards first
			}

			//Then spawn the broadside
			newTurr.GetComponent<CapitalBroadsideHolderScript>().SpawnLocationTurret(m_cShipTurretID);

			//Turn off the holder's renderer
			DisableRenderer();

			//Turn off the appropriate glow too
			this.transform.parent.GetComponent<CapitalShipScript>().GetGlowForTurretByID(m_cShipTurretID).GetComponent<CapitalShipGlowScript>().SetGlowIsActive(false);
		}
		//newTurr.transform.parent = this.transform;

		if(!m_forwardFacing)
		{
			if(newTurr.GetComponent<CapitalWeaponScript>() != null)
				newTurr.GetComponent<CapitalWeaponScript>().isForwardFacing = false;
		}
	}

	public void EnableRenderer()
	{
		this.renderer.enabled = true;
		networkView.RPC ("PropagateEnableRenderer", RPCMode.All);
	}
	[RPC]
	void PropagateEnableRenderer()
	{
		this.renderer.enabled = true;
	}
	public void DisableRenderer()
	{
		networkView.RPC ("PropagateDisableRenderer", RPCMode.All);
	}
	[RPC]
	void PropagateDisableRenderer()
	{
		this.renderer.enabled = false;
	}

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

}

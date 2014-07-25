using UnityEngine;
using System.Collections;

public class CapitalBroadsideHolderScript : MonoBehaviour 
{
	[SerializeField]
	GameObject[] m_turretLocationVersions;

	int turretLocationAttachedTo = -1;

	GameObject attachedTurret = null;

	// Use this for initialization
	void Start () 
	{
	
	}
	
	// Update is called once per frame
	void Update () 
	{
	
	}

	public void SpawnLocationTurret(int id)
	{
		Debug.Log ("Spawning broadside turret with id #" + id);
		turretLocationAttachedTo = id;
		GameObject turret = (GameObject)Network.Instantiate(m_turretLocationVersions[id - 1], this.transform.position, GameObject.FindGameObjectWithTag("Capital").transform.rotation, 0);
		turret.transform.parent = this.transform;
		ParentThisWeaponToCShip(id);
		attachedTurret = turret;

		this.transform.localPosition = new Vector3(0, 0, 0.15f);
	}

	void OnDestroy()
	{
		//Destroy the attached turret
		Network.Destroy (attachedTurret);

		GameObject CShip = GameObject.FindGameObjectWithTag("Capital");
		//Renable the glow for this section
		CShip.GetComponent<CapitalShipScript>().GetGlowForTurretByID(turretLocationAttachedTo).GetComponent<CapitalShipGlowScript>().SetGlowIsActive(true);

		//Renable the renderer for this THolder
		CShip.GetComponent<CapitalShipScript>().GetCTurretHolderWithId(turretLocationAttachedTo).GetComponent<CShipTurretHolder>().EnableRenderer();
	}

	public void ParentThisWeaponToCShip(int location)
	{
		networkView.RPC ("PropagateParentToLocation", RPCMode.All, location);
	}
	[RPC]
	void PropagateParentToLocation(int location)
	{
		GameObject cship = GameObject.FindGameObjectWithTag("Capital");
		this.transform.parent = cship.GetComponent<CapitalShipScript>().GetCTurretHolderWithId(location).transform;
		//this.transform.parent = cship.transform;
		//attachedTurret.GetComponent<CapitalWeaponScript>().PropagateParentToLocation(location);
		this.transform.localPosition = Vector3.zero;
		//this.transform.localPosition = new Vector3(0.0f, 0.6f, 0.1f);
		//this.transform.localPosition = m_posOffset;
	}
}

using UnityEngine;
using System.Collections;

//TODO: Change this to support changing weapons at any time (ie, inventory system)
public class PlayerWeaponScript : MonoBehaviour 
{
	/*[SerializeField]
	float m_recoilTime;
	float m_currentRecoil;
	[SerializeField]
	GameObject m_bulletRef;*/

	[SerializeField]
	GameObject m_currentWeapon;

	public void EquipWeapon(GameObject weapon)
	{
		m_currentWeapon = weapon;
	}

	// Use this for initialization
	void Start () 
	{
		/*GameObject weapon = (GameObject)Instantiate(m_currentWeapon, this.transform.position, this.transform.rotation);
		weapon.transform.parent = this.transform;
		m_currentWeapon = weapon;*/
	}
	
	// Update is called once per frame
	void Update () 
	{
		if(m_currentWeapon == null)
		{
			if(Network.isServer)
			{
				//Re-request a weapon object from player (only serverside)
				Debug.Log ("Weapon object is null. Resetting weapon.");
				this.GetComponent<PlayerControlScript>().ResetEquippedWeapon();
			}
			else
			{
				//If we're serverside, check if the server has spawned us a weapon yet
				GameObject weapon = FindAttachedWeapon();
				if(weapon != null)
				{
					m_currentWeapon = weapon;
				}
			}
		}
	}

	public void PlayerRequestsFire()
	{
		if(Network.isServer)
		{
			//Tell current weapon to fire
			m_currentWeapon.GetComponent<WeaponScript>().PlayerRequestsFire();
		}
		else
		{
			if(m_currentWeapon.GetComponent<WeaponScript>().CheckCanFire())
			{
				networkView.RPC ("RequestFireOverNetwork", RPCMode.Server);
				m_currentWeapon.GetComponent<WeaponScript>().ActAsFired();
			}
		}
	}
	public void PlayerReleaseFire()
	{
		if(Network.isServer)
		{
			if(m_currentWeapon.GetComponent<WeaponScript>().m_isBeam)
				m_currentWeapon.GetComponent<WeaponScript>().AlertBeamWeaponNotFiring();
		}
		else
		{
			if(m_currentWeapon.GetComponent<WeaponScript>().m_isBeam)
			{
				m_currentWeapon.GetComponent<WeaponScript>().AlertBeamWeaponNotFiring();
				networkView.RPC ("StopFireOverNetwork", RPCMode.Server);
			}
		}
	}
	[RPC]
	void StopFireOverNetwork()
	{
		m_currentWeapon.GetComponent<WeaponScript>().AlertBeamWeaponNotFiring();
	}
	[RPC]
	void RequestFireOverNetwork()
	{
		//Debug.Log ("Recieved remote request to fire");
		m_currentWeapon.GetComponent<WeaponScript>().PlayerRequestsFireNoRecoilCheck();
		//PlayerRequestsFire();
	}

	public GameObject FindAttachedWeapon()
	{
		foreach(Transform child in transform)
		{
			if(child.tag == "Weapon")
			{
				return child.gameObject;
			}
		}

		return null;
	}
}

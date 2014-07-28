using UnityEngine;
using System.Collections;

//TODO: Change this to support changing weapons at any time (ie, inventory system)
public class PlayerWeaponScript : MonoBehaviour 
{

	[SerializeField] GameObject m_currentWeapon;

    #region getset

    public void EquipWeapon(GameObject weapon)
	{
		m_currentWeapon = weapon;
	}

    #endregion getset

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
			m_currentWeapon.GetComponent<EquipmentWeapon>().PlayerRequestsFire();
		}
		else
		{
			if(m_currentWeapon.GetComponent<EquipmentWeapon>().CheckCanFire())
			{
				networkView.RPC ("RequestFireOverNetwork", RPCMode.Server);
				m_currentWeapon.GetComponent<EquipmentWeapon>().ActAsFired();
			}
		}
	}

	public void PlayerReleaseFire()
	{
		if(Network.isServer)
		{
			if(m_currentWeapon.GetComponent<EquipmentWeapon>().GetIsBeam())
				m_currentWeapon.GetComponent<EquipmentWeapon>().AlertBeamWeaponNotFiring();
		}
		else
		{
            if (m_currentWeapon.GetComponent<EquipmentWeapon>().GetIsBeam())
			{
				m_currentWeapon.GetComponent<EquipmentWeapon>().AlertBeamWeaponNotFiring();
				networkView.RPC ("StopFireOverNetwork", RPCMode.Server);
			}
		}
	}

	[RPC] void StopFireOverNetwork()
	{
		m_currentWeapon.GetComponent<EquipmentWeapon>().AlertBeamWeaponNotFiring();
	}

	[RPC] void RequestFireOverNetwork()
	{
		//Debug.Log ("Recieved remote request to fire");
		m_currentWeapon.GetComponent<EquipmentWeapon>().PlayerRequestsFireNoRecoilCheck();
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

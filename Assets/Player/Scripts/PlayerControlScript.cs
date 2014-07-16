using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum DockingState
{
    NOTDOCKING = 0,
    OnApproach = 1,
    OnEntry = 2,
    Docked = 3,
    Exiting = 4
}

public class PlayerControlScript : Ship
{

	[SerializeField]
	bool m_shouldRecieveInput = true;

	NetworkPlayer owner;
	[SerializeField]
	string ownerSt;

	[SerializeField]
	float m_baseEngineSpeed = 5.0f;
	[SerializeField]
	float m_baseEngineTurnSpeed = 1.0f;

	[SerializeField]
	int m_baseShipHull = 25;
	[SerializeField]
	float m_baseShipWeight = 0.05f;

	[SerializeField]
	float m_maxDockingSpeed = 225f;		//Maxmium docking speed for players
	[SerializeField]
	float m_dockRotateSpeed = 3f;			//How quickly to rotate the ship towards the dock
	float m_dockingTime = 0.0f;				//Used to determine if the player should continue the docking attempt

    //[SerializeField]
    //float m_playerMoveSpeed = 50.0f;
    //[SerializeField]
    //float m_playerRotateSpeed = 5.0f;
    [SerializeField]
    float m_playerStrafeMod = 0.6f;

	//Spacebux
	[SerializeField]
	int m_currentSpaceBucks = 0;
	public int GetSpaceBucks()
	{
		return m_currentSpaceBucks;
	}
	public void AddSpaceBucks(int amount)
	{
		m_currentSpaceBucks += amount;

		if(Network.player != owner)
		{
			networkView.RPC ("PropagateCashAmount", owner, m_currentSpaceBucks);
		}
	}
	[RPC]
	void PropagateCashAmount(int amount)
	{
		m_currentSpaceBucks = amount;
	}
	public bool CheckCanAffordAmount(int amount)
	{
		if(m_currentSpaceBucks >= amount)
			return true;
		else
			return false;
	}
	public bool RemoveSpaceBucks(int amount)
	{
		if(CheckCanAffordAmount(amount))
		{
			m_currentSpaceBucks -= amount;

			networkView.RPC ("PropagateCashAmount", RPCMode.Server, m_currentSpaceBucks);
			return true;
		}
		else
			return false;
	}

    protected override void Awake()
    {
        Init();
    }

	//Sounds
	bool shouldPlaySound = false;
	[RPC]
	void PropagateIsPlayingSound(bool isPlaying)
	{
		shouldPlaySound = isPlaying;

		if(isPlaying)
		{
			this.audio.volume = volumeHolder;
			this.audio.Play();
		}
		else
		{
			this.audio.Stop();
		}
	}

	public void EquipEngineStats(float moveSpeed, float turnSpeed, float strafeMod)
	{
        SetMaxShipSpeed(m_baseEngineSpeed + moveSpeed);
        SetCurrentShipSpeed(m_baseEngineSpeed + moveSpeed);
        SetRotateSpeed(m_baseEngineTurnSpeed + turnSpeed);
        m_playerStrafeMod = strafeMod;
        //m_playerMoveSpeed = m_baseEngineSpeed + moveSpeed;
        //m_playerRotateSpeed = m_baseEngineTurnSpeed + turnSpeed;
	}

	//Inventory
	public GameObject m_equippedWeaponItem;
	public void ResetEquippedWeapon()
	{
		Debug.Log ("Recieved request to reset weapon. Re-equipping weapon: " + m_equippedWeaponItem.GetComponent<ItemScript>().GetItemName());

		GameObject equippedWeap = GetWeaponObject();
		if(equippedWeap != null)
		{
			Debug.Log ("Destroyed old weapon: " + equippedWeap.name + ".");
			Network.Destroy(equippedWeap);
		}


		GameObject weapon = (GameObject)Network.Instantiate(m_equippedWeaponItem.GetComponent<ItemScript>().GetEquipmentReference(), this.transform.position, this.transform.rotation, 0);
		weapon.transform.parent = this.transform;
		weapon.transform.localPosition = weapon.GetComponent<WeaponScript>().GetOffset();

		networkView.RPC ("PropagateWeaponResetHomingBool", RPCMode.All, m_equippedWeaponItem.GetComponent<ItemScript>().GetEquipmentReference().GetComponent<WeaponScript>().m_needsLockon);
		
		//Parenting needs to be broadcast to all clients!
		string name = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameStateController>().GetNameFromNetworkPlayer(owner);
		weapon.GetComponent<WeaponScript>().ParentWeaponToOwner(name);
		this.GetComponent<PlayerWeaponScript>().EquipWeapon(weapon);
	}
	[RPC]
	void PropagateWeaponResetHomingBool(bool state)
	{
		if(owner == Network.player)
		{
			GameObject.FindGameObjectWithTag("GUIManager").GetComponent<GUIManager>().m_currentWeaponNeedsLockon = state;
		}
	}

	public GameObject m_equippedShieldItem;
	public void ResetEquippedShield()
	{
		GameObject shield = (GameObject)Network.Instantiate(m_equippedShieldItem.GetComponent<ItemScript>().GetEquipmentReference(), this.transform.position, this.transform.rotation, 0);
		shield.transform.parent = this.transform;
		
		ShieldScript ssc = shield.GetComponent<ShieldScript>();
		shield.transform.localPosition = ssc.GetOffset();

		string name = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameStateController>().GetNameFromNetworkPlayer(owner);
		ssc.ParentToPlayer(name);

		HealthScript HP = this.GetComponent<HealthScript>();
		HP.EquipNewShield(ssc.GetShieldMaxCharge(), ssc.GetShieldRechargeRate(), ssc.GetShieldRechargeDelay());
	}
	public GameObject m_equippedEngineItem;
	public void ResetEquippedEngine()
	{
		GameObject engine = (GameObject)Network.Instantiate(m_equippedEngineItem.GetComponent<ItemScript>().GetEquipmentReference(), this.transform.position, this.transform.rotation, 0);
		engine.transform.parent = this.transform;

		EngineScript esc = engine.GetComponent<EngineScript>();
		engine.transform.localPosition = esc.GetOffset();

		string name = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameStateController>().GetNameFromNetworkPlayer(owner);
		esc.ParentToPlayer(name);

        //m_playerMoveSpeed = m_baseEngineSpeed + esc.GetMoveSpeed();
        //m_playerRotateSpeed = m_baseEngineTurnSpeed + esc.GetTurnSpeed();
        m_playerStrafeMod = esc.GetStrafeModifier();
        SetMaxShipSpeed(m_baseEngineSpeed + esc.GetMoveSpeed());
        SetCurrentShipSpeed(m_baseEngineSpeed + esc.GetMoveSpeed());
        SetRotateSpeed(m_baseEngineTurnSpeed + esc.GetTurnSpeed());

        ResetThrusters();

	}

	public GameObject m_equippedPlatingItem;
	public void ResetEquippedPlating()
	{
		GameObject plating = (GameObject)Network.Instantiate(m_equippedPlatingItem.GetComponent<ItemScript>().GetEquipmentReference(), this.transform.position, this.transform.rotation, 0);
		plating.transform.parent = this.transform;
		
		PlatingScript psc = plating.GetComponent<PlatingScript>();
		plating.transform.localPosition = psc.GetOffset();

		string name = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameStateController>().GetNameFromNetworkPlayer(owner);
		psc.ParentToPlayer(name);

		//Update hull
		this.GetComponent<HealthScript>().EquipNewPlating (psc.GetPlatingHealth() + m_baseShipHull);

		//Update RB
		rigidbody.mass = m_baseShipWeight + psc.GetPlatingWeight();
	}

	public List<GameObject> m_playerInventory;
	public void AddItemToInventoryLocalOnly(GameObject itemWrapper)
	{
		if(!InventoryIsFull())
		{
			m_playerInventory.Add(itemWrapper);
		}
	}
	public void AddItemToInventory(GameObject itemWrapper)
	{
		if(Network.isServer)
		{
			if(!InventoryIsFull())
			{
				m_playerInventory.Add(itemWrapper);
			}
		}
		else
		{
			if(!InventoryIsFull())
			{
				m_playerInventory.Add(itemWrapper);
			}
			networkView.RPC ("TellServerAddItem", RPCMode.Server, itemWrapper.GetComponent<ItemScript>().m_equipmentID);
			//networkView.RPC ("TellServerAddItem", RPCMode.Server, itemWrapper);
		}
	}
	[RPC]
	void TellServerAddItem(int id)
	{
		//AddItemToInventory(item);
		AddItemToInventory(GameObject.FindGameObjectWithTag("ItemManager").GetComponent<ItemIDHolder>().GetItemWithID(id));

	}
	public void RemoveItemFromInventoryLocalOnly(GameObject itemWrapper)
	{
		if(m_playerInventory.Contains(itemWrapper))
		{
			m_playerInventory.Remove(itemWrapper);
		}
	}
	public void RemoveItemFromInventory(GameObject itemWrapper)
	{
		if(Network.isServer)
		{
			if(m_playerInventory.Contains(itemWrapper))
			{
				m_playerInventory.Remove(itemWrapper);
			}
		}
		else
		{
			if(m_playerInventory.Contains(itemWrapper))
			{
				m_playerInventory.Remove(itemWrapper);
			}
			networkView.RPC ("TellServerRemoveItem", RPCMode.Server, itemWrapper.GetComponent<ItemScript>().m_equipmentID);
		}
	}
	[RPC]
	void TellServerRemoveItem(int id)
	{
		RemoveItemFromInventory(GameObject.FindGameObjectWithTag("ItemManager").GetComponent<ItemIDHolder>().GetItemWithID(id));
	}
	public void ClearInventory()
	{
		m_playerInventory.Clear();
	}
	public bool InventoryIsFull()
	{
		if(m_playerInventory.Count > 4)
		{
			return true;
		}
		else
			return false;
	}
	public GameObject GetItemInSlot(int slot)
	{
		if(m_playerInventory[slot] != null)
		{
			return m_playerInventory[slot];
		}
		else
			return null;
	}
	public void EquipItemInSlot(int slot)
	{
		if(Network.isServer)
		{
			switch(m_playerInventory[slot].GetComponent<ItemScript>().m_typeOfItem)
			{
				case ItemType.Weapon:
				{
					//If we're told to equip a weapon:
					Debug.Log ("Equipping weapon " + m_playerInventory[slot].GetComponent<ItemScript>().GetItemName() + " on player #" + owner);

					//Unequip old weapon
					GameObject temp = m_equippedWeaponItem;
					//Destroy object
					Network.Destroy(GetWeaponObject());

					//Equip new weapon
					GameObject newWeapon = m_playerInventory[slot];
					m_equippedWeaponItem = newWeapon;

					if(owner == Network.player)
					{
						if(newWeapon.GetComponent<ItemScript>().GetEquipmentReference().GetComponent<WeaponScript>().m_needsLockon)
						{
							GameObject.FindGameObjectWithTag("GUIManager").GetComponent<GUIManager>().m_currentWeaponNeedsLockon = true;
							Debug.Log ("New weapon is homing, alerting GUI...");
						}
						else
						{
							GameObject.FindGameObjectWithTag("GUIManager").GetComponent<GUIManager>().m_currentWeaponNeedsLockon = false;
							Debug.Log ("Weapon is not homing. Alerting GUI.");
						}
					}
				
					GameObject weapon = (GameObject)Network.Instantiate(m_equippedWeaponItem.GetComponent<ItemScript>().GetEquipmentReference(), this.transform.position, this.transform.rotation, 0);
					weapon.transform.parent = this.transform;
					weapon.transform.localPosition = weapon.GetComponent<WeaponScript>().GetOffset();
					string name = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameStateController>().GetNameFromNetworkPlayer(owner);
					weapon.GetComponent<WeaponScript>().ParentWeaponToOwner(name);
					//Broadcast parenting here too
					this.GetComponent<PlayerWeaponScript>().EquipWeapon(weapon);

					//Send relevant info back to client
					
					networkView.RPC ("ReturnInfoToEquippingClient", owner, m_equippedWeaponItem.GetComponent<ItemScript>().m_equipmentID);

					//Take new weapon out of inventory
					RemoveItemFromInventory(m_playerInventory[slot]);

					//Place old weapon into inventory
					AddItemToInventory(temp);
					break;
				}
				case ItemType.Shield:
				{
					Debug.Log ("Equipping shield " + m_playerInventory[slot].GetComponent<ItemScript>().GetItemName() + " on player #" + owner);

					//Unequip old shield
					GameObject temp = m_equippedShieldItem;
					//Destroy the sheld
					Network.Destroy (GetShieldObject());

					//Equip the new shield
					GameObject newShield = m_playerInventory[slot];
					m_equippedShieldItem = newShield;
					GameObject shield = (GameObject)Network.Instantiate(m_equippedShieldItem.GetComponent<ItemScript>().GetEquipmentReference(), this.transform.position, this.transform.rotation, 0);
					shield.transform.parent = this.transform;

					ShieldScript ssc = shield.GetComponent<ShieldScript>();
					shield.transform.localPosition = ssc.GetOffset();

					string name = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameStateController>().GetNameFromNetworkPlayer(owner);
					ssc.ParentToPlayer(name);
					
					//TODO: Add changes to HPscript here
					HealthScript HP = this.GetComponent<HealthScript>();
					Debug.Log ("Attempting to access shield script on item: " + shield.name);
					HP.EquipNewShield(ssc.GetShieldMaxCharge(), ssc.GetShieldRechargeRate(), ssc.GetShieldRechargeDelay());

					//Remove new shield from inv
					RemoveItemFromInventory(m_playerInventory[slot]);

					//Place old shield into inv
					AddItemToInventory(temp);
					break;
				}
				case ItemType.Engine:
				{
					Debug.Log ("Equipping engine " + m_playerInventory[slot].GetComponent<ItemScript>().GetItemName() + " on player #" + owner);

					//Unequip old engine
					GameObject temp = m_equippedEngineItem;
					//Destroy the engine object
					Network.Destroy(GetEngineObject());

					//Equip new shield
					GameObject newEngine = m_playerInventory[slot];
					m_equippedEngineItem = newEngine;
					GameObject engine = (GameObject)Network.Instantiate(m_equippedEngineItem.GetComponent<ItemScript>().GetEquipmentReference(), this.transform.position, this.transform.rotation, 0);
					engine.transform.parent = this.transform;

					EngineScript esc = engine.GetComponent<EngineScript>();
					engine.transform.localPosition = esc.GetOffset();
				
					string name = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameStateController>().GetNameFromNetworkPlayer(owner);
					esc.ParentToPlayer(name);

					//Change our move stats
					SetMaxShipSpeed(m_baseEngineSpeed + esc.GetMoveSpeed());
                    SetCurrentShipSpeed(m_baseEngineSpeed + esc.GetMoveSpeed());
					SetRotateSpeed(m_baseEngineTurnSpeed + esc.GetTurnSpeed());	

					//Remove new engine from inv
					RemoveItemFromInventory(m_playerInventory[slot]);

					//Place old engine into inv
					AddItemToInventory(temp);
					break;
				}
				case ItemType.Plating:
				{
					Debug.Log ("Equipping plating " + m_playerInventory[slot].GetComponent<ItemScript>().GetItemName() + " on player #" + owner);

					//Unequip old plating
					GameObject temp = m_equippedPlatingItem;
					//Destroy plating object
					Network.Destroy(GetPlatingObject());

					//Equip new plating
					GameObject newPlating = m_playerInventory[slot];
					m_equippedPlatingItem = newPlating;
					GameObject plating = (GameObject)Network.Instantiate(m_equippedPlatingItem.GetComponent<ItemScript>().GetEquipmentReference(), this.transform.position, this.transform.rotation, 0);
					plating.transform.parent = this.transform;

					PlatingScript psc = plating.GetComponent<PlatingScript>();
					plating.transform.localPosition = psc.GetOffset();
				
					string name = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameStateController>().GetNameFromNetworkPlayer(owner);
					psc.ParentToPlayer(name);

					//Update our HP
					this.GetComponent<HealthScript>().EquipNewPlating (psc.GetPlatingHealth() + m_baseShipHull);

					//Update RB
					rigidbody.mass = m_baseShipWeight + psc.GetPlatingWeight();

					//Remove new plating from inv
					RemoveItemFromInventory(m_playerInventory[slot]);

					//Add old plating to inv
					AddItemToInventory(temp);
					break;
				}
			}
		}
		else
		{
			switch(m_playerInventory[slot].GetComponent<ItemScript>().m_typeOfItem)
			{
				case ItemType.Weapon:
				{
					//If we're told to equip a weapon:
					
					//Unequip old weapon
					GameObject temp = m_equippedWeaponItem;
					//Destroy object
					//Network.Destroy(GetWeaponObject());
					
					//Equip new weapon
					/*GameObject newWeapon = m_playerInventory[slot];
					m_equippedWeaponItem = newWeapon;*/
					//GameObject weapon = (GameObject)Instantiate(m_equippedWeaponItem.GetComponent<ItemScript>().GetEquipmentReference(), this.transform.position, this.transform.rotation);
					//weapon.transform.parent = this.transform;
					//this.GetComponent<PlayerWeaponScript>().EquipWeapon(weapon);
					
					//If it's a homing weapon, alert the GUI
					if(m_playerInventory[slot].GetComponent<ItemScript>().GetEquipmentReference().GetComponent<WeaponScript>().m_needsLockon)
					{
						GameObject.FindGameObjectWithTag("GUIManager").GetComponent<GUIManager>().m_currentWeaponNeedsLockon = true;
						Debug.Log ("New weapon is homing, alerting GUI...");
					}
					else
					{
						GameObject.FindGameObjectWithTag("GUIManager").GetComponent<GUIManager>().m_currentWeaponNeedsLockon = false;
						Debug.Log ("Weapon is not homing. Alerting GUI.");
					}
				
					//Take new weapon out of inventory
					//RemoveItemFromInventory(m_playerInventory[slot]);*/
					RemoveItemFromInventoryLocalOnly(m_playerInventory[slot]);
					
					//Place old weapon into inventory
					//AddItemToInventory(temp);
					AddItemToInventoryLocalOnly(temp);

					
					
					break;
				}
				case ItemType.Shield:
				{
					//Unequip old
					GameObject temp = m_equippedShieldItem;

					//Don't destroy, handled by server
					//Equip new
					GameObject newShield = m_playerInventory[slot];
					m_equippedShieldItem = newShield;

					//Update local inventory
					RemoveItemFromInventoryLocalOnly(m_playerInventory[slot]);
					AddItemToInventoryLocalOnly(temp);
					break;
				}
				case ItemType.Engine:
				{
					//Unequip old
					GameObject temp = m_equippedEngineItem;
					
					//Don't destroy, handled by server
					//Equip new
					GameObject newEngine = m_playerInventory[slot];
					m_equippedEngineItem = newEngine;
					
					//Update local inventory
					RemoveItemFromInventoryLocalOnly(m_playerInventory[slot]);
					AddItemToInventoryLocalOnly(temp);
					break;
				}
				case ItemType.Plating:
				{
					//Unequip old
					GameObject temp = m_equippedPlatingItem;
					
					//Don't destroy, handled by server
					//Equip new
					GameObject newPlating = m_playerInventory[slot];
					m_equippedPlatingItem = newPlating;
					
					//Update local inventory
					RemoveItemFromInventoryLocalOnly(m_playerInventory[slot]);
					AddItemToInventoryLocalOnly(temp);
					break;
				}
			}
			networkView.RPC ("TellServerEquipItemInSlot", RPCMode.Server, slot);
		}
	}
	[RPC]
	void TellServerEquipItemInSlot(int slot)
	{
		EquipItemInSlot(slot);
	}
	[RPC]
	void ReturnInfoToEquippingClient(int weaponID)
	{
		GameObject equipmentObject = GameObject.FindGameObjectWithTag("ItemManager").GetComponent<ItemIDHolder>().GetItemWithID(weaponID);

		m_equippedWeaponItem = equipmentObject;
	}

	public void SetNewTargetLock(GameObject target)
	{
		GetWeaponObject().GetComponent<WeaponScript>().SetTarget(target);
		//Debug.Log ("Receieved target lock on enemy: " + target.name);
	}
	public void UnsetTargetLock()
	{
		GetWeaponObject().GetComponent<WeaponScript>().UnsetTarget();
	}
	public float GetReloadPercentage()
	{
		return GetWeaponObject().GetComponent<WeaponScript>().GetReloadPercentage();
	}


	// Use this for initialization
	float volumeHolder = 1.0f;
	void Start () 
	{
		volumeHolder = PlayerPrefs.GetFloat("EffectVolume", 1.0f);

		m_currentVelocity = Vector3.zero;
		m_playerInventory = new List<GameObject>(5);

		//ResetEquippedWeapon();
		if(Network.isServer)
		{
			ResetEquippedWeapon();
			ResetEquippedShield();
			ResetEquippedEngine();
			ResetEquippedPlating();
		}

		timeSinceLastPacket = Time.realtimeSinceStartup;

		previousPacketPosition = this.transform.position;
		predictedPacketPosition = this.transform.position;
	}

	float timeSinceLastPacket;
	IEnumerator DeadReckonPosition(Vector3 newPos, Vector3 newVel)
	{
		Debug.Log ("Received new velocity: " + newVel + ", against current velocity: " + this.rigidbody.velocity);
		Vector3 accel = (newVel - this.rigidbody.velocity) / (Time.realtimeSinceStartup - timeSinceLastPacket);
		timeSinceLastPacket = Time.realtimeSinceStartup;
		float steps = 0.05f;
		
		Vector3 coord1 = this.transform.position;
		Vector3 coord2 = coord1 + rigidbody.velocity;
		Vector3 coord3 = newPos + (newVel * steps) + (0.5f * accel * steps * steps);
		Vector3 coord4 = coord3 - (newVel + (accel * steps));
		
		float A = coord4.x - (3 * coord3.x) + (3 * coord2.x) - coord1.x;
		float B = (3 * coord3.x) - (6 * coord2.x) + (3 * coord1.x);
		float C = (3 * coord2.x) - (3 * coord1.x);
		float D = coord1.x;
		
		float E = coord4.y - (3 * coord3.y) + (3 * coord2.y) - coord1.y;
		float F = (3 * coord3.y) - (6 * coord2.y) + (3 * coord1.y);
		float G = (3 * coord2.y) - (3 * coord1.y);
		float H = coord1.y;
		
		float I = coord4.z - (3 * coord3.z) + (3 * coord2.z) - coord1.z;
		float J = (3 * coord3.z) - (6 * coord2.z) + (3 * coord1.z);
		float K = (3 * coord2.z) - (3 * coord1.z);
		float L = coord1.z;
		
		float time = 0;
		while(time < steps)
		{
			time += Time.deltaTime;
			//float t = time / timeDiff;
			float t = time * (1.0f / steps);
			
			float X = (A * t * t * t) + (B * t * t) + (C * t) + D;
			float Y = (E * t * t * t) + (F * t * t) + (G * t) + H;
			//float Y = Mathf.Lerp (coord1.y, newPos.y);
			float Z = (I * t * t * t) + (J * t * t) + (K * t) + L;
			
			//this.transform.position = new Vector3(X, this.transform.position.y, Z);
			this.transform.position = new Vector3(X, Y, Z);
			rigidbody.velocity = rigidbody.velocity + accel;
			yield return 0;
		}
		
		this.rigidbody.velocity = newVel;
	}

	IEnumerator ContinuePlayerMovement()
	{
		float t = 0.0f;

		while(t < 0.2f)
		{
			t += Time.deltaTime;
			rigidbody.MovePosition(rigidbody.position + (rigidbody.velocity * Time.deltaTime * Time.deltaTime));
			yield return 0;
		}
	}
	float rotationInc = 0.0f;
	float prevRot = 0.0f;
	IEnumerator ContinuePlayerRotation()
	{
		float t = 0.0f;

		while(t < 0.2f)
		{
			t += Time.deltaTime;
			rigidbody.MoveRotation(Quaternion.Euler(0, 0, (this.transform.rotation.eulerAngles.z + (rotationInc * Time.deltaTime * Time.deltaTime))));
			yield return 0;
		}
	}

	Vector3 previousPacketPosition = Vector3.zero;
	Vector3 predictedPacketPosition = Vector3.zero;
	float timeSinceLastPacketNew = 0.0f;

	void OnSerializeNetworkView(BitStream stream, NetworkMessageInfo info)
	{
		Vector3 position = this.transform.position;
		float zRotation = this.transform.rotation.eulerAngles.z;
		Vector3 velocity = this.rigidbody.velocity;
		
		if(stream.isWriting)
		{
			//We're the owner, send our info to other people
			stream.Serialize(ref position);
			stream.Serialize(ref zRotation);
			stream.Serialize(ref velocity);
		}
		else
		{
			//We're recieving info from a remote client
			stream.Serialize(ref position);
			stream.Serialize(ref zRotation);
			stream.Serialize(ref velocity);

			if(position != predictedPacketPosition)
			{
				//Jump to recieved pos
				this.transform.position = position;
			}
			else
			{
				//If it matches our predication, don't do anything!
				Debug.Log ("Recieved position matches prediction!");
			}

			//this.transform.position = position;

			this.rigidbody.velocity = velocity;
			prevRot = this.transform.rotation.eulerAngles.z;
			rotationInc = zRotation - prevRot;
			this.transform.rotation = Quaternion.Euler(0, 0, zRotation);

			//StartCoroutine(ContinuePlayerMovement());

			//Calculate the predicted position for next time
			/*predictedPacketPosition = position + (previousPacketPosition - position);
			previousPacketPosition = position;
			timeSinceLastPacketNew = 0.0f;*/
		}
	}

	//Movement vars
	public Vector3 m_currentVelocity;

	public bool m_isInRangeOfCapitalDock = false;
	public bool m_isInRangeOfTradingDock = false;
	public GameObject nearbyShop = null;

	[SerializeField]
	bool m_isAnimating = false;
	[SerializeField]
	DockingState m_currentDockingState = DockingState.NOTDOCKING;

	[SerializeField]
	GameObject CShip = null;
	[SerializeField]
	Vector3 targetPoint = Vector3.zero;

	public void InitPlayerOnCShip(GameObject CShip)
	{
		this.CShip = CShip;
		targetPoint = CShip.transform.position;

		networkView.RPC ("PropagateInvincibility", RPCMode.All, false);
		rigidbody.isKinematic = true;

		m_isAnimating = true;
		m_currentDockingState = DockingState.Docked;
		GameObject.FindGameObjectWithTag("GameController").GetComponent<GameStateController>().NotifyLocalPlayerHasDockedAtCShip();
	}

	public void TellPlayerStopDocking()
	{
		//Alert the camera
		Camera.main.GetComponent<CameraScript>().TellCameraPlayerIsUnDocked();

		//Unparent ourselves
		transform.parent = null;

		//Reinstate movement (although input should never be cut anyway)
		m_shouldRecieveInput = true;
		networkView.RPC ("PropagateInvincibility", RPCMode.All, false);
		rigidbody.isKinematic = false;

		//Alert animation it needs to leave
		m_currentDockingState = DockingState.Exiting;
	}

	public void SetInputMethod(bool useControl)
	{
		useController = useControl;
	}

	[RPC]
	void PropagateInvincibility(bool state)
	{
		GetComponent<HealthScript>().m_isInvincible = state;
	}

	bool useController = false;
	Quaternion targetAngle;
	// Update is called once per frame
    protected override void Update() 
	{
		ownerSt = owner.ToString();
		bool recievedInput = false;
		base.Update();

		if(owner != null && owner == Network.player)
		{
			if((useController && Input.GetButtonDown("X360Start")) || (!useController && Input.GetKeyDown(KeyCode.Escape)))
			{
				GameObject.FindGameObjectWithTag("GUIManager").GetComponent<GUIManager>().ToggleMenuState();
			}

			if(m_isAnimating)
			{
				//If for any reason CShip is not set, find it
				if(CShip == null)
				{
					CShip = GameObject.FindGameObjectWithTag("Capital");
				}
				
				//If still on the entrance phases, allow cancelling with 'X'
				/*if(Input.GetKey (KeyCode.X))
					{
						if(m_currentDockingState == DockingState.OnApproach || m_currentDockingState == DockingState.OnEntry)
						{
							//Cancel the animation
							m_isAnimating = false;
						}
					}*/
				
				//Now animate based on state
				switch(m_currentDockingState)
				{
				case DockingState.NOTDOCKING:
				{
					//We shouldn't even be here man
					//We shouln't even BE here!
					m_isAnimating = false;
					networkView.RPC ("PropagateInvincibility", RPCMode.All, false);
					rigidbody.isKinematic = false;
					break;
				}
				case DockingState.OnApproach:
				{
					// Make sure targetPoint is up to date
					targetPoint = CShip.transform.position + (CShip.transform.right * 7.0f);
					
					// Move towards entrance point
					Vector3 direction = targetPoint - transform.position;
					Vector3 rotation = -CShip.transform.right;
					MoveToDockPoint (direction, rotation);
					
					// If we're near, switch to onEntry
					if(direction.magnitude <= 1.35f)
					{
						m_dockingTime += Time.deltaTime;
						
						if (m_dockingTime >= 0.36f)
						{
							// Reset the docking time
							m_dockingTime = 0f;
							
							// Kill our speed temporarily
							rigidbody.isKinematic = true;
							m_currentDockingState = DockingState.OnEntry;
							targetPoint = CShip.transform.position + (CShip.transform.up * 1.5f);
							rigidbody.isKinematic = false;
							this.transform.position = new Vector3(this.transform.position.x, this.transform.position.y, 10.75f);
						}
					}
					
					else
					{
						m_dockingTime = 0f;
					}
					
					//Play sounds
					if(!shouldPlaySound)
					{
						shouldPlaySound = true;
						this.audio.volume = volumeHolder;
						this.audio.Play();
						networkView.RPC ("PropagateIsPlayingSound", RPCMode.Others, shouldPlaySound);
					}
					recievedInput = true;
					break;
				}
				case DockingState.OnEntry:
				{
					//Make sure targetPoint is up to date
					targetPoint = CShip.transform.position;
					
					//Rotate towards entrance point
					Vector3 direction = targetPoint - transform.position;
					Vector3 rotation = -CShip.transform.right;
					MoveToDockPoint (direction, rotation);
					
					//If we're near, switch to docked and cut input. Then alert GUI we've docked
					if (direction.magnitude <= 1.5f)
					{
						m_dockingTime += Time.deltaTime;
						
						if (m_dockingTime >= 0.25f)
						{
							// Reset the docking time
							m_dockingTime = 0f;
							
							// Perform docking process
							m_currentDockingState = DockingState.Docked;
							transform.rotation = CShip.transform.rotation;
							GameObject.FindGameObjectWithTag("GameController").GetComponent<GameStateController>().NotifyLocalPlayerHasDockedAtCShip();
							transform.parent = CShip.transform;
							rigidbody.isKinematic = true;
							networkView.RPC ("PropagateInvincibility", RPCMode.All, true);
						}
					}
					
					else
					{
						// Reset the docking time
						m_dockingTime = 0f;
					}
					
					//Play sounds
					if(!shouldPlaySound)
					{
						shouldPlaySound = true;
						this.audio.volume = volumeHolder;
						this.audio.Play();
						networkView.RPC ("PropagateIsPlayingSound", RPCMode.Others, shouldPlaySound);
					}
					recievedInput = true;
					break;
				}
				case DockingState.Docked:
				{
					//We shouldn't need to do anything. Await GUI telling us we're done
					
					// Stop exception spam by ensuring the CShip is alive
					if (CShip)
					{
						//Ensure rotation matches CShip
						transform.rotation = CShip.transform.rotation;
						
						//Also position
						float oldZ = transform.position.z;
						transform.position = new Vector3(CShip.transform.position.x, CShip.transform.position.y, oldZ);

					}
					break;
				}
				case DockingState.Exiting:
				{
					//Accelerate forwards
					this.rigidbody.AddForce(this.transform.up * GetCurrentMomentum() * Time.deltaTime);
					
					//If we're far enough away, stop animating
					Vector3 dir = CShip.transform.position - transform.position;
					if(dir.magnitude >= 12.0f)
					{
						//Fly free!
						m_currentDockingState = DockingState.NOTDOCKING;
						m_isAnimating = false;
						this.transform.position = new Vector3(this.transform.position.x, this.transform.position.y, 10.0f);
						networkView.RPC ("PropagateInvincibility", RPCMode.All, false);
						rigidbody.isKinematic = false;
					}
					
					//Play the sound
					if(!shouldPlaySound)
					{
						shouldPlaySound = true;
						this.audio.volume = volumeHolder;
						this.audio.Play();
						networkView.RPC ("PropagateIsPlayingSound", RPCMode.Others, shouldPlaySound);
					}
					recievedInput = true;
					break;
				}
				}
			}

			if(m_shouldRecieveInput)
			{
				if(useController && Input.GetJoystickNames().Length < 1)
				{
					useController = false;
				}


				if(!m_isAnimating)
				{
					if((useController && Input.GetButtonDown("X360X")) || (!useController && Input.GetKey (KeyCode.X)))
					{
						if(m_isInRangeOfCapitalDock)
						{
							//GameObject.FindGameObjectWithTag("GUIManager").GetComponent<GUIManager>().m_PlayerHasDockedAtCapital = true;
							/*GameObject.FindGameObjectWithTag("GameController").GetComponent<GameStateController>().NotifyLocalPlayerHasDockedAtCShip();
							transform.parent = GameObject.FindGameObjectWithTag("Capital").transform;
							rigidbody.isKinematic = true;
							m_shouldRecieveInput = false;*/

							//Begin the animation sequence
							CShip = GameObject.FindGameObjectWithTag("Capital");
							targetPoint = CShip.transform.position + (CShip.transform.right * 7.0f) + (CShip.transform.up * 1.5f);
							m_currentDockingState = DockingState.OnApproach;
							GameObject.FindGameObjectWithTag("GUIManager").GetComponent<GUIManager>().CloseMap();
							m_isAnimating = true;
						}
						else if(m_isInRangeOfTradingDock && nearbyShop != null)
						{
							GameObject.FindGameObjectWithTag("GameController").GetComponent<GameStateController>().NotifyLocalPlayerHasDockedAtShop(nearbyShop);
							transform.parent = nearbyShop.transform;
							rigidbody.isKinematic = true;
							m_shouldRecieveInput = false;
							
						}
					}

					//In here, player should respond to any input
					if(!useController)
					{
						if(Input.GetKey(KeyCode.W))
						{
							//this.rigidbody.AddForce(this.transform.up * m_playerMoveSpeed * Time.deltaTime);
                            this.rigidbody.AddForce(this.transform.up * GetCurrentMomentum() * Time.deltaTime);
							
							//Play sound + particles
							if(!shouldPlaySound)
							{
								shouldPlaySound = true;
								this.audio.volume = volumeHolder;
								this.audio.Play();
								networkView.RPC ("PropagateIsPlayingSound", RPCMode.Others, shouldPlaySound);
							}
							recievedInput = true;
						}

						if(Input.GetKey (KeyCode.S))
						{
							//this.rigidbody.AddForce(this.transform.up * -m_playerMoveSpeed * Time.deltaTime);
                            this.rigidbody.AddForce(this.transform.up * (-GetCurrentMomentum() * m_playerStrafeMod) * Time.deltaTime);
							
							if(!shouldPlaySound)
							{
								shouldPlaySound = true;
								this.audio.volume = volumeHolder;							
								this.audio.Play();
								networkView.RPC ("PropagateIsPlayingSound", RPCMode.Others, shouldPlaySound);
							}
							recievedInput = true;
						}

						if(Input.GetKey (KeyCode.A))
						{
							//this.rigidbody.AddForce(this.transform.right * (-m_playerMoveSpeed * m_playerStrafeMod) * Time.deltaTime);
                            this.rigidbody.AddForce(this.transform.right * (-GetCurrentMomentum() * m_playerStrafeMod) * Time.deltaTime);
							
							if(!shouldPlaySound)
							{
								shouldPlaySound = true;
								this.audio.volume = volumeHolder;
								this.audio.Play();
								networkView.RPC ("PropagateIsPlayingSound", RPCMode.Others, shouldPlaySound);
							}
							recievedInput = true;
						}

						if(Input.GetKey (KeyCode.D))
						{
							//this.rigidbody.AddForce(this.transform.right * (m_playerMoveSpeed * m_playerStrafeMod) * Time.deltaTime);
                            this.rigidbody.AddForce(this.transform.right * (GetCurrentMomentum() * m_playerStrafeMod) * Time.deltaTime);
							
							if(!shouldPlaySound)
							{
								shouldPlaySound = true;
								this.audio.volume = volumeHolder;
								this.audio.Play();
								networkView.RPC ("PropagateIsPlayingSound", RPCMode.Others, shouldPlaySound);
							}
							recievedInput = true;
						}

						if(Input.GetKeyDown(KeyCode.Tab))
						{
							GameObject.FindGameObjectWithTag("GUIManager").GetComponent<GUIManager>().ToggleMap();
						}
						
						if(Input.GetKeyDown(KeyCode.Z))
						{
							bool mapVal = GameObject.FindGameObjectWithTag("GUIManager").GetComponent<GUIManager>().m_isOnFollowMap;
							GameObject.FindGameObjectWithTag("GUIManager").GetComponent<GUIManager>().m_isOnFollowMap = !mapVal;
						}

						if(Input.GetKeyDown (KeyCode.P))
						{
							GameObject.FindGameObjectWithTag("Capital").GetComponent<HealthScript>().RemotePlayerRequestsDirectDamage(150);
						}
					}
					else
					{
						if(Input.GetAxis("LeftStickVertical") > 0)
						{
							//Forward
							float v = Input.GetAxis("LeftStickVertical");
							float h = Input.GetAxis("LeftStickHorizontal");

							Vector3 inputVec = new Vector3(h, v, 0);
							if(inputVec.sqrMagnitude > 1.0f)
							{
								inputVec.Normalize();
								inputVec *= 0.7071067f;
							}
							Vector3 forward = this.transform.up;

							float forwardSpeedFac = Mathf.Abs(Vector3.Dot(inputVec.normalized, forward));

							float speed = 0;
							if(forwardSpeedFac > 0.95f)
							{
								//Apply forward speed
								speed = GetCurrentShipSpeed();
							}
							else
							{
								//Apply side speed
								speed = GetCurrentShipSpeed() * m_playerStrafeMod;
							}

							//float sideSpeedFac = Mathf.Abs(Vector3.Dot(inputVec, this.transform.right));
							//float speed = (forwardSpeedFac * m_playerMoveSpeed) + (sideSpeedFac * (m_playerMoveSpeed * m_playerStrafeMod));

							Vector3 moveFac = inputVec * speed;

							this.rigidbody.AddForce(moveFac * Time.deltaTime);

							if(!shouldPlaySound)
							{
								shouldPlaySound = true;
								this.audio.volume = volumeHolder;
								this.audio.Play();
								networkView.RPC ("PropagateIsPlayingSound", RPCMode.Others, shouldPlaySound);
							}
							recievedInput = true;
						}

						if(Input.GetAxis("LeftStickVertical") < 0)
						{
							//Back
							float v = Input.GetAxis("LeftStickVertical");
							float h = Input.GetAxis("LeftStickHorizontal");
							
							Vector3 inputVec = new Vector3(h, v, 0);
							if(inputVec.sqrMagnitude > 1.0f)
							{
								inputVec.Normalize();
								inputVec *= 0.7071067f;
							}
							Vector3 forward = this.transform.up;
							
							float forwardSpeedFac = Mathf.Abs(Vector3.Dot(inputVec.normalized, forward));
							float speed = 0;
							if(forwardSpeedFac > 0.95f)
							{
								//Apply forward speed
                                speed = GetCurrentShipSpeed();
							}
							else
							{
								//Apply side speed
								speed = GetCurrentShipSpeed() * m_playerStrafeMod;
							}

							Vector3 moveFac = inputVec * speed;
							
							this.rigidbody.AddForce(moveFac * Time.deltaTime);
							
							if(!shouldPlaySound)
							{
								shouldPlaySound = true;
								this.audio.volume = volumeHolder;
								this.audio.Play();
								networkView.RPC ("PropagateIsPlayingSound", RPCMode.Others, shouldPlaySound);
							}
							recievedInput = true;
						}

						if(Input.GetAxis("LeftStickHorizontal") < 0)
						{
							//Left
							float v = Input.GetAxis("LeftStickVertical");
							float h = Input.GetAxis("LeftStickHorizontal");
							
							Vector3 inputVec = new Vector3(h, v, 0);
							if(inputVec.sqrMagnitude > 1.0f)
							{
								inputVec.Normalize();
								inputVec *= 0.7071067f;
							}
							Vector3 forward = this.transform.up;
							
							float forwardSpeedFac = Mathf.Abs(Vector3.Dot(inputVec.normalized, forward));
							float speed = 0;
							if(forwardSpeedFac > 0.95f)
							{
								//Apply forward speed
								speed = GetCurrentShipSpeed();
							}
							else
							{
								//Apply side speed
								speed = GetCurrentShipSpeed() * m_playerStrafeMod;
							}

							Vector3 moveFac = inputVec * speed;
							
							this.rigidbody.AddForce(moveFac * Time.deltaTime);
							
							if(!shouldPlaySound)
							{
								shouldPlaySound = true;
								this.audio.volume = volumeHolder;
								this.audio.Play();
								networkView.RPC ("PropagateIsPlayingSound", RPCMode.Others, shouldPlaySound);
							}
							recievedInput = true;
						}

						if(Input.GetAxis("LeftStickHorizontal") > 0)
						{
							//Right
							float v = Input.GetAxis("LeftStickVertical");
							float h = Input.GetAxis("LeftStickHorizontal");
							
							Vector3 inputVec = new Vector3(h, v, 0);
							if(inputVec.sqrMagnitude > 1.0f)
							{
								inputVec.Normalize();
								inputVec *= 0.7071067f;
							}
							Vector3 forward = this.transform.up;
							
							float forwardSpeedFac = Mathf.Abs(Vector3.Dot(inputVec.normalized, forward));
							float speed = 0;
							if(forwardSpeedFac > 0.95f)
							{
								//Apply forward speed
                                speed = GetCurrentShipSpeed();
							}
							else
							{
								//Apply side speed
                                speed = GetCurrentShipSpeed() * m_playerStrafeMod;
							}

							Vector3 moveFac = inputVec * speed * Time.deltaTime;
							
							this.rigidbody.AddForce(moveFac);
							
							if(!shouldPlaySound)
							{
								shouldPlaySound = true;
								this.audio.volume = volumeHolder;
								this.audio.Play();
								networkView.RPC ("PropagateIsPlayingSound", RPCMode.Others, shouldPlaySound);
							}
							recievedInput = true;
						}

						if(Input.GetButtonDown("X360Back"))
						{
							GUIManager gui = GameObject.FindGameObjectWithTag("GUIManager").GetComponent<GUIManager>();
							int status = gui.GetMapStatus();
							
							if(status == 0)
							{
								//Go from follow map to non-follow map
								gui.m_isOnFollowMap = false;
							}
							else if(status == 1)
							{
								//Go from non-follow to fullscreen
								gui.m_isOnFollowMap = true;
								gui.ToggleMap();
							}
							else
							{
								//Go from fullscreen to follow
								gui.ToggleMap();
							}
						}
					}



					if((useController && Input.GetButtonDown("X360B")) || (!useController && Input.GetMouseButtonDown (2)))
					{
						GameObject.FindGameObjectWithTag("GUIManager").GetComponent<GUIManager>().RequestBreakLock();
					}

					if(useController)
					{
						//Don't rotate to face cursor, instead, listen for right stick input
						float v = Input.GetAxis("RightStickVertical");
						float h = Input.GetAxis("RightStickHorizontal");

						if(v != 0 || h != 0)
						{
							float angle = (Mathf.Atan2 (v,h) - Mathf.PI/2) * Mathf.Rad2Deg;
							Quaternion target = Quaternion.Euler(new Vector3(0, 0, angle));
							targetAngle = target;
						}

						transform.rotation = Quaternion.Slerp(transform.rotation, targetAngle, GetRotateSpeed() * Time.deltaTime);

						if(Input.GetAxis("X360Triggers") < 0)
							this.GetComponent<PlayerWeaponScript>().PlayerRequestsFire();
						else if(Input.GetAxis ("X360Triggers") == 0)
							this.GetComponent<PlayerWeaponScript>().PlayerReleaseFire();
					}
					else
					{
						//Here, it should rotate to face the mouse cursor
						var objectPos = Camera.main.WorldToScreenPoint(transform.position);
                        var dir = Input.mousePosition - objectPos;

                        RotateTowards(transform.position + dir);

						if(Input.GetMouseButton(0))
						{
							this.GetComponent<PlayerWeaponScript>().PlayerRequestsFire();
						}

						if(Input.GetMouseButtonUp(0))
						{
							this.GetComponent<PlayerWeaponScript>().PlayerReleaseFire();
						}
					}

				//Listen for combat input
				/*if((useController && Input.GetAxis("X360Triggers") < 0) || (!useController && Input.GetMouseButton(0)))
				{
					this.GetComponent<PlayerWeaponScript>().PlayerRequestsFire();
				}

				if((useController && Input.GetAxis("X360Triggers") == 0) || (!useController && Input.GetMouseButtonUp(0)))
					this.GetComponent<PlayerWeaponScript>().PlayerReleaseFire();*/

				}
			}
			//Now finish up by applying vevlocity + momentum
			//this.transform.position += m_currentVelocity;
			//m_currentVelocity *= 0.995f;

			if(!recievedInput)
			{
				if(shouldPlaySound)
				{
					shouldPlaySound = false;
					this.audio.Stop ();
					networkView.RPC ("PropagateIsPlayingSound", RPCMode.Others, false);
				}
			}

			//Finish by checking to make sure we're not too far from 0,0
			float distance = Vector3.Distance(this.transform.position, new Vector3(0, 0, 10));
			if(m_playerIsOoB)
			{
				if(distance < 290)
				{
					//Stop warning screen
					GameObject.FindGameObjectWithTag("GUIManager").GetComponent<GUIManager>().StopOutOfBoundsWarning();
					m_playerIsOoB = false;
				}
			}
			else
			{
				if(distance >= 290)
				{
					//Begin warning screen
					GameObject.FindGameObjectWithTag("GUIManager").GetComponent<GUIManager>().BeginOutOfBoundsWarning();
					m_playerIsOoB = true;
				}
			}
		}
		else
		{

		}
	}

    public override void RotateTowards(Vector3 targetPosition)
    {
        Vector2 targetDirection = targetPosition - transform.position;
        float idealAngle = Mathf.Rad2Deg * (Mathf.Atan2(targetDirection.y, targetDirection.x) - Mathf.PI / 2);
        float currentAngle = transform.rotation.eulerAngles.z;

        if (Mathf.Abs(Mathf.DeltaAngle(idealAngle, currentAngle)) > 5f && true) /// turn to false to use old rotation movement
        {
            float nextAngle = Mathf.MoveTowardsAngle(currentAngle, idealAngle, GetRotateSpeed() * Time.deltaTime);
            transform.rotation = Quaternion.Euler(new Vector3(0.0f, 0.0f, nextAngle));
        }
        else
        {
            Quaternion rotate = Quaternion.LookRotation(targetDirection, Vector3.back);
            rotate.x = 0;
            rotate.y = 0;

            transform.rotation = Quaternion.Slerp(transform.rotation, rotate, GetRotateSpeed() / 50 * Time.deltaTime);
        }
    }

	void MoveToDockPoint (Vector3 moveTo, Vector3 rotateTo)
	{
		float magnitude = moveTo.magnitude;
        float playerSpeedDistance = 200f;
		float dockSpeedDistance = 80f;
		float desiredDockSpeed = 0f;
		
		if (GetCurrentShipSpeed() > m_maxDockingSpeed)
		{
			// Use the players speed
            if (magnitude > playerSpeedDistance)
			{
                desiredDockSpeed = GetCurrentShipSpeed();
			}
			
			// Lerp between the players movement speed and the max docking speed
            else if (magnitude > dockSpeedDistance)
			{
                desiredDockSpeed = Mathf.Lerp(GetCurrentMomentum(), m_maxDockingSpeed, (magnitude - dockSpeedDistance) / (playerSpeedDistance - dockSpeedDistance));
			}
			
			else
			{
				desiredDockSpeed = m_maxDockingSpeed;
			}
		}
		else
		{
            desiredDockSpeed = GetCurrentShipSpeed();
		}

        //Debug.LogError("desiredDockSpeed = " + desiredDockSpeed + " maxShipMomentum = " + GetMaxShipSpeed());

		this.rigidbody.AddForce (moveTo.normalized * desiredDockSpeed * rigidbody.mass * Time.deltaTime);
		
		// Rotate towards point
        Quaternion target = Quaternion.Euler(new Vector3(0, 0, (Mathf.Atan2(rotateTo.y, rotateTo.x) - Mathf.PI / 2) * Mathf.Rad2Deg));
        transform.rotation = Quaternion.Slerp(transform.rotation, target, m_dockRotateSpeed * Time.deltaTime);
	}

	bool m_playerIsOoB = false;

	public void Respawn()
	{
		//this.gameObject.SetActive(true);
		this.renderer.enabled = true;
		this.enabled = true;
		this.GetComponent<HealthScript>().enabled = true;
		this.transform.position = Vector3.zero + new Vector3(0, 0, 10);
		this.transform.rotation = Quaternion.identity;
		this.GetComponent<HealthScript>().ResetHPOnRespawn();
		networkView.RPC ("PropagateRespawn", RPCMode.Others);
	}
	[RPC]
	void PropagateRespawn()
	{
		//this.gameObject.SetActive(true);
		this.renderer.enabled = true;
		this.enabled = true;
		this.GetComponent<HealthScript>().enabled = true;
		this.transform.position = Vector3.zero;
		this.transform.rotation = Quaternion.identity;
		this.GetComponent<HealthScript>().ResetHPOnRespawn();
	}

	public void TellOtherClientsShipHasOwner(NetworkPlayer player)
	{
		networkView.RPC ("SetOwner", RPCMode.Others, player);
		//Also tell GUI to update player blobs for map
		GameObject.FindGameObjectWithTag("GUIManager").GetComponent<GUIManager>().AlertGUIRemotePlayerHasRespawned();
	}
	public void TellPlayerWeAreOwner(NetworkPlayer player)
	{
		GameObject.FindGameObjectWithTag("GUIManager").GetComponent<GUIManager>().thisPlayerHP = this.GetComponent<HealthScript>();
		Camera.main.GetComponent<CameraScript>().InitPlayer(this.gameObject);//.m_currentPlayer = this.gameObject;
		owner = player;
		TellOtherClientsShipHasOwner(player);

		//Ensure weapon is up to date
		//ResetEquippedWeapon();
	}
	[RPC]
	void SetOwner(NetworkPlayer player)
	{
		//this.gameObject.AddComponent<RemotePlayerInterp>();
		//this.GetComponent<RemotePlayerInterp>().localPlayer = this.gameObject;
		owner = player;
	}
	public NetworkPlayer GetOwner()
	{
		return owner;
	}

	public void TellShipStartRecievingInput()
	{
		m_shouldRecieveInput = true;
	}
	public void TellShipStopRecievingInput()
	{
		m_shouldRecieveInput = false;
		//networkView.RPC ("PropagateRecieveInput", RPCMode.Others);
	}
	[RPC]
	void PropagateRecieveInput()
	{
		m_shouldRecieveInput = false;
		this.GetComponent<HealthScript>().m_shouldStop = true;
	}

	public GameObject GetWeaponObject()
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
	GameObject GetShieldObject()
	{
		foreach(Transform child in transform)
		{
			if(child.tag == "ShieldItem")
			{
				return child.gameObject;
			}
		}

		return null;
	}
	GameObject GetEngineObject()
	{
		foreach(Transform child in transform)
		{
			if(child.tag == "Engine")
			{
				return child.gameObject;
			}
		}
		
		return null;
	}
	GameObject GetPlatingObject()
	{
		foreach(Transform child in transform)
		{
			if(child.tag == "Plating")
			{
				return child.gameObject;
			}
		}
		
		return null;
	}

	void OnDestroy()
	{
		if(Network.player == owner)
			Screen.showCursor = true;
	}

	//Do shield fizzle wizzle
	int shaderCounter = 0;
	public void BeginShaderCoroutine(Vector3 position, int type, float magnitude)
	{
		//Debug.Log ("Bullet collision, beginning shader coroutine");
		Vector3 pos = this.transform.InverseTransformPoint(position);
		pos = new Vector3(pos.x * transform.localScale.x, pos.y * transform.localScale.y, pos.z);
		GetShield().renderer.material.SetVector("_ImpactPos" + (shaderCounter + 1).ToString(), new Vector4(pos.x, pos.y, pos.z, 1));
		GetShield().renderer.material.SetFloat("_ImpactTime" + (shaderCounter + 1).ToString(), 1.0f);
		GetShield().renderer.material.SetInt("_ImpactTypes" + (shaderCounter + 1).ToString(), type);
		GetShield().renderer.material.SetFloat("_ImpactMagnitude" + (shaderCounter + 1).ToString(), magnitude);

		StartCoroutine(ReduceShieldEffectOverTime(shaderCounter));
		
		++shaderCounter;
		if(shaderCounter >= 4)
			shaderCounter = 0;
	}
	public void BeginShaderCoroutine(Vector3 position)
	{
		//Debug.Log ("Bullet collision, beginning shader coroutine");
		Vector3 pos = this.transform.InverseTransformPoint(position);
		pos = new Vector3(pos.x * transform.localScale.x, pos.y * transform.localScale.y, pos.z);
		GetShield().renderer.material.SetVector("_ImpactPos" + (shaderCounter + 1).ToString(), new Vector4(pos.x, pos.y, pos.z, 1));
		GetShield().renderer.material.SetFloat("_ImpactTime" + (shaderCounter + 1).ToString(), 1.0f);
		GetShield().renderer.material.SetInt("_ImpactTypes" + (shaderCounter + 1).ToString(), 0);
		GetShield().renderer.material.SetFloat("_ImpactMagnitude" + (shaderCounter + 1).ToString(), 0.0f);

		StartCoroutine(ReduceShieldEffectOverTime(shaderCounter));
		
		++shaderCounter;
		if(shaderCounter >= 4)
			shaderCounter = 0;
	}

	bool coroutineIsRunning = false;
	bool coroutineForceStopped = false;
	IEnumerator ReduceShieldEffectOverTime(int i)
	{
		float t = 0;
		coroutineIsRunning = true;
		//while(t <= 1.0f && coroutineIsRunning)
		while(t <= 1.0f)
		{
			t += Time.deltaTime;
			GameObject shield = GetShield();
			float time = shield.renderer.material.GetFloat("_ImpactTime" + (i + 1).ToString());
			
			//oldImp.w = 1.0f - t;
			
			shield.renderer.material.SetFloat("_ImpactTime" + (i + 1).ToString(), 1.0f - t);
			yield return 0;
		}
		
		/*if(!coroutineIsRunning)
				coroutineForceStopped = true;*/
		
		
		coroutineIsRunning = false;
	}

	GameObject GetShield()
	{
		foreach(Transform child in this.transform)
		{
			if(child.tag == "Shield")
			{
				return child.gameObject;
			}
		}

		Debug.LogWarning ("No shield found for mob " + this.name);
		return null;
	}


	[RPC]
	void PropagateExplosiveForce (float x, float y, float range, float minForce, float maxForce, int mode = (int) ForceMode.Force)
	{
		// Use the players z position to stop the force causing players to move upwards all the time
		Vector3 position = new Vector3 (x, y, transform.position.z);
		rigidbody.AddCustomExplosionForce (position, range, minForce, maxForce, (ForceMode) mode);
	}


	public void ApplyExplosiveForceOverNetwork (float x, float y, float range, float minForce, float maxForce, ForceMode mode = ForceMode.Force)
	{
		networkView.RPC ("PropagateExplosiveForce", RPCMode.Others, x, y, range, minForce, maxForce, (int) mode);
	}

	
	/// <summary>
	/// You probably don't want to use this. This will return the weapon, shield, plating or engine based on what number you pass.
	/// The numbers correspond to how they're displayed in the GUI to the player
	/// </summary>
	public GameObject GetEquipmentFromSlot (int slotNumber)
	{
		switch (slotNumber)
		{
			case 1:
				return m_equippedWeaponItem;
				
			case 2:
				return m_equippedShieldItem;
				
			case 3:
				return m_equippedPlatingItem;
				
			case 4:
				return m_equippedEngineItem;
				
			default:
				return null;
		}
	}

}

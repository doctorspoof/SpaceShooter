﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;


public class CapitalShipScript : Ship
{
	[SerializeField] GameObject m_buildUpExplodeRef;
	[SerializeField] GameObject m_finalExplodeRef;
	[SerializeField] GameObject m_shatteredShip;

    [SerializeField] Transform m_targetPoint;

    [SerializeField] float m_shipSpeed = 15.0f;

    [SerializeField] GameObject[] m_attachedTurretsItemWrappers;

    [SerializeField] bool m_shouldAnchor = false;

    [SerializeField] bool m_shouldStart = false;

    bool temp;

	ItemIDHolder m_itemIDs;

    public List<GameObject> m_cShipInventory;
	List<bool> m_requestedItem = new List<bool>(0);
	bool m_hadItemResponse = false;
	bool m_itemRequestResponse = false;

    int m_bankedCash = 1500;

    int m_currentResourceMass = 500;
    int m_currentResourceWater = 500;
    int m_currentResourceFuel = 500;

    int m_maxResourceMass = 1000;
    int m_maxResourceWater = 1000;
    int m_maxResourceFuel = 1000;

    //GameObject m_buildUpExplo;
    //GameObject m_bigExplo;

    List<GameObject> m_potentialTargets = new List<GameObject>();
    List<GameObject> m_alreadyBeingTargetted = new List<GameObject>();
    int m_searchRadius = 20;

    bool m_updatedTargetListThisFrame = false;

    #region getset

    public void SetTargetPoint(Transform newTarget)
    {
        m_targetPoint = newTarget;
    }

    public bool GetShouldStart()
    {
        return m_shouldStart;
    }

    public void SetShouldStart(bool flag_)
    {
        m_shouldStart = flag_;
    }

    public int GetBankedCash()
    {
        return m_bankedCash;
    }

    public GameObject[] GetAttachedTurrets()
    {
        return m_attachedTurretsItemWrappers;
    }

    public int GetCurrentResourceWater()
    {
        return m_currentResourceWater;
    }

    public int GetMaxResourceWater()
    {
        return m_maxResourceWater;
    }

    public int GetCurrentResourceFuel()
    {
        return m_currentResourceFuel;
    }

    public int GetMaxResourceFuel()
    {
        return m_maxResourceFuel;
    }

    public int GetCurrentResourceMass()
    {
        return m_currentResourceMass;
    }

    public int GetMaxResourceMass()
    {
        return m_maxResourceMass;
    }

    #endregion getset

    protected override void Awake()
    {
        base.Awake();
    }

    void Start()
    {
        GameObject temp = null;
        temp = GameObject.FindGameObjectWithTag("CSTarget");
        if (temp != null)
        {
            m_targetPoint = temp.transform;
        }

        if (m_cShipInventory == null || m_cShipInventory.Count == 0)
        {
            Debug.Log("Telling inventory to initialise");
            m_cShipInventory = new List<GameObject>();
        }
        else
        {
            m_requestedItem = Enumerable.Repeat(false, m_cShipInventory.Count).ToList();
        }

        if (m_shouldAnchor)
        {
            this.rigidbody.isKinematic = true;
        }

        GameObject itemManager = GameObject.FindGameObjectWithTag("ItemManager");
        if (itemManager != null)
        {
            m_itemIDs = itemManager.GetComponent<ItemIDHolder>();
        }
        else
        {
            Debug.LogError("Unable to find ItemManager from CapitalShipScript.");
        }

        if (Network.isServer)
            ResetAttachedTurretsFromWrappers();
    }

    protected override void Update()
    {
        base.Update();
        if (m_shouldStart && m_targetPoint != null)
        {
            var dir = m_targetPoint.position - transform.position;
            Quaternion target = Quaternion.Euler(new Vector3(0, 0, (Mathf.Atan2(dir.y, dir.x) - Mathf.PI / 2) * Mathf.Rad2Deg));
            transform.rotation = Quaternion.Slerp(transform.rotation, target, 2.0f * Time.deltaTime);

            //rigidbody.AddForce(new Vector3(0, 50.0f, 0));
            if (!m_shouldAnchor)
                rigidbody.AddForce(this.transform.up * m_shipSpeed * Time.deltaTime);

            if (!this.audio.isPlaying)
            {
                this.audio.volume = PlayerPrefs.GetFloat("EffectVolume", 1.0f);
                this.audio.Play();
            }
        }

        m_updatedTargetListThisFrame = false;
    }

	public void RequestItemFromServer (GameObject requested)
	{
		/*ItemScript itemScript = requested.GetComponent<ItemScript>();
		int id = itemScript ? itemScript.m_equipmentID : -1;

		if (id != -1 && m_cShipInventory.Contains (requested))
		{
			if (Network.isServer)
			{
				RequestItem (id, new NetworkMessageInfo());
			}

			else
			{
				networkView.RPC ("RequestItem", RPCMode.Server, id);
			}
		}

		// No point querying the server as it doesn't exist
		else
		{
			m_hadItemResponse = true;
			m_itemRequestResponse = false;
		}*/
	}


	public void CancelItemRequestFromServer (GameObject cancellation)
	{
		ItemScript itemScript = cancellation.GetComponent<ItemScript>();
		int id = itemScript ? itemScript.m_equipmentID : -1;

		if (id != -1 && m_cShipInventory.Contains (cancellation))
		{
			if (Network.isServer)
			{
				CancelItem (id);
			}

			else
			{
				networkView.RPC ("CancelItem", RPCMode.Server, id);
			}
		}

		m_hadItemResponse = false;
		m_itemRequestResponse = false;
	}


	[RPC] void CancelItem (int itemID)
	{
		ItemScript itemScript = null;

		for (int i = 0; i < m_cShipInventory.Count; ++i)
		{
			// Get the ItemScript
			itemScript = m_cShipInventory[i].GetComponent<ItemScript>();
			
			// If itemScript is valid, the itemID is the same as requests and the item hasn't been requested before
			if (itemScript && itemScript.m_equipmentID == itemID && m_requestedItem[i])
			{
				m_requestedItem[i] = false;
				break;
			}
		}
	}


	/*[RPC]
	void RequestItem (int itemID, NetworkMessageInfo message)
	{
		bool reply = false;
		ItemScript itemScript = null;

		for (int i = 0; i < m_cShipInventory.Count; ++i)
		{
			// Get the ItemScript
			itemScript = m_cShipInventory[i].GetComponent<ItemScript>();

			// If itemScript is valid, the itemID is the same as requests and the item hasn't been requested before
			if (itemScript && itemScript.m_equipmentID == itemID && !m_requestedItem[i])
			{
				m_requestedItem[i] = true;
				reply = true;
				break;
			}
		}

		// Server requesting
		if (message.sender == (new NetworkPlayer()))
		{
			// Silly unity can't send an RPC from server to itself
			RequestItemReply (reply);
		}

		else
		{
			networkView.RPC ("RequestItemReply", message.sender, reply);
		}
	}*/

	[RPC] void RequestItemReply (bool reply)
	{
		m_hadItemResponse = true;
		m_itemRequestResponse = reply;
	}

	public bool GetRequestResponse (out bool reponse)
	{
		reponse = m_itemRequestResponse;
		return m_hadItemResponse;
	}

    public void RemoveItemFromInventory (GameObject item)
	{
		m_hadItemResponse = false;
		m_itemRequestResponse = false;

		ItemScript itemScript = item.GetComponent<ItemScript>();
		int id = itemScript ? itemScript.m_equipmentID : -1;

		if (id != -1)
		{
			if (Network.isServer)
			{
				AlertServerInventoryRemoval (id);
			}

			else
			{
				networkView.RPC ("AlertServerInventoryRemoval", RPCMode.Server, id);
			}
		}
    }


    public void AddItemToInventory(GameObject item)
	{
		ItemScript itemScript = item.GetComponent<ItemScript>();
		int id = itemScript ? itemScript.m_equipmentID : -1;
		
		if (id != -1)
		{
			if (Network.isServer)
			{
				AlertServerInventoryAddition (id);
			}

			else
			{
				networkView.RPC ("AlertServerInventoryAddition", RPCMode.Server, id);
			}
		}
	}

	[RPC] void AlertServerInventoryRemoval (int itemID)
	{
		// Determine the GameObject and index
		GameObject toRemove = m_itemIDs.GetItemWithID (itemID);
		int index = m_cShipInventory.IndexOf (toRemove);

		// Remove the item
		m_cShipInventory.RemoveAt (index);
		m_requestedItem.RemoveAt (index);

		//Propagate inventory after change
		networkView.RPC("AlertCShipInventoryHasChanged", RPCMode.Others);
		for (int i = 0; i < m_cShipInventory.Count; i++)
		{
			networkView.RPC("PropagateCShipInventory", RPCMode.Others, i, m_cShipInventory[i].GetComponent<ItemScript>().m_equipmentID);
		}
	}
	
	[RPC] void AlertServerInventoryAddition (int itemID)
	{
		// Find the GameObject
		GameObject toAdd = m_itemIDs.GetItemWithID (itemID);

		// Add it to the lists
		m_cShipInventory.Add (toAdd);
		m_requestedItem.Add (false);
		
		//Propagate inventory after change
		networkView.RPC("AlertCShipInventoryHasChanged", RPCMode.Others);
		for (int i = 0; i < m_cShipInventory.Count; i++)
		{
			networkView.RPC("PropagateCShipInventory", RPCMode.Others, i, m_cShipInventory[i].GetComponent<ItemScript>().m_equipmentID);
		}
	}


    [RPC] void AlertCShipInventoryHasChanged()
    {
        //m_cShipInventory = new List<GameObject>();
        m_cShipInventory.Clear();
    }

    [RPC] void PropagateCShipInventory(int position, int itemID)
    {
        GameObject itemToPlace = m_itemIDs.GetItemWithID(itemID);
        Debug.Log ("Requesting that CShip get item: " + itemToPlace.GetComponent<ItemScript>().GetItemName() + " at position " + position);
        m_cShipInventory.Add(itemToPlace);
    }
    
    public bool CShipCanAfford(int amount)
    {
        if (m_bankedCash < amount)
            return false;
        else
            return true;
    }

    public void SpendBankedCash(int amount)
    {
        if (CShipCanAfford(amount))
            m_bankedCash -= amount;

        networkView.RPC("PropagateCShipCash", RPCMode.Others, m_bankedCash);
    }

    public void DepositCashToCShip(int amount)
    {
        m_bankedCash += amount;
        networkView.RPC("PropagateCShipCash", RPCMode.Others, m_bankedCash);

        if ((m_bankedCash - amount) < 500 && m_bankedCash >= 500)
            GameObject.FindGameObjectWithTag("GameController").GetComponent<GameStateController>().AlertMoneyAboveRespawn();
    }

    [RPC] void PropagateCShipCash(int amount)
    {
        m_bankedCash = amount;
    }

    void ResetAttachedTurretsFromWrappers()
    {
        for (int i = 0; i < m_attachedTurretsItemWrappers.Length; i++)
        {
            GameObject tHolder = GetCTurretHolderWithId(i + 1);
            //Debug.Log("Replacing turret at position #" + (i + 1) + " with equipment " + m_attachedTurretsItemWrappers[i].GetComponent<ItemScript>().GetItemName());
            tHolder.GetComponent<CShipTurretHolder>().ReplaceAttachedTurret(m_attachedTurretsItemWrappers[i].GetComponent<ItemScript>().GetEquipmentReference());
        }
    }

    public void TellServerEquipTurret(int turretHolderID, GameObject turret)
    {
		// Ensure the item is correct
		ItemScript script = turret ? turret.GetComponent<ItemScript>() : null;
		int itemID = script ? script.m_equipmentID : -1;

		if (itemID >= 0)
		{
			// Can't send an RPC to the server from the server cause that would be helpful!
			if (Network.isServer)
			{
				ReplaceTurretAtPosition (turretHolderID, itemID);
			}
			else
			{
				networkView.RPC("ReplaceTurretAtPosition", RPCMode.Server, turretHolderID, itemID);
			}
		}
    }

    [RPC] void ReplaceTurretAtPosition(int id, int itemID)
    {
        //We should create a temp to store the previously equipped turret, note id-1 for turretId -> array
		ItemScript turret = m_itemIDs.GetItemWithID (itemID).GetComponent<ItemScript>();

       	ReplaceTurretAtPosition (id, turret);
    }

	void ReplaceTurretAtPosition (int id, ItemScript item)
	{
		if (Network.isServer)
		{
			if (id >= 0 && id <= m_attachedTurretsItemWrappers.Length && item != null)
			{
				//Put the new turret into the item wrapper list
				m_attachedTurretsItemWrappers[id - 1] = item.gameObject;
				
				//Tell the turret holder to spawn the new turret
				GetCTurretHolderWithId(id).GetComponent<CShipTurretHolder>().ReplaceAttachedTurret(item.GetEquipmentReference());
				
				//Propagate equipped items to clients too
				for (int i = 0; i < m_attachedTurretsItemWrappers.Length; i++)
				{
					networkView.RPC("PropagateAttachTurretItemWrappers", RPCMode.Others, i, m_attachedTurretsItemWrappers[i].GetComponent<ItemScript>().m_equipmentID);
				}
			}
			else
			{
				Debug.LogError ("Unable to equip " + item + " at ID #" + id + " on " + name);
			}
		}
		else
		{
			Debug.LogError ("A client attempted to call " + name + ".CapitalShipScript.ReplaceTurretPosition()");
		}
			
	}

    [RPC] void PropagateAttachTurretItemWrappers(int position, int turretID)
    {
        GameObject item = m_itemIDs.GetItemWithID(turretID);
        m_attachedTurretsItemWrappers[position] = item;
    }    

    void OnSerializeNetworkView(BitStream stream, NetworkMessageInfo info)
    {
        //If the CShip is anchored, there is no reason to send messages about it's movements
        if (!m_shouldAnchor)
        {
            float posX = this.transform.position.x;
            float posY = this.transform.position.y;

            float rotZ = this.transform.rotation.eulerAngles.z;

            Vector3 velocity = rigidbody.velocity;

            if (stream.isWriting)
            {
                //We're the owner, send our info to other people
                stream.Serialize(ref posX);
                stream.Serialize(ref posY);
                stream.Serialize(ref rotZ);
                stream.Serialize(ref velocity);
            }
            else
            {
                //Recieve data!
                stream.Serialize(ref posX);
                stream.Serialize(ref posY);
                stream.Serialize(ref rotZ);
                stream.Serialize(ref velocity);

                this.transform.position = new Vector3(posX, posY, 10.5f);
                this.transform.rotation = Quaternion.Euler(0, 0, rotZ);
                rigidbody.velocity = velocity;
            }
        }
    }

    /* Water funcs */
    

    public void ReduceResourceWater(int amount)
    {
        m_currentResourceWater -= amount;
        if (m_currentResourceWater < 0)
            m_currentResourceWater = 0;

        PropagateResourceLevels();
    }
    public void IncreaseResourceWater(int amount)
    {
        m_currentResourceWater += amount;
        if (m_currentResourceWater > m_maxResourceWater)
            m_currentResourceWater = m_maxResourceWater;

        PropagateResourceLevels();
    }

    public bool HasEnoughResourceWater(int amount)
    {
        if (m_currentResourceWater >= amount)
            return true;
        else
            return false;
    }

    public void ReduceMaxResourceWater(int amount)
    {
        m_maxResourceWater -= amount;
        if (m_currentResourceWater > m_maxResourceWater)
        {
            m_currentResourceWater = m_maxResourceWater;
        }
        if (m_maxResourceWater < 0)
        {
            //TODO: Bad things for players
            m_maxResourceWater = 0;
        }

        PropagateResourceLevels();
    }

    public void IncreaseMaxResourceWater(int amount)
    {
        m_maxResourceWater += amount;

        PropagateResourceLevels();
    }

    /* Fuel funcs */
    

    public void ReduceResourceFuel(int amount)
    {
        m_currentResourceFuel -= amount;
        if (m_currentResourceFuel < 0)
            m_currentResourceFuel = 0;

        PropagateResourceLevels();
    }
    public void IncreaseResourceFuel(int amount)
    {
        m_currentResourceFuel += amount;
        if (m_currentResourceFuel > m_maxResourceFuel)
            m_currentResourceFuel = m_maxResourceFuel;

        PropagateResourceLevels();
    }

    public bool HasEnoughResourceFuel(int amount)
    {
        if (m_currentResourceFuel >= amount)
            return true;
        else
            return false;
    }

    public void ReduceMaxResourceFuel(int amount)
    {
        m_maxResourceFuel -= amount;
        if (m_currentResourceFuel > m_maxResourceFuel)
        {
            m_currentResourceFuel = m_maxResourceFuel;
        }
        if (m_maxResourceFuel < 0)
        {
            //TODO: Bad things for players
            m_maxResourceFuel = 0;
        }

        PropagateResourceLevels();
    }
    public void IncreaseMaxResourceFuel(int amount)
    {
        m_maxResourceFuel += amount;

        PropagateResourceLevels();
    }

    /* Mass funcs */
    

    public void ReduceResourceMass(int amount)
    {
        m_currentResourceMass -= amount;
        if (m_currentResourceMass < 0)
            m_currentResourceMass = 0;

        PropagateResourceLevels();
    }
    public void IncreaseResourceMass(int amount)
    {
        m_currentResourceMass += amount;
        if (m_currentResourceMass > m_maxResourceMass)
            m_currentResourceMass = m_maxResourceMass;

        PropagateResourceLevels();
    }

    public bool HasEnoughResourceMass(int amount)
    {
        if (m_currentResourceMass >= amount)
            return true;
        else
            return false;
    }

    public void ReduceMaxResourceMass(int amount)
    {
        m_maxResourceMass -= amount;
        if (m_currentResourceMass > m_maxResourceMass)
        {
            m_currentResourceMass = m_maxResourceMass;
        }
        if (m_maxResourceMass < 0)
        {
            //TODO: Bad things for players
            m_maxResourceMass = 0;
        }

        PropagateResourceLevels();
    }

    public void IncreaseMaxResourceMass(int amount)
    {
        m_maxResourceMass += amount;
        PropagateResourceLevels();
    }

    void PropagateResourceLevels()
    {
        networkView.RPC("ReceiveResourceLevels", RPCMode.Others, m_currentResourceWater, m_currentResourceFuel, m_currentResourceMass, m_maxResourceWater, m_maxResourceFuel, m_maxResourceMass);
    }

    [RPC] void ReceiveResourceLevels(int water, int fuel, int mass, int waterMax, int fuelMax, int massMax)
    {
        m_currentResourceWater = water;
        m_currentResourceFuel = fuel;
        m_currentResourceMass = mass;

        m_maxResourceWater = waterMax;
        m_maxResourceFuel = fuelMax;
        m_maxResourceMass = massMax;
    }

	public void BeginDeathBuildUpAnim()
	{
		GameObject explodeObj1 = (GameObject)Instantiate(m_buildUpExplodeRef, this.transform.position + new Vector3(0, 0, -1.0f), this.transform.rotation);
		explodeObj1.transform.parent = this.transform;
		//m_buildUpExplo = explodeObj1;
	}

	public void BeginDeathFinalAnim()
	{
		/*GameObject explodeObj2 = (GameObject)*/Instantiate(m_finalExplodeRef, this.transform.position + new Vector3(0, 0, -1.5f), this.transform.rotation);
		//m_bigExplo = explodeObj2;

		//Begin a timer here, and then split the cship into fragments
		StartCoroutine(SplitCShipDelay());
	}

	IEnumerator SplitCShipDelay()
	{
		float t = 0;

		while(t < 2.75f)
		{
			t += Time.deltaTime;
			yield return 0;
		}

		//Spawn shattered bits
		/*GameObject ship = (GameObject)*/Instantiate(m_shatteredShip, this.transform.position, this.transform.rotation);

		//Destroy self
		Destroy (this.gameObject);
	}

    GameObject GetCTurretWithID(int id)
    {
        GameObject holder = GetCTurretHolderWithId(id);
        if (holder != null)
        {
            return holder.GetComponent<CShipTurretHolder>().GetAttachedTurret();
        }
        else
        {
            Debug.LogWarning("Couldn't find CShipTurret with id #" + id);
            return null;
        }
    }

    public GameObject GetCTurretHolderWithId(int id)
    {
        foreach (Transform child in transform)
        {
            if (child.tag == "CTurretHolder" && child.GetComponent<CShipTurretHolder>().GetShipTurretID() == id)
            {
                return child.gameObject;
            }
        }

        Debug.LogWarning("Couldn't find CShipHolder with id #" + id);
        return null;
    }

    public GameObject GetGlowForTurretByID(int id)
    {
        foreach (Transform child in transform)
        {
            if (child.tag == "CShipToggleGlow")
            {
                if (child.GetComponent<CapitalShipGlowScript>().GetGlowID() == id)
                    return child.gameObject;
            }
        }

        return null;
    }

    

    public List<GameObject> RequestTargets(int layerMask)//Vector2 position, int layer)
    {
        UpdateTargetLists(layerMask);

        //GameObject closestTarget = null;
        //float distanceSqr = 0;
        //foreach(GameObject obj in potentialTargets)
        //{
        //    if(closestTarget == null || Vector2.SqrMagnitude(obj.transform.position - transform.position) < distanceSqr && ((1 << obj.layer) & layer) != 0 )
        //    {
        //        distanceSqr = Vector2.SqrMagnitude(obj.transform.position - transform.position);
        //        closestTarget = obj;
        //    }
        //}

        //potentialTargets.Remove(closestTarget);
        //alreadyBeingTargetted.Add(closestTarget);

        return m_potentialTargets;
    }

    public void ClaimTarget(GameObject obj)
    {
        m_potentialTargets.Remove(obj);
        m_alreadyBeingTargetted.Add(obj);
    }

    public void UnclaimTarget(GameObject obj)
    {
        m_alreadyBeingTargetted.Remove(obj);
    }

    public void UpdateTargetLists(int layerMask)
    {
        if (m_updatedTargetListThisFrame)
            return;

        m_updatedTargetListThisFrame = true;

        m_potentialTargets.Clear();

        GameObject[] objects = Physics.OverlapSphere(transform.position, m_searchRadius, layerMask).GetAttachedRigidbodies().GetUniqueOnly().GetGameObjects();

        if(objects == null)
        {
            return;
        }

        m_potentialTargets.AddRange(objects);

        for (int i = m_alreadyBeingTargetted.Count - 1; i >= 0; -- i )
        {
            if (m_alreadyBeingTargetted[i] == null || Vector2.SqrMagnitude(m_alreadyBeingTargetted[i].transform.position - transform.position) > Mathf.Pow(m_searchRadius, 2))
            {
                m_alreadyBeingTargetted.RemoveAt(i);
            }
            else
            {
                m_potentialTargets.Remove(m_alreadyBeingTargetted[i]);
            }
        }
    }
}
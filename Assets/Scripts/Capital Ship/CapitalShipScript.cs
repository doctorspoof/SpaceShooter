using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;



/// <summary>
/// The Resources class is effectively a struct of the three different resources in the game. This allows us to group the resources together
/// into a single object.
/// </summary>
[System.Serializable] public sealed class Resources
{
    [Range (0, 100000)] public int mass = 500;  //!< The amount of mass available.
    [Range (0, 100000)] public int water = 500; //!< The amount of water available.
    [Range (0, 100000)] public int fuel = 500;  //!< The amount of fuel available.

}



/// <summary>
/// The primary class used to control the players Capital Ship. This is used to manage the resources available in the ship, the cash available,
/// the movement of the capital ship and it maintains a list of potential targets for weapons to target.
/// </summary>
public sealed class CapitalShipScript : Ship
{
    #region Unity modifiable values
    
    // Movement
    [SerializeField]                    bool m_shouldAnchor = false;                        //!< Indicates whether the CShip should stay stationary or not.

    // Cash
    [SerializeField, Range (0, 10000)]  int m_bankedCash = 1500;                            //!< The amount of cash available to the CShip.
    
    // Resources
    [SerializeField]                    Resources m_currentResources = new Resources();     //!< The current amount of resources available.
    [SerializeField]                    Resources m_maxResources = new Resources();         //!< The limit caps for resources in the game.

    // Targetting
    [SerializeField, Range (0f, 1000f)] float m_searchRadius = 20f;                         //!< How far away enemies can be in the targetting list.

    // Explosion effect
	[SerializeField]                    GameObject m_buildUpExplodeRef = null;              //!< A reference to the prefab used when the CShip starts blowing up.
	[SerializeField]                    GameObject m_finalExplodeRef = null;                //!< A reference to the prefab used when the CShip performs its final explosion.
	[SerializeField]                    GameObject m_shatteredShip = null;                  //!< The broken version of the CShip which is displayed upon explosion.

    // Others
    [SerializeField]                    Transform m_targetPoint = null;                     //!< Points to where the CShip needs to travel to.
    [SerializeField]                    ItemScript[] m_attachedTurretsItemWrappers = null;  //!< Contains references to the item wrapper of each turret on the CShip.

    #endregion


    #region Internal data

    bool m_shouldMoveToTarget = false;                                  //!< Indicates whether the CShip should start moving towards the target point or not.
    bool m_updatedTargetListThisFrame = false;                          //!< Makes sure that the target list only gets updated once in case two turrets request a target on the same frame.

    NetworkInventory m_inventory;                                       //!< Stored in case another class wants to get a copy of the inventory instead of calling .GetComponent<T>()

    List<GameObject> m_potentialTargets = new List<GameObject>();       //!< A list of targetable enemies for turrets to choose from, this stops turrets targetting the same enemy.
    List<GameObject> m_alreadyBeingTargetted = new List<GameObject>();  //!< A list of enemies already being targetted, again used to prevent enemies being targeted by the same turret.

    #endregion


    #region External references
    
    ItemIDHolder m_itemIDs = null;  //!< An external reference to the ItemIDHolder used to obtain turret ItemScripts.

    #endregion


    #region Getters & setters

    public void SetTargetPoint(Transform newTarget)
    {
        m_targetPoint = newTarget;
    }

    public bool GetShouldStart()
    {
        return m_shouldMoveToTarget;
    }

    public void SetShouldStart(bool flag_)
    {
        m_shouldMoveToTarget = flag_;
    }

    public int GetBankedCash()
    {
        return m_bankedCash;
    }

    public ItemScript[] GetAttachedTurrets()
    {
        return m_attachedTurretsItemWrappers;
    }

    public int GetCurrentResourceWater()
    {
        return m_currentResources.water;
    }

    public int GetMaxResourceWater()
    {
        return m_maxResources.water;
    }

    public int GetCurrentResourceFuel()
    {
        return m_currentResources.fuel;
    }

    public int GetMaxResourceFuel()
    {
        return m_maxResources.fuel;
    }

    public int GetCurrentResourceMass()
    {
        return m_currentResources.mass;
    }

    public int GetMaxResourceMass()
    {
        return m_maxResources.mass;
    }

    #endregion Getters & setters


    #region Behavior functions

    /// <summary>
    /// Calls Ship.Awake() and determines whether the rigidbody should be kinematic or not.
    /// </summary>
    protected override void Awake()
    {
        base.Awake();

        // Stop all natural rigidbody movement if the ship should be anchored
        rigidbody.isKinematic = m_shouldAnchor;
    }


    /// <summary>
    /// Start performs the setup for external references and resets the attached turrets.
    /// </summary>
    void Start()
    {
        // Set up the external reference to the target point
        GameObject temp = GameObject.FindGameObjectWithTag("CSTarget");
        if (temp != null)
        {
            m_targetPoint = temp.transform;
        }

        // Set up the external reference to the ItemIDHolder
        GameObject itemManager = GameObject.FindGameObjectWithTag("ItemManager");
        if (itemManager != null)
        {
            m_itemIDs = itemManager.GetComponent<ItemIDHolder>();
        }
        else
        {
            Debug.LogError ("Unable to find ItemManager from CapitalShipScript.");
        }

        if (Network.isServer)
        {
            ResetAttachedTurretsFromWrappers();
        }
    }


    /// <summary>
    /// Moves the ship if necessary
    /// </summary>
    void FixedUpdate()
    {
        if (m_shouldMoveToTarget && m_targetPoint != null)
        {
            var dir = m_targetPoint.position - transform.position;
            Quaternion target = Quaternion.Euler(new Vector3(0, 0, (Mathf.Atan2(dir.y, dir.x) - Mathf.PI / 2) * Mathf.Rad2Deg));
            transform.rotation = Quaternion.Slerp(transform.rotation, target, 2.0f * Time.deltaTime);

            if (!m_shouldAnchor)
                rigidbody.AddForce(this.transform.up * GetCurrentMomentum() * Time.deltaTime);

            if (!this.audio.isPlaying)
            {
                this.audio.volume = PlayerPrefs.GetFloat("EffectVolume", 1.0f);
                this.audio.Play();
            }
        }

        m_updatedTargetListThisFrame = false;
    }   


    /// <summary>
    /// Restricts the network serialisation to position.x & y, rotation.eulerAngles.z and rigidbody.velocity.
    /// </summary>
    /// <param name="stream">The BitStream provided by Unity.</param>
    /// <param name="info">Gives access to information as to where the packet came from.</param>
    void OnSerializeNetworkView(BitStream stream, NetworkMessageInfo info)
    {
        // If the CShip is anchored, there is no reason to send messages about it's movements
        if (!m_shouldAnchor)
        {
            float posX = this.transform.position.x;
            float posY = this.transform.position.y;
            
            float rotZ = this.transform.rotation.eulerAngles.z;
            
            Vector3 velocity = rigidbody.velocity;
            
            if (stream.isWriting)
            {
                // We're the owner, send our info to other people
                stream.Serialize(ref posX);
                stream.Serialize(ref posY);
                stream.Serialize(ref rotZ);
                stream.Serialize(ref velocity);
            }
            else
            {
                // Recieve data!
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

    #endregion Behavior functions


    #region Turret functionality

    /// <summary>
    /// Tells the server to equip a CShip turret and then propagates the change to the other clients on the network.
    /// </summary>
    /// <param name="turretHolderID">The slot to place the new turret at.</param>
    /// <param name="turret">The turret object to attach to the CShip.</param>
    public void TellServerEquipTurret(int turretHolderID, GameObject turret)
    {
        // Ensure the item is correct
        ItemScript script = turret ? turret.GetComponent<ItemScript>() : null;
        int itemID = script ? script.m_equipmentID : -1;
        
        if (itemID >= 0 && turretHolderID >= 0)
        {
            // Can't send an RPC to the server from the server cause that would be helpful!
            if (Network.isServer)
            {
                ReplaceTurretAtPosition (turretHolderID, script);
            }
            else
            {
                networkView.RPC("ReplaceTurretAtPosition", RPCMode.Server, turretHolderID, itemID);
            }
        }
    }


    /// <summary>
    /// Replaces the turret at the indicated position. This RPC is primarily used to be called on the server
    /// </summary>
    /// <param name="id">The slot to place the new turret into.</param>
    /// <param name="itemID">The ID number of the turret ItemScript so that it can be recreated.</param>
    [RPC] void ReplaceTurretAtPosition(int id, int itemID)
    {
        //We should create a temp to store the previously equipped turret, note id-1 for turretId -> array
        ItemScript turret = m_itemIDs.GetItemWithID (itemID).GetComponent<ItemScript>();
        
        ReplaceTurretAtPosition (id, turret);
    }


    /// <summary>
    /// Replaces the turret at the indicated position. This overload provides a way for the server to skip the reconstruction of the item.
    /// </summary>
    /// <param name="id">The slot to place the new turret into.</param>
    /// <param name="item">The reconstructed ItemScript of the turret to be placed at the desired position.</param>
    void ReplaceTurretAtPosition (int id, ItemScript item)
    {
        if (Network.isServer)
        {
            if (id >= 0 && id < m_attachedTurretsItemWrappers.Length && item != null)
            {
                //Put the new turret into the item wrapper list
                m_attachedTurretsItemWrappers[id] = item;
                
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
    
    
    void ResetAttachedTurretsFromWrappers()
    {
        for (int i = 0; i < m_attachedTurretsItemWrappers.Length; i++)
        {
            GameObject tHolder = GetCTurretHolderWithId(i + 1);
            //Debug.Log("Replacing turret at position #" + (i + 1) + " with equipment " + m_attachedTurretsItemWrappers[i].GetComponent<ItemScript>().GetItemName());
            tHolder.GetComponent<CShipTurretHolder>().ReplaceAttachedTurret(m_attachedTurretsItemWrappers[i].GetComponent<ItemScript>().GetEquipmentReference());
        }
    }
    
    [RPC] void PropagateAttachTurretItemWrappers(int position, int turretID)
    {
        GameObject item = m_itemIDs.GetItemWithID(turretID);
        m_attachedTurretsItemWrappers[position] = item;
    }

    #endregion Turret functionality


    #region Cash functions
    
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

    #endregion Cash functions


    #region Resource functions
    
    /// <summary>
    /// Determines whether this instance has enough water for the specified amount.
    /// </summary>
    /// <returns><c>true</c> if this instance has enough water; otherwise, <c>false</c>.</returns>
    /// <param name="amount">The amount of water to check for.</param>
    public bool HasEnoughResourceWater (int amount)
    {
        return m_currentResources.water >= amount;
    }


    /// <summary>
    /// Determines whether this instance has enough fuel for the specified amount.
    /// </summary>
    /// <returns><c>true</c> if this instance has enough fuel; otherwise, <c>false</c>.</returns>
    /// <param name="amount">The amount of fuel to check for.</param>
    public bool HasEnoughResourceFuel (int amount)
    {
        return m_currentResources.fuel >= amount;
    }


    /// <summary>
    /// Determines whether this instance has enough mass for the specified amount.
    /// </summary>
    /// <returns><c>true</c> if this instance has enough mass; otherwise, <c>false</c>.</returns>
    /// <param name="amount">The amount of mass to check for.</param>
    public bool HasEnoughResourceMass (int amount)
    {
        return m_currentResources.mass >= amount;
    }


    /// <summary>
    /// Allows for the incrementing/decrementing of the available water resource.
    /// </summary>
    /// <param name="amount">How much to increment by (negatives values decrement).</param>
    public void AlterCurrentResourceWater (int amount)
    {
        m_currentResources.water = Mathf.Clamp (m_currentResources.water + amount, 0, m_maxResources.water);

        PropagateResourceLevels();
    }
    
    
    /// <summary>
    /// Allows for the incrementing/decrementing of the maximum water resource.
    /// </summary>
    /// <param name="amount">How much to increment by (negatives values decrement).</param>
    public void AlterMaxResourceWater (int amount)
    {
        m_maxResources.water = Mathf.Max (0, m_maxResources.water + amount);

        if (m_maxResources.water < m_currentResources.water)
        {
            m_currentResources.water = m_maxResources.water;
        }
        
        PropagateResourceLevels();
    }


    /// <summary>
    /// Allows for the incrementing/decrementing of the available fuel resource.
    /// </summary>
    /// <param name="amount">How much to increment by (negatives values decrement).</param>
    public void AlterCurrentResourceFuel (int amount)
    {
        m_currentResources.fuel = Mathf.Clamp (m_currentResources.fuel + amount, 0, m_maxResources.fuel);
        
        PropagateResourceLevels();
    }


    /// <summary>
    /// Allows for the incrementing/decrementing of the maximum fuel resource.
    /// </summary>
    /// <param name="amount">How much to increment by (negatives values decrement).</param>
    public void AlterMaxResourceFuel (int amount)
    {
        m_maxResources.fuel = Mathf.Max (0, m_maxResources.fuel + amount);

        if (m_maxResources.fuel < m_currentResources.fuel)
        {
            m_currentResources.fuel = m_maxResources.fuel;
        }
        
        PropagateResourceLevels();
    }


    /// <summary>
    /// Allows for the incrementing/decrementing of the available mass resource.
    /// </summary>
    /// <param name="amount">How much to increment by (negatives values decrement).</param>
    public void AlterCurrentResourceMass (int amount)
    {
        m_currentResources.mass = Mathf.Clamp (m_currentResources.mass + amount, 0, m_maxResources.mass);
        
        PropagateResourceLevels();
    }


    /// <summary>
    /// Allows for the incrementing/decrementing of the maximum mass resource.
    /// </summary>
    /// <param name="amount">How much to increment by (negatives values decrement).</param>
    public void AlterMaxResourceMass (int amount)
    {
        m_maxResources.mass = Mathf.Max (0, m_maxResources.mass + amount);

        if (m_maxResources.mass < m_currentResources.mass)
        {
            m_currentResources.mass = m_maxResources.mass;
        }
        
        PropagateResourceLevels();
    }


    /// <summary>
    /// Propagates the resource levels to all other clients on the network.
    /// </summary>
    void PropagateResourceLevels()
    {
        networkView.RPC ("ReceiveResourceLevels", RPCMode.Others, m_currentResources.water, m_currentResources.fuel, m_currentResources.mass, m_maxResources.water, m_maxResources.fuel, m_maxResources.mass);
    }


    /// <summary>
    /// The RPC received when resources are propagated over the network. The resource objects have their values set to the value of the parameters.
    /// </summary>
    /// <param name="water">The value for m_currentResources.water.</param>
    /// <param name="fuel">The value for m_currentResources.fuel.</param>
    /// <param name="mass">The value for m_currentResources.mass.</param>
    /// <param name="waterMax">The value for m_maxResources.water.</param>
    /// <param name="fuelMax">The value for m_maxResources.fuel.</param>
    /// <param name="massMax">The value for m_maxResources.mass.</param>
    [RPC] void ReceiveResourceLevels(int water, int fuel, int mass, int waterMax, int fuelMax, int massMax)
    {
        m_currentResources.water = water;
        m_currentResources.fuel = fuel;
        m_currentResources.mass = mass;

        m_maxResources.water = waterMax;
        m_maxResources.fuel = fuelMax;
        m_maxResources.mass = massMax;
    }

    #endregion Resource functions


    #region Death animation


    /// <summary>
    /// Starts the explosion animation which happens on the CShip before it finally splits.
    /// </summary>
	public void BeginDeathBuildUpAnim()
	{
		GameObject explodeObject = (GameObject) Instantiate (m_buildUpExplodeRef, this.transform.position + new Vector3 (0, 0, -1.0f), this.transform.rotation);
		
        // Place the build up explosion inside the CShip heirarchy.
        explodeObject.transform.parent = this.transform;
	}


    /// <summary>
    /// Causes the CShip to perform its final explosion sequence which will also invoke the death of the ship.
    /// </summary>
	public void BeginDeathFinalAnim()
	{
		Instantiate (m_finalExplodeRef, this.transform.position + new Vector3 (0, 0, -1.5f), this.transform.rotation);
		
		//Begin a timer here, and then split the cship into fragments
		Invoke ("SpawnShatteredShip", 2.75f);
	}


    /// <summary>
    /// Spawns the shattered ship and then destroys the living CShip.
    /// </summary>
    void SpawnShatteredShip()
    {
        // Spawn shattered bits
        Instantiate (m_shatteredShip, this.transform.position, this.transform.rotation);
        
        // Destroy self
        Destroy (this.gameObject);
    }

    #endregion Death animation


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

    

    public List<GameObject> RequestTargets(int layerMask)
    {
        UpdateTargetLists(layerMask);

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
        if (!m_updatedTargetListThisFrame)
        {
            m_updatedTargetListThisFrame = true;

            m_potentialTargets.Clear();

            GameObject[] objects = Physics.OverlapSphere(transform.position, m_searchRadius, layerMask).GetAttachedRigidbodies().GetUniqueOnly().GetGameObjects();

            if(objects != null)
            {
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
    }
}

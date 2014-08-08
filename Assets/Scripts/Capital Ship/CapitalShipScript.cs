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
    [SerializeField]                    ItemWrapper[] m_attachedTurretsItemWrappers = null;  //!< Contains references to the item wrapper of each turret on the CShip.

    #endregion


    #region Internal data

    bool m_shouldMoveToTarget = false;                                  //!< Indicates whether the CShip should start moving towards the target point or not.
    bool m_updatedTargetListThisFrame = false;                          //!< Makes sure that the target list only gets updated once in case two turrets request a target on the same frame.

    NetworkInventory m_inventory;                                       //!< Stored in case another class wants to get a copy of the inventory instead of calling .GetComponent<T>()

    CShipTurretHolder[] m_turretHolders = null;                         //!< A cached copy of each in use turret on the CShip.
    CapitalShipGlowScript[] m_turretGlows = null;                       //!< A cached copy of each glow location on the CShip.

    List<GameObject> m_potentialTargets = new List<GameObject>();       //!< A list of targetable enemies for turrets to choose from, this stops turrets targetting the same enemy.
    List<GameObject> m_alreadyBeingTargetted = new List<GameObject>();  //!< A list of enemies already being targetted, again used to prevent enemies being targeted by the same turret.

    #endregion


    #region External references
    
    ItemIDHolder m_itemIDs = null;  //!< An external reference to the ItemIDHolder used to obtain turret ItemScripts.

    #endregion


    #region Getters & setters

    public void SetTargetPoint (Transform newTarget)
    {
        m_targetPoint = newTarget;
    }


    public bool GetShouldMove()
    {
        return m_shouldMoveToTarget;
    }


    public void SetShouldMove (bool flag_)
    {
        m_shouldMoveToTarget = flag_;
    }


    public int GetBankedCash()
    {
        return m_bankedCash;
    }

    public ItemWrapper[] GetAttachedTurretItemWrappers()
    {
        return m_attachedTurretsItemWrappers;
    }


    public int GetCurrentResourceWater()
    {
        return m_currentResources.water;
    }
    
    
    public int GetCurrentResourceFuel()
    {
        return m_currentResources.fuel;
    }
    
    
    public int GetCurrentResourceMass()
    {
        return m_currentResources.mass;
    }


    public int GetMaxResourceWater()
    {
        return m_maxResources.water;
    }


    public int GetMaxResourceFuel()
    {
        return m_maxResources.fuel;
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

        RefreshTurretCache();
    }

    /// <summary>
    /// Start performs the setup for external references and resets the attached turrets.
    /// </summary>
    protected override void Start()
    {
        // Set up the external reference to the target point
        GameObject temp = GameObject.FindGameObjectWithTag ("CSTarget");
        if (temp != null)
        {
            m_targetPoint = temp.transform;
        }

        // Set up the external reference to the ItemIDHolder
        GameObject itemManager = GameObject.FindGameObjectWithTag ("ItemManager");
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
            // Rotate to the correct direction
            Vector3 dir = m_targetPoint.position - transform.position;
            Quaternion target = Quaternion.Euler (new Vector3 (0, 0, (Mathf.Atan2 (dir.y, dir.x) - Mathf.PI / 2) * Mathf.Rad2Deg));
            transform.rotation = Quaternion.Slerp (transform.rotation, target, 2.0f * Time.deltaTime);

            // Move forwards if necessarty
            if (!m_shouldAnchor)
            {
                rigidbody.AddForce (this.transform.up * GetCurrentMomentum() * Time.deltaTime);
            }
                
            // Play any audio if necessary
            if (!this.audio.isPlaying)
            {
                this.audio.volume = PlayerPrefs.GetFloat ("EffectVolume", 1.0f);
                this.audio.Play();
            }
        }

        // Since enemies will only move in Update() we might as well only allow the target list to be updated every FixedUpdate()
        m_updatedTargetListThisFrame = false;
    }


    /// <summary>
    /// Restricts the network serialisation to position.x & y, rotation.eulerAngles.z and rigidbody.velocity.
    /// </summary>
    /// <param name="stream">The BitStream provided by Unity.</param>
    /// <param name="info">Gives access to information as to where the packet came from.</param>
    void OnSerializeNetworkView (BitStream stream, NetworkMessageInfo info)
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
                stream.Serialize (ref posX);
                stream.Serialize (ref posY);
                stream.Serialize (ref rotZ);
                stream.Serialize (ref velocity);
            }

            else
            {
                // Recieve data!
                stream.Serialize (ref posX);
                stream.Serialize (ref posY);
                stream.Serialize (ref rotZ);
                stream.Serialize (ref velocity);
                
                this.transform.position = new Vector3 (posX, posY, 10.5f);
                this.transform.rotation = Quaternion.Euler (0, 0, rotZ);
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
    public void TellServerEquipTurret (int turretSlot, ItemWrapper turret)
    {
        // Ensure the item is correct
        int itemID = turret ? turret.GetItemID() : -1;
        
        if (turretSlot >= 0 && turretSlot < m_attachedTurretsItemWrappers.Length &&
            itemID >= 0 && turret.GetItemType() == ItemType.CapitalWeapon)
        {
            // Can't send an RPC to the server from the server cause that would be helpful!
            if (Network.isServer)
            {
                ReplaceTurretAtPosition (turretSlot, turret);
            }

            else
            {
                networkView.RPC ("ReplaceTurretAtPosition", RPCMode.Server, turretSlot, itemID);
            }
        }
    }


    /// <summary>
    /// Replaces the turret at the indicated position. This RPC is primarily used to be called on the server
    /// </summary>
    /// <param name="id">The slot to place the new turret into.</param>
    /// <param name="itemID">The ID number of the turret ItemScript so that it can be recreated.</param>
    [RPC] void ReplaceTurretAtPosition (int id, int itemID)
    {
        // We should create a temp to store the previously equipped turret
        ReplaceTurretAtPosition (id, m_itemIDs.GetItemWithID (itemID));
    }


    /// <summary>
    /// Replaces the turret at the indicated position. This overload provides a way for the server to skip the reconstruction of the item.
    /// </summary>
    /// <param name="id">The slot to place the new turret into.</param>
    /// <param name="item">The reconstructed ItemScript of the turret to be placed at the desired position.</param>
    void ReplaceTurretAtPosition (int id, ItemWrapper item)
    {
        if (Network.isServer)
        {
            if (id >= 0 && id < m_attachedTurretsItemWrappers.Length && item != null)
            {
                // Put the new turret into the item wrapper list
                m_attachedTurretsItemWrappers[id] = item;
                
                // Tell the turret holder to spawn the new turret
                GetCTurretHolderWithID (id).GetComponent<CShipTurretHolder>().ReplaceAttachedTurret (item.GetItemPrefab());
                
                // Propagate equipped items to clients too
                for (int i = 0; i < m_attachedTurretsItemWrappers.Length; i++)
                {
                    networkView.RPC ("PropagateAttachTurretItemWrappers", RPCMode.Others, i, m_attachedTurretsItemWrappers[i].GetItemID());
                }

                // Ensure the cache is up to date
                RefreshTurretCache();
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
    

    /// <summary>
    /// Resets the attached turrets from the stored item wrappers.
    /// </summary>
    void ResetAttachedTurretsFromWrappers()
    {
        // Avoid excessive variable creation
        CShipTurretHolder tHolder = null;

        // Force a reset of each turret on the CShip
        for (int i = 0; i < m_attachedTurretsItemWrappers.Length; i++)
        {
            if ((tHolder = GetCTurretHolderWithID (i)) != null)
            {
                tHolder.ReplaceAttachedTurret (m_attachedTurretsItemWrappers[i].GetItemPrefab());
            }
        }
    }


    /// <summary>
    /// An RPC used to propagate an item wrapper at a particular position of the attached turrets array.
    /// </summary>
    /// <param name="position">The position/index of the array where the item wrapper should be placed.</param>
    /// <param name="turretID">The itemID of the turret so that the clients can recreate the correct object.</param>
    [RPC] void PropagateAttachTurretItemWrappers (int position, int turretID)
    {
        m_attachedTurretsItemWrappers[position] = m_itemIDs.GetItemWithID(turretID);
    }
    

    /// <summary>
    /// A simple function which returns the actual turret GameObject with the corresponding ID.
    /// </summary>
    /// <returns>The desired turret GameObject, null if none exists.</returns>
    /// <param name="id">The ID to search for.</param>
    GameObject GetCTurretWithID (int id)
    {
        // Find the holder
        CShipTurretHolder holder = GetCTurretHolderWithID (id);

        // Return the attached holder if possible
        if (holder != null)
        {
            return holder.GetAttachedTurret();
        }

        // Otherwise return null and present a warning
        Debug.LogWarning ("Couldn't find CShipTurret with id #" + id);
        return null;
    }


    /// <summary>
    /// Gets the turret holder with the corresponding ID number. Returns null if none are found.
    /// </summary>
    /// <returns>The turret holder with the corresponding ID number.</returns>
    /// <param name="id">The ID number to look for.</param>
    public CShipTurretHolder GetCTurretHolderWithID (int id)
    {
        foreach (CShipTurretHolder holder in m_turretHolders)
        {
            if (holder.GetShipTurretID() == id)
            {
                return holder;
            }
        }
        
        Debug.LogWarning ("Couldn't find CShipHolder with id #" + id);
        return null;
    }


    /// <summary>
    /// Gets the turret glow with the corresponding ID number. Returns null if none are found.
    /// </summary>
    /// <returns>The turret glow with the corresponding ID number.</returns>
    /// <param name="id">The ID number to look for.</param>
    public CapitalShipGlowScript GetGlowForTurretByID (int id)
    {
        foreach (CapitalShipGlowScript glow in m_turretGlows)
        {
            if (glow.GetGlowID() == id)
            {
                return glow;
            }
        }
        
        return null;
    }


    /// <summary>
    /// Refreshes the turret holder and glow ache by using GetComponentsInChildren<T>(). If called by the server it the propagate the request to clients.
    /// </summary>
    [RPC] void RefreshTurretCache()
    {
        m_turretHolders = GetComponentsInChildren<CShipTurretHolder>();
        m_turretGlows = GetComponentsInChildren<CapitalShipGlowScript>();

        if (Network.isServer)
        {
            networkView.RPC ("RefreshTurretCache", RPCMode.Others);
        }
    }

    #endregion Turret functionality


    #region Cash functions

    /// <summary>
    /// Determines whether this instance has enough cash for the specified amount.
    /// </summary>
    /// <returns><c>true</c> if this instance has enough cash; otherwise, <c>false</c>.</returns>
    /// <param name="amount">Amount.</param>
    public bool HasEnoughCash (int amount)
    {
        return m_bankedCash >= amount;
    }


    /// <summary>
    /// Allows for the incrementing/decrementing of the CShip cash.
    /// </summary>
    /// <param name="amount">How much to increment by (negatives values decrement).</param>
    public void AlterCash (int amount)
    {
        if (HasEnoughCash (Mathf.Abs (amount)))
        {
            m_bankedCash = Mathf.Max (0, m_bankedCash + amount);

            networkView.RPC ("PropagateCShipCash", RPCMode.Others, m_bankedCash);

            if (m_bankedCash - amount < 500 && m_bankedCash >= 500)
            {
                GameObject.FindGameObjectWithTag("GameController").GetComponent<GameStateController>().AlertMoneyAboveRespawn();
            }
        }
    }


    /// <summary>
    /// The RPC used for clients to receive the correct current amount of cash which the CShip has.
    /// </summary>
    /// <param name="amount">The value for m_bankedCash.</param>
    [RPC] void PropagateCShipCash (int amount)
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
        
        networkView.RPC ("PropagateResourceWater", RPCMode.Others, m_currentResources.water, m_maxResources.water);
    }
    
    
    /// <summary>
    /// Allows for the incrementing/decrementing of the available fuel resource.
    /// </summary>
    /// <param name="amount">How much to increment by (negatives values decrement).</param>
    public void AlterCurrentResourceFuel (int amount)
    {
        m_currentResources.fuel = Mathf.Clamp (m_currentResources.fuel + amount, 0, m_maxResources.fuel);
        
        networkView.RPC ("PropagateResourceFuel", RPCMode.Others, m_currentResources.fuel, m_maxResources.fuel);
    }
    
    
    /// <summary>
    /// Allows for the incrementing/decrementing of the available mass resource.
    /// </summary>
    /// <param name="amount">How much to increment by (negatives values decrement).</param>
    public void AlterCurrentResourceMass (int amount)
    {
        m_currentResources.mass = Mathf.Clamp (m_currentResources.mass + amount, 0, m_maxResources.mass);
        
        networkView.RPC ("PropagateResourceMass", RPCMode.Others, m_currentResources.mass, m_maxResources.mass);
    }
    
    
    /// <summary>
    /// Allows for the incrementing/decrementing of the maximum water resource.
    /// </summary>
    /// <param name="amount">How much to increment by (negatives values decrement).</param>
    public void AlterMaxResourceWater (int amount)
    {
        // Clamp the minimum to 0
        m_maxResources.water = Mathf.Max (0, m_maxResources.water + amount);
        
        // Ensure the limit hasn't been reduced below the current availability
        if (m_maxResources.water < m_currentResources.water)
        {
            m_currentResources.water = m_maxResources.water;
        }
        
        networkView.RPC ("PropagateResourceWater", RPCMode.Others, m_currentResources.water, m_maxResources.water);
    }


    /// <summary>
    /// Allows for the incrementing/decrementing of the maximum fuel resource.
    /// </summary>
    /// <param name="amount">How much to increment by (negatives values decrement).</param>
    public void AlterMaxResourceFuel (int amount)
    {
        // Clamp the minimum to 0
        m_maxResources.fuel = Mathf.Max (0, m_maxResources.fuel + amount);
        
        // Ensure the limit hasn't been reduced below the current availability
        if (m_maxResources.fuel < m_currentResources.fuel)
        {
            m_currentResources.fuel = m_maxResources.fuel;
        }
        
        networkView.RPC ("PropagateResourceFuel", RPCMode.Others, m_currentResources.fuel, m_maxResources.fuel);
    }


    /// <summary>
    /// Allows for the incrementing/decrementing of the maximum mass resource.
    /// </summary>
    /// <param name="amount">How much to increment by (negatives values decrement).</param>
    public void AlterMaxResourceMass (int amount)
    {
        // Clamp the minimum to 0
        m_maxResources.mass = Mathf.Max (0, m_maxResources.mass + amount);

        // Ensure the limit hasn't been reduced below the current availability
        if (m_maxResources.mass < m_currentResources.mass)
        {
            m_currentResources.mass = m_maxResources.mass;
        }
        
        networkView.RPC ("PropagateResourceMass", RPCMode.Others, m_currentResources.mass, m_maxResources.mass);
    }


    /// <summary>
    /// An RPC used to propagate the amount of water resources available.
    /// </summary>
    /// <param name="currentWater">How much water is currently available.</param>
    /// <param name="maxWater">How much the water limit should be.</param>
    [RPC] void PropagateResourceWater (int currentWater, int maxWater)
    {
        m_currentResources.water = currentWater;
        m_maxResources.water = maxWater;
    }


    /// <summary>
    /// An RPC used to propagate the amount of fuel resources available.
    /// </summary>
    /// <param name="currentFuel">How much fuel is currently available.</param>
    /// <param name="maxFuel">How much the fuel limit should be.</param>
    [RPC] void PropagateResourceFuel (int currentFuel, int maxFuel)
    {
        m_currentResources.fuel = currentFuel;
        m_maxResources.fuel = maxFuel;
    }


    /// <summary>
    /// An RPC used to propagate the amount of mass resources available.
    /// </summary>
    /// <param name="currentMass">How much mass is currently available.</param>
    /// <param name="maxMass">How much the mass limit should be.</param>
    [RPC] void PropagateResourceMass (int currentMass, int maxMass)
    {
        m_currentResources.mass = currentMass;
        m_maxResources.mass = maxMass;
    }

    #endregion Resource functions


    #region Death animation

    /// <summary>
    /// Starts the explosion animation which happens on the CShip before it finally splits.
    /// </summary>
	public void BeginDeathBuildUpAnim()
	{
		GameObject explodeObject = Instantiate (m_buildUpExplodeRef, this.transform.position + new Vector3 (0, 0, -1.0f), this.transform.rotation) as GameObject;
		
        // Place the build up explosion inside the CShip heirarchy.
        if (explodeObject != null)
        {
            explodeObject.transform.parent = this.transform;
        }
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


    #region Weapon targetting
    
        /// <summary>
    /// Allows a turret to claim a target so no other turrets can fire at it.
    /// </summary>
    /// <param name="mob">The mob to be claimed.</param>
    public void ClaimTarget (GameObject mob)
    {
        if (m_potentialTargets.Remove (mob))
        {
            m_alreadyBeingTargetted.Add (mob);
        }
    }
    
    
    /// <summary>
    /// Removes a target from the targetted list, this allows it to be claimed by another turret.
    /// </summary>
    /// <param name="mob">The mob to be unclaimed.</param>
    public void UnclaimTarget (GameObject mob)
    {
        m_alreadyBeingTargetted.Remove (mob);
    }


    /// <summary>
    /// Requests that the target lists be updated and returned to the callee. This checks for nearby enemies and removes dead enemies from the lists. 
    /// The lists will only be updated once every FixedUpdate(), any subsequent calls will not update the lists and instead will just return the unclaimed list.
    /// </summary>
    /// <returns>A list of unclaimed targets which can be claimed by a weapon.</returns>
    /// <param name="layerMask">The layer mask to use when searching for nearby targets.</param>
    public List<GameObject> RequestTargets (int layerMask)
    {
        UpdateTargetLists (layerMask);

        return m_potentialTargets;
    }


    /// <summary>
    /// Based on a layer mask the potential targets list will be cleared and refreshed, this allows for the list to be accurate of the enemies around the ship.
    /// </summary>
    /// <param name="layerMask">Layer mask.</param>
    void UpdateTargetLists (int layerMask)
    {
        if (!m_updatedTargetListThisFrame)
        {
            m_updatedTargetListThisFrame = true;

            m_potentialTargets.Clear();

            GameObject[] objects = Physics.OverlapSphere (transform.position, m_searchRadius, layerMask).GetAttachedRigidbodies().GetUniqueOnly().GetGameObjects();

            if (objects != null)
            {
                m_potentialTargets.AddRange (objects);
                
                for (int i = m_alreadyBeingTargetted.Count - 1; i >= 0; --i)
                {
                    if (m_alreadyBeingTargetted[i] == null || Vector2.SqrMagnitude(m_alreadyBeingTargetted[i].transform.position - transform.position) > m_searchRadius.Squared())
                    {
                        m_alreadyBeingTargetted.RemoveAt (i);
                    }

                    else
                    {
                        m_potentialTargets.Remove (m_alreadyBeingTargetted[i]);
                    }
                }
            }
        }
    }

    #endregion Weapon targetting
}

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
    /* Serializable Members */
	[SerializeField] bool   m_shouldRecieveInput = true;
	[SerializeField] float  m_baseEngineSpeed = 5.0f;
	[SerializeField] float  m_baseEngineTurnSpeed = 1.0f;
	[SerializeField] int    m_baseShipHull = 25;
	[SerializeField] float  m_baseShipWeight = 0.05f;
	[SerializeField] float  m_maxDockingSpeed = 225f;		//Maxmium docking speed for players
	[SerializeField] float  m_dockRotateSpeed = 3f;			//How quickly to rotate the ship towards the dock
    [SerializeField] float  m_playerStrafeMod = 0.6f;
	[SerializeField] int    m_currentCash = 0;

    //Inventory
    [SerializeField] ItemWrapper m_equippedWeaponItem;
    [SerializeField] ItemWrapper m_equippedShieldItem;
    [SerializeField] ItemWrapper m_equippedEngineItem;
    [SerializeField] ItemWrapper m_equippedPlatingItem;
    [SerializeField] List<ItemWrapper> m_playerInventory;

    /* Internal Members */
    bool m_isAnimating = false;
    DockingState m_currentDockingState = DockingState.NOTDOCKING;
    Vector3 m_targetPoint = Vector3.zero;
    float m_dockingTime = 0.0f;				    //Used to determine if the player should continue the docking attempt
    //bool m_shouldPlaySound = false;
    float m_volumeHolder = 1.0f;
    bool m_useController = false;
    Quaternion m_targetAngle;

    bool m_playerIsOutOfBounds = false;

    bool m_isInRangeOfCapitalDock = false;
    bool m_isInRangeOfTradingDock = false;
    GameObject m_nearbyShop = null;
    
    // Homing Stuff
    bool m_currentWeaponNeedsLockon = false;
    bool m_correctScreenToHome = false;
    bool m_isLockingOn = false;
    float m_lockOnTime = 0.0f;
    float m_reqLockOnTime = 0.7f;
    GameObject m_lockingTarget = null;
    GameObject m_lockedOnTarget = null;
    
    #region getters
    public GameObject GetLockedTarget()
    {
        return m_lockedOnTarget;
    }
    public bool GetNeedsLock()
    {
        return m_currentWeaponNeedsLockon;
    }
    #endregion

    /* Cached members */
    GameObject m_cShipCache = null;
    GameObject[] m_shops = null;
    GameStateController m_gscCache = null;
    GUIInGameMaster m_guiCache = null;

    #region getset

    public int GetCash()
	{
		return m_currentCash;
	}

    public void AddCash(int amount)
	{
		m_currentCash += amount;

		if(Network.player != m_owner)
		{
			networkView.RPC ("PropagateCashAmount", m_owner, m_currentCash);
		}
	}

    public bool RemoveCash(int amount)
    {
        if (CheckCanAffordAmount(amount))
        {
            m_currentCash -= amount;

            networkView.RPC("PropagateCashAmount", RPCMode.Server, m_currentCash);
            return true;
        }
        else
            return false;
    }

    public ItemWrapper GetEquipedPlatingItem()
    {
        return m_equippedPlatingItem;
    }

    public void SetEquippedPlatingItem(ItemWrapper platingItem_)
    {
        m_equippedPlatingItem = platingItem_;
    }

    public ItemWrapper GetEquipedEngineItem()
    {
        return m_equippedEngineItem;
    }

    public void SetEquippedEngineItem(ItemWrapper engineItem_)
    {
        m_equippedEngineItem = engineItem_;
    }

    public ItemWrapper GetEquipedShieldItem()
    {
        return m_equippedShieldItem;
    }

    public void SetEquippedShieldItem(ItemWrapper shieldItem_)
    {
        m_equippedShieldItem = shieldItem_;
    }

    public ItemWrapper GetEquipedWeaponItem()
    {
        return m_equippedWeaponItem;
    }

    public void SetEquippedWeaponItem(ItemWrapper weaponItem_)
    {
        m_equippedWeaponItem = weaponItem_;
    }

    public List<ItemWrapper> GetPlayerInventory()
    {
        return m_playerInventory;
    }

    public bool IsInRangeOfCapitalDock()
    {
        return m_isInRangeOfCapitalDock;
    }

    public void SetIsInRangeOfCapitalDock(bool flag_)
    {
        m_isInRangeOfCapitalDock = flag_;
    }

    public bool IsInRangeOfTradingDock()
    {
        return m_isInRangeOfTradingDock;
    }

    public void SetIsInRangeOfTradingDock(bool flag_)
    {
        m_isInRangeOfTradingDock = flag_;
    }

    public GameObject GetNearbyShop()
    {
        return m_nearbyShop;
    }

    public void SetNearbyShop(GameObject shop_)
    {
        m_nearbyShop = shop_;
    }
    
    public void SetCorrectHomingScreen(bool ready)
    {
        m_correctScreenToHome = ready;
    }
    #endregion

    /* Unity Functions */
    protected override void Awake()
    {
        base.Awake();
        m_guiCache = GameObject.FindGameObjectWithTag("GUIManager").GetComponent<GUIInGameMaster>();
    }

    void Start()
    {
        m_volumeHolder = PlayerPrefs.GetFloat("EffectVolume", 1.0f);
        m_playerInventory = new List<ItemWrapper>(5);

        if (Network.isServer)
        {
            ResetEquippedWeapon();
            ResetEquippedShield();
            ResetEquippedEngine();
            ResetEquippedPlating();
        }

        m_gscCache = GameStateController.Instance();
        StartCoroutine(EnsureEquipmentValidity());
    }

    protected override void Update()
    {
        base.Update();
        m_ownerSt = m_owner.ToString();

        if (m_owner == Network.player)
        {
            if ((m_useController && Input.GetButtonDown("X360Start")) || (!m_useController && Input.GetKeyDown(KeyCode.Escape)))
            {
                m_gscCache.ToggleMainMenu();
            }

            if (m_isAnimating)
            {
                UpdateDockingState();
            }

            if (m_shouldRecieveInput)
            {
                if (m_useController && Input.GetJoystickNames().Length < 1)
                {
                    m_useController = false;
                }


                if (!m_isAnimating)
                {
                    if ((m_useController && Input.GetButtonDown("X360X")) || (!m_useController && Input.GetKey(KeyCode.X)))
                    {
                        StartDocking();
                    }

                    //In here, player should respond to any input
                    if (!m_useController)
                    {
                        UpdateFromKeyboardInput();
                    }
                    else
                    {
                        UpdateFromController();
                    }

                    
                    if ((m_useController && Input.GetButtonDown("X360B")) || (!m_useController && Input.GetMouseButtonDown(2)))
                    {
                        BreakLock();
                    }

                    if (m_useController)
                    {
                        //Don't rotate to face cursor, instead, listen for right stick input
                        ListenForControllerShootStick();
                    }
                    else
                    {
                        //Here, it should rotate to face the mouse cursor
                        RotateTowardsMouse();
                        
                        //Now attempt homing lockon
                        if(m_correctScreenToHome && m_currentWeaponNeedsLockon)
                        {
                            UpdateHoming();
                        }
                    }
                }
            }

            //Increment homing vars, if appropriate
            if(m_correctScreenToHome && m_currentWeaponNeedsLockon)
            {
                UpdateHomingVars();
            }

            //Finish by checking to make sure we're not too far from 0,0
            float distance = (this.transform.position - new Vector3(0, 0, 10)).sqrMagnitude;
            if (m_playerIsOutOfBounds)
            {
                if (distance < 290f.Squared())
                {
                    //Stop warning screen
                    m_guiCache.SetOutOfBoundsWarning(false);
                    m_playerIsOutOfBounds = false;
                }
            }
            else
            {
                if (distance >= 290f.Squared())
                {
                    //Begin warning screen
                    m_guiCache.SetOutOfBoundsWarning(true);
                    m_playerIsOutOfBounds = true;
                }
            }
            
            //Finally, check distances to dock and shops
            float cshipDist = Vector3.Distance(m_cShipCache.transform.position, this.transform.position);
            if(cshipDist < 7.5f)
            {
                SetIsInRangeOfCapitalDock(true);
                m_guiCache.PassThroughCShipDockableState(true);
                
            }
            else
            {
                SetIsInRangeOfCapitalDock(false);
                m_guiCache.PassThroughCShipDockableState(false);
            }
            
            GameObject nearestShop = GetClosestShop();
            float shopDist = Vector3.Distance(nearestShop.transform.position, this.transform.position);
            if(shopDist < 1.5f)
            {
                m_nearbyShop = nearestShop;
                m_isInRangeOfTradingDock = true;
                m_guiCache.PassThroughShopDockableState(true);
            }
            else
            {
                m_nearbyShop = null;
                m_isInRangeOfTradingDock = false;
                m_guiCache.PassThroughShopDockableState(false);
            }
        }
        else
        {

        }
    }

    void OnDestroy()
    {
        if (Network.player == m_owner)
            Screen.showCursor = true;
    }

    /* Custom Functions */
    #region Cash-related functions
    [RPC] void PropagateCashAmount(int amount)
    {
        m_currentCash = amount;
    }

    public bool CheckCanAffordAmount(int amount)
    {
        if (m_currentCash >= amount)
            return true;
        else
            return false;
    }
    #endregion

    #region Dock-related functions
    private void StartDocking()
    {
        if (m_isInRangeOfCapitalDock)
        {
            //GameObject.FindGameObjectWithTag("GUIManager").GetComponent<GUIManager>().m_PlayerHasDockedAtCapital = true;
            /*GameObject.FindGameObjectWithTag("GameController").GetComponent<GameStateController>().NotifyLocalPlayerHasDockedAtCShip();
            transform.parent = GameObject.FindGameObjectWithTag("Capital").transform;
            rigidbody.isKinematic = true;
            m_shouldRecieveInput = false;*/

            //Begin the animation sequence
            m_cShipCache = GameObject.FindGameObjectWithTag("Capital");
            m_targetPoint = m_cShipCache.transform.position + (m_cShipCache.transform.right * 7.0f) + (m_cShipCache.transform.up * 1.5f);
            m_currentDockingState = DockingState.OnApproach;
            //GameObject.FindGameObjectWithTag("GUIManager").GetComponent<GUIManager>().CloseMap();
            m_isAnimating = true;
        }
        else if (m_isInRangeOfTradingDock && m_nearbyShop != null)
        {
            GameObject.FindGameObjectWithTag("GameController").GetComponent<GameStateController>().NotifyLocalPlayerHasDockedAtShop(m_nearbyShop);
            transform.parent = m_nearbyShop.transform;
            rigidbody.isKinematic = true;
            m_shouldRecieveInput = false;

        }
    }
    
    GameObject GetClosestShop()
    {
        if(m_shops == null)
            m_shops = GameObject.FindGameObjectsWithTag("Shop");

        float shortestDistance = 999;
        GameObject shortestShop = null;
        foreach (GameObject shop in m_shops)
        {
            float distance = Vector3.Distance(shop.transform.position, this.transform.position);
            if (shortestShop == null || distance < shortestDistance)
            {
                shortestDistance = distance;
                shortestShop = shop;
            }
        }
        return shortestShop;
    }
    
    void UpdateDockingState()
    {
        //If for any reason CShip is not set, find it
        if (m_cShipCache == null)
        {
            m_cShipCache = GameObject.FindGameObjectWithTag("Capital");
        }
        
        //Now animate based on state
        switch (m_currentDockingState)
        {
            case DockingState.NOTDOCKING:
            {
                //We shouldn't even be here man
                //We shouln't even BE here!
                m_isAnimating = false;
                networkView.RPC("PropagateInvincibility", RPCMode.All, false);
                rigidbody.isKinematic = false;
                break;
            }
            case DockingState.OnApproach:
            {
                // Make sure targetPoint is up to date
                m_targetPoint = m_cShipCache.transform.position + (m_cShipCache.transform.right * 7.0f);
                
                // Move towards entrance point
                Vector3 direction = m_targetPoint - transform.position;
                Vector3 rotation = -m_cShipCache.transform.right;
                MoveToDockPoint(direction, rotation);
                
                // If we're near, switch to onEntry
                if (direction.magnitude <= 1.35f)
                {
                    m_dockingTime += Time.deltaTime;
                    
                    if (m_dockingTime >= 0.36f)
                    {
                        // Reset the docking time
                        m_dockingTime = 0f;
                        
                        // Kill our speed temporarily
                        rigidbody.isKinematic = true;
                        m_currentDockingState = DockingState.OnEntry;
                        m_targetPoint = m_cShipCache.transform.position + (m_cShipCache.transform.up * 1.5f);
                        rigidbody.isKinematic = false;
                        this.transform.position = new Vector3(this.transform.position.x, this.transform.position.y, 10.75f);
                    }
                }
                
                else
                {
                    m_dockingTime = 0f;
                }
                
                //Play sounds
                /*if (!shouldPlaySound)
                        {
                            shouldPlaySound = true;
                            this.audio.volume = volumeHolder;
                            this.audio.Play();
                            networkView.RPC("PropagateIsPlayingSound", RPCMode.Others, shouldPlaySound);
                        }
                        recievedInput = true;*/
                break;
            }
            case DockingState.OnEntry:
            {
                //Make sure targetPoint is up to date
                m_targetPoint = m_cShipCache.transform.position;
                
                //Rotate towards entrance point
                Vector3 direction = m_targetPoint - transform.position;
                Vector3 rotation = -m_cShipCache.transform.right;
                MoveToDockPoint(direction, rotation);
                
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
                        transform.rotation = m_cShipCache.transform.rotation;
                        GameObject.FindGameObjectWithTag("GameController").GetComponent<GameStateController>().NotifyLocalPlayerHasDockedAtCShip();
                        transform.parent = m_cShipCache.transform;
                        rigidbody.isKinematic = true;
                        networkView.RPC("PropagateInvincibility", RPCMode.All, true);
                    }
                }
                
                else
                {
                    // Reset the docking time
                    m_dockingTime = 0f;
                }
                
                //Play sounds
                /*if (!shouldPlaySound)
                        {
                            shouldPlaySound = true;
                            this.audio.volume = volumeHolder;
                            this.audio.Play();
                            networkView.RPC("PropagateIsPlayingSound", RPCMode.Others, shouldPlaySound);
                        }
                        recievedInput = true;*/
                break;
            }
            case DockingState.Docked:
            {
                //We shouldn't need to do anything. Await GUI telling us we're done
                
                // Stop exception spam by ensuring the CShip is alive
                if (m_cShipCache)
                {
                    //Ensure rotation matches CShip
                    transform.rotation = m_cShipCache.transform.rotation;
                    
                    //Also position
                    float oldZ = transform.position.z;
                    transform.position = new Vector3(m_cShipCache.transform.position.x, m_cShipCache.transform.position.y, oldZ);
                    
                }
                break;
            }
            case DockingState.Exiting:
            {
                //Accelerate forwards
                this.rigidbody.AddForce(this.transform.up * GetCurrentMomentum() * Time.deltaTime);
                
                //If we're far enough away, stop animating
                Vector3 dir = m_cShipCache.transform.position - transform.position;
                if (dir.magnitude >= 12.0f)
                {
                    //Fly free!
                    m_currentDockingState = DockingState.NOTDOCKING;
                    m_isAnimating = false;
                    this.transform.position = new Vector3(this.transform.position.x, this.transform.position.y, 10.0f);
                    networkView.RPC("PropagateInvincibility", RPCMode.All, false);
                    rigidbody.isKinematic = false;
                }
                break;
            }
        }
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
    
    [RPC] void PropagateInvincibility(bool state)
    {
        GetComponent<HealthScript>().SetInvincible(state);
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
    #endregion

    #region Input/Update Functions
    void RotateTowardsMouse()
    {
        var objectPos = Camera.main.WorldToScreenPoint(transform.position);
        var dir = Input.mousePosition - objectPos;
        
        RotateTowards(transform.position + dir);
        
        if (Input.GetMouseButton(0))
        {
            this.GetComponent<PlayerWeaponScript>().PlayerRequestsFire();
        }
        
        if (Input.GetMouseButtonUp(0))
        {
            this.GetComponent<PlayerWeaponScript>().PlayerReleaseFire();
        }
    }
    
    void ListenForControllerShootStick()
    {
        float v = Input.GetAxis("RightStickVertical");
        float h = Input.GetAxis("RightStickHorizontal");
        
        if (v != 0 || h != 0)
        {
            float angle = (Mathf.Atan2(v, h) - Mathf.PI / 2) * Mathf.Rad2Deg;
            Quaternion target = Quaternion.Euler(new Vector3(0, 0, angle));
            m_targetAngle = target;
        }
        
        transform.rotation = Quaternion.Slerp(transform.rotation, m_targetAngle, GetRotateSpeed() * Time.deltaTime);
        
        if (Input.GetAxis("X360Triggers") < 0)
            this.GetComponent<PlayerWeaponScript>().PlayerRequestsFire();
        else if (Input.GetAxis("X360Triggers") == 0)
            this.GetComponent<PlayerWeaponScript>().PlayerReleaseFire();
    }
    
    public void TellShipStartRecievingInput()
    {
        m_shouldRecieveInput = true;
    }
    
    public void TellShipStopRecievingInput()
    {
        m_shouldRecieveInput = false;
    }
    
    [RPC] void PropagateRecieveInput()
    {
        m_shouldRecieveInput = false;
        this.GetComponent<HealthScript>().SetShouldStop(true);
    }
    
    private void UpdateFromController()
    {
        if (Input.GetAxis("LeftStickVertical") > 0)
        {
            //Forward
            float v = Input.GetAxis("LeftStickVertical");
            float h = Input.GetAxis("LeftStickHorizontal");

            Vector3 inputVec = new Vector3(h, v, 0);
            if (inputVec.sqrMagnitude > 1.0f)
            {
                inputVec.Normalize();
                inputVec *= 0.7071067f;
            }
            Vector3 forward = this.transform.up;

            float forwardSpeedFac = Mathf.Abs(Vector3.Dot(inputVec.normalized, forward));

            float speed = 0;
            if (forwardSpeedFac > 0.95f)
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

            //if (!shouldPlaySound)
            //{
            //    shouldPlaySound = true;
            //    this.audio.volume = volumeHolder;
            //    this.audio.Play();
            //    networkView.RPC("PropagateIsPlayingSound", RPCMode.Others, shouldPlaySound);
            //}
            //receivedInput = true;
        }

        if (Input.GetAxis("LeftStickVertical") < 0)
        {
            //Back
            float v = Input.GetAxis("LeftStickVertical");
            float h = Input.GetAxis("LeftStickHorizontal");

            Vector3 inputVec = new Vector3(h, v, 0);
            if (inputVec.sqrMagnitude > 1.0f)
            {
                inputVec.Normalize();
                inputVec *= 0.7071067f;
            }
            Vector3 forward = this.transform.up;

            float forwardSpeedFac = Mathf.Abs(Vector3.Dot(inputVec.normalized, forward));
            float speed = 0;
            if (forwardSpeedFac > 0.95f)
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

            //if (!shouldPlaySound)
            //{
            //    shouldPlaySound = true;
            //    this.audio.volume = volumeHolder;
            //    this.audio.Play();
            //    networkView.RPC("PropagateIsPlayingSound", RPCMode.Others, shouldPlaySound);
            //}
            //receivedInput = true;
        }

        if (Input.GetAxis("LeftStickHorizontal") < 0)
        {
            //Left
            float v = Input.GetAxis("LeftStickVertical");
            float h = Input.GetAxis("LeftStickHorizontal");

            Vector3 inputVec = new Vector3(h, v, 0);
            if (inputVec.sqrMagnitude > 1.0f)
            {
                inputVec.Normalize();
                inputVec *= 0.7071067f;
            }
            Vector3 forward = this.transform.up;

            float forwardSpeedFac = Mathf.Abs(Vector3.Dot(inputVec.normalized, forward));
            float speed = 0;
            if (forwardSpeedFac > 0.95f)
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

            //if (!shouldPlaySound)
            //{
            //    shouldPlaySound = true;
            //    this.audio.volume = volumeHolder;
            //    this.audio.Play();
            //    networkView.RPC("PropagateIsPlayingSound", RPCMode.Others, shouldPlaySound);
            //}
            //receivedInput = true;
        }

        if (Input.GetAxis("LeftStickHorizontal") > 0)
        {
            //Right
            float v = Input.GetAxis("LeftStickVertical");
            float h = Input.GetAxis("LeftStickHorizontal");

            Vector3 inputVec = new Vector3(h, v, 0);
            if (inputVec.sqrMagnitude > 1.0f)
            {
                inputVec.Normalize();
                inputVec *= 0.7071067f;
            }
            Vector3 forward = this.transform.up;

            float forwardSpeedFac = Mathf.Abs(Vector3.Dot(inputVec.normalized, forward));
            float speed = 0;
            if (forwardSpeedFac > 0.95f)
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

            //if (!shouldPlaySound)
            //{
            //    shouldPlaySound = true;
            //    this.audio.volume = volumeHolder;
            //    this.audio.Play();
            //    networkView.RPC("PropagateIsPlayingSound", RPCMode.Others, shouldPlaySound);
            //}
            //receivedInput = true;
        }

        if (Input.GetButtonDown("X360Back"))
        {
            m_guiCache.ToggleMapsTogether();
        }
    }

    private void UpdateFromKeyboardInput()
    {
        if (Input.GetKey(KeyCode.W))
        {
            //this.rigidbody.AddForce(this.transform.up * m_playerMoveSpeed * Time.deltaTime);
            this.rigidbody.AddForce(this.transform.up * GetCurrentMomentum() * Time.deltaTime);

            //Play sound + particles
            //if (!shouldPlaySound)
            //{
            //    shouldPlaySound = true;
            //    this.audio.volume = volumeHolder;
            //    this.audio.Play();
            //    networkView.RPC("PropagateIsPlayingSound", RPCMode.Others, shouldPlaySound);
            //}
            //receivedInput = true;
        }

        if (Input.GetKey(KeyCode.S))
        {
            //this.rigidbody.AddForce(this.transform.up * -m_playerMoveSpeed * Time.deltaTime);
            this.rigidbody.AddForce(this.transform.up * (-GetCurrentMomentum() * m_playerStrafeMod) * Time.deltaTime);

            //if (!shouldPlaySound)
            //{
            //    shouldPlaySound = true;
            //    this.audio.volume = volumeHolder;
            //    this.audio.Play();
            //    networkView.RPC("PropagateIsPlayingSound", RPCMode.Others, shouldPlaySound);
            //}
            //receivedInput = true;
        }

        if (Input.GetKey(KeyCode.A))
        {
            //this.rigidbody.AddForce(this.transform.right * (-m_playerMoveSpeed * m_playerStrafeMod) * Time.deltaTime);
            this.rigidbody.AddForce(this.transform.right * (-GetCurrentMomentum() * m_playerStrafeMod) * Time.deltaTime);

            //if (!shouldPlaySound)
            //{
            //    shouldPlaySound = true;
            //    this.audio.volume = volumeHolder;
            //    this.audio.Play();
            //    networkView.RPC("PropagateIsPlayingSound", RPCMode.Others, shouldPlaySound);
            //}
            //receivedInput = true;
        }

        if (Input.GetKey(KeyCode.D))
        {
            //this.rigidbody.AddForce(this.transform.right * (m_playerMoveSpeed * m_playerStrafeMod) * Time.deltaTime);
            this.rigidbody.AddForce(this.transform.right * (GetCurrentMomentum() * m_playerStrafeMod) * Time.deltaTime);

            //if (!shouldPlaySound)
            //{
            //    shouldPlaySound = true;
            //    this.audio.volume = volumeHolder;
            //    this.audio.Play();
            //    networkView.RPC("PropagateIsPlayingSound", RPCMode.Others, shouldPlaySound);
            //}
            //receivedInput = true;
        }

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            m_gscCache.ToggleBigMapState();
        }

        if (Input.GetKeyDown(KeyCode.Z))
        {
            /*bool mapVal = GameObject.FindGameObjectWithTag("GUIManager").GetComponent<GUIManager>().m_isOnFollowMap;
            GameObject.FindGameObjectWithTag("GUIManager").GetComponent<GUIManager>().SetIsOnFollowMap(!mapVal);*/

            m_gscCache.ToggleSmallMapState();
        }
    }
    
    public void SetInputMethod(bool useControl)
    {
        m_useController = useControl;
    }
    #endregion

    #region Equipment Functions
    public float GetReloadPercentage()
    {
        return GetWeaponObject().GetComponent<EquipmentWeapon>().GetReloadPercentage();
    }
    
	public void EquipEngineStats(float moveSpeed, float turnSpeed, float strafeMod)
	{
        SetMaxShipSpeed(m_baseEngineSpeed + moveSpeed);
        SetCurrentShipSpeed(m_baseEngineSpeed + moveSpeed);
        SetRotateSpeed(m_baseEngineTurnSpeed + turnSpeed);
        m_playerStrafeMod = strafeMod;
	}
	
	public void ResetEquippedWeapon()
	{
		//Debug.Log ("Recieved request to reset weapon. Re-equipping weapon: " + m_equippedWeaponItem.GetComponent<ItemScript>().GetItemName());

		GameObject equippedWeap = GetWeaponObject();
		if(equippedWeap != null)
		{
			Debug.Log ("Destroyed old weapon: " + equippedWeap.name + ".");
			Network.Destroy(equippedWeap);
		}


		GameObject weapon = (GameObject)Network.Instantiate(m_equippedWeaponItem.GetComponent<ItemWrapper>().GetItemPrefab(), this.transform.position, this.transform.rotation, 0);
		weapon.transform.parent = this.transform;
		weapon.transform.localPosition = weapon.GetComponent<EquipmentWeapon>().GetOffset();

		networkView.RPC ("PropagateWeaponResetHomingBool", RPCMode.All, m_equippedWeaponItem.GetComponent<ItemWrapper>().GetItemPrefab().GetComponent<EquipmentWeapon>().GetNeedsLockon());
		
		//Parenting needs to be broadcast to all clients!
		string name = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameStateController>().GetNameFromNetworkPlayer(m_owner);
		weapon.GetComponent<EquipmentWeapon>().ParentWeaponToOwner(name);
		this.GetComponent<PlayerWeaponScript>().EquipWeapon(weapon);
	}

	[RPC] void PropagateWeaponResetHomingBool(bool state)
	{
		if(m_owner == Network.player)
		{
            m_currentWeaponNeedsLockon = state;
		}
	}

	public void ResetEquippedShield()
	{
		GameObject shield = (GameObject)Network.Instantiate(m_equippedShieldItem.GetComponent<ItemWrapper>().GetItemPrefab(), this.transform.position, this.transform.rotation, 0);
		shield.transform.parent = this.transform;
		
		ShieldScript ssc = shield.GetComponent<ShieldScript>();
		shield.transform.localPosition = ssc.GetOffset();

		string name = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameStateController>().GetNameFromNetworkPlayer(m_owner);
		ssc.ParentToPlayer(name);

		HealthScript HP = this.GetComponent<HealthScript>();
		HP.EquipNewShield(ssc.GetShieldMaxCharge(), ssc.GetShieldRechargeRate(), ssc.GetShieldRechargeDelay());
	}

	
	public void ResetEquippedEngine()
	{
        GameObject oldEngine = GetEngineObject();

        if(oldEngine != null)
        {
            Network.Destroy(oldEngine);
        }

		GameObject engine = (GameObject)Network.Instantiate(m_equippedEngineItem.GetComponent<ItemWrapper>().GetItemPrefab(), this.transform.position, this.transform.rotation, 0);
		engine.transform.parent = this.transform;

		EngineScript esc = engine.GetComponent<EngineScript>();
		engine.transform.localPosition = esc.GetOffset();

		string name = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameStateController>().GetNameFromNetworkPlayer(m_owner);
		esc.ParentToPlayer(name);

        m_playerStrafeMod = esc.GetStrafeModifier();
        SetMaxShipSpeed(m_baseEngineSpeed + esc.GetMoveSpeed());
        SetCurrentShipSpeed(m_baseEngineSpeed + esc.GetMoveSpeed());
        SetRotateSpeed(m_baseEngineTurnSpeed + esc.GetTurnSpeed());

	}

	
	public void ResetEquippedPlating()
	{
		GameObject plating = (GameObject)Network.Instantiate(m_equippedPlatingItem.GetComponent<ItemWrapper>().GetItemPrefab(), this.transform.position, this.transform.rotation, 0);
		plating.transform.parent = this.transform;
		
		PlatingScript psc = plating.GetComponent<PlatingScript>();
		plating.transform.localPosition = psc.GetOffset();

		string name = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameStateController>().GetNameFromNetworkPlayer(m_owner);
		psc.ParentToPlayer(name);

		//Update hull
		this.GetComponent<HealthScript>().EquipNewPlating (psc.GetPlatingHealth() + m_baseShipHull);

		//Update RB
		rigidbody.mass = m_baseShipWeight + psc.GetPlatingWeight();
	}
    
    IEnumerator EnsureEquipmentValidity()
    {
        if (m_owner == Network.player)
        {
            while (true)
            {
                ItemWrapper script;
                
                // Ensure the weapon is valid
                if (GetWeaponObject() == null)
                {
                    script = m_equippedWeaponItem ? m_equippedWeaponItem.GetComponent<ItemWrapper>() : null;
                    
                    if (script == null || script.GetItemType() != ItemType.Weapon)
                    {
                        m_equippedWeaponItem = GameObject.FindGameObjectWithTag ("ItemManager").GetComponent<ItemIDHolder>().GetItemWithID (0);
                        Debug.LogError ("Resetting null WeaponObject on: " + name);
                    }
                    
                    ResetEquippedWeapon();
                }
                
                if (GetShieldObject() == null)
                {
                    script = m_equippedShieldItem ? m_equippedShieldItem.GetComponent<ItemWrapper>() : null;

                    if (script == null || script.GetItemType() != ItemType.Shield)
                    {
                        m_equippedShieldItem = GameObject.FindGameObjectWithTag ("ItemManager").GetComponent<ItemIDHolder>().GetItemWithID (30);
                        Debug.LogError ("Resetting null ShieldObject on: " + name);
                    }
                                        
                    ResetEquippedShield();
                }
                
                if (GetEngineObject() == null)
                {
                    script = m_equippedEngineItem ? m_equippedEngineItem.GetComponent<ItemWrapper>() : null;

                    if (script == null || script.GetItemType() != ItemType.Engine)                       
                    {
                        m_equippedEngineItem = GameObject.FindGameObjectWithTag ("ItemManager").GetComponent<ItemIDHolder>().GetItemWithID (60);
                        Debug.LogError ("Resetting null EngineObject on: " + name);
                    }
                    
                    ResetEquippedEngine();
                }
                
                if (GetPlatingObject() == null)
                {
                    script = m_equippedPlatingItem ? m_equippedPlatingItem.GetComponent<ItemWrapper>() : null;

                    if (script == null || script.GetItemType() != ItemType.Plating)
                    {
                        m_equippedPlatingItem = GameObject.FindGameObjectWithTag ("ItemManager").GetComponent<ItemIDHolder>().GetItemWithID (90);
                        Debug.LogError ("Resetting null PlatingObject on: " + name);
                    }
                    
                    ResetEquippedPlating();
                }
                
                yield return new WaitForSeconds (1f);
            }
        }
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
        
        Debug.Log ("ERROR: Player couldn't find attached weapon object");
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
    #endregion
	
    #region Inventory
	public void AddItemToInventoryLocalOnly(ItemWrapper itemWrapper)
	{
		if(!IsInventoryFull())
		{
			m_playerInventory.Add(itemWrapper);
		}
	}

	public void AddItemToInventory(ItemWrapper itemWrapper)
	{
		if(Network.isServer)
		{
			if(!IsInventoryFull())
			{
				m_playerInventory.Add(itemWrapper);
			}
		}
		else
		{
			if(!IsInventoryFull())
			{
				m_playerInventory.Add(itemWrapper);
			}
			networkView.RPC ("TellServerAddItem", RPCMode.Server, itemWrapper.GetItemID());
		}
	}

	[RPC] void TellServerAddItem(int id)
	{
		//AddItemToInventory(item);
		AddItemToInventory(GameObject.FindGameObjectWithTag("ItemManager").GetComponent<ItemIDHolder>().GetItemWithID(id));

	}

	public void RemoveItemFromInventoryLocalOnly(ItemWrapper itemWrapper)
	{
		if(m_playerInventory.Contains(itemWrapper))
		{
			m_playerInventory.Remove(itemWrapper);
		}
	}

	public void RemoveItemFromInventory(ItemWrapper itemWrapper)
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
			networkView.RPC ("TellServerRemoveItem", RPCMode.Server, itemWrapper.GetItemID());
		}
	}

	[RPC] void TellServerRemoveItem(int id)
	{
		RemoveItemFromInventory(GameObject.FindGameObjectWithTag("ItemManager").GetComponent<ItemIDHolder>().GetItemWithID(id));
	}

	public void ClearInventory()
	{
		m_playerInventory.Clear();
	}

	public bool IsInventoryFull()
	{
		return m_playerInventory.Count > 4;
	}

	public ItemWrapper GetItemInSlot(int slot)
	{
        try
        {
            return m_playerInventory[slot];
        }
		
        catch (System.Exception error)
        {
            Debug.LogError (error.Message);
            return null;
        }
	}

	public void EquipItemInSlot(int slot)
	{
		if(Network.isServer)
		{
			switch(m_playerInventory[slot].GetComponent<ItemWrapper>().GetItemType())
			{
				case ItemType.Weapon:
				{
					//If we're told to equip a weapon:
					Debug.Log ("Equipping weapon " + m_playerInventory[slot].GetComponent<ItemWrapper>().GetItemName() + " on player #" + m_owner);

					//Unequip old weapon
					ItemWrapper temp = m_equippedWeaponItem;
					//Destroy object
					Network.Destroy(GetWeaponObject());

					//Equip new weapon
					ItemWrapper newWeapon = m_playerInventory[slot];
					m_equippedWeaponItem = newWeapon;

					if(m_owner == Network.player)
					{
						if(newWeapon.GetComponent<ItemWrapper>().GetItemPrefab().GetComponent<EquipmentWeapon>().GetNeedsLockon())
						{
							Debug.Log ("New weapon is homing, alerting GUI...");
                            networkView.RPC ("PropagateWeaponResetHomingBool", RPCMode.All, true);
						}
						else
						{
							Debug.Log ("Weapon is not homing. Alerting GUI.");
                            networkView.RPC ("PropagateWeaponResetHomingBool", RPCMode.All, false);
						}
					}
				
					GameObject weapon = (GameObject)Network.Instantiate(m_equippedWeaponItem.GetComponent<ItemWrapper>().GetItemPrefab(), this.transform.position, this.transform.rotation, 0);
					weapon.transform.parent = this.transform;
					weapon.transform.localPosition = weapon.GetComponent<EquipmentWeapon>().GetOffset();
					string name = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameStateController>().GetNameFromNetworkPlayer(m_owner);
					weapon.GetComponent<EquipmentWeapon>().ParentWeaponToOwner(name);
					//Broadcast parenting here too
					this.GetComponent<PlayerWeaponScript>().EquipWeapon(weapon);

					//Send relevant info back to client
					networkView.RPC ("ReturnInfoToEquippingClient", m_owner, m_equippedWeaponItem.GetComponent<ItemWrapper>().GetItemID());

					//Take new weapon out of inventory
					RemoveItemFromInventory(m_playerInventory[slot]);

					//Place old weapon into inventory
					AddItemToInventory(temp);
					break;
				}
				case ItemType.Shield:
				{
					Debug.Log ("Equipping shield " + m_playerInventory[slot].GetComponent<ItemWrapper>().GetItemName() + " on player #" + m_owner);

					//Unequip old shield
					ItemWrapper temp = m_equippedShieldItem;
					//Destroy the sheld
					Network.Destroy (GetShieldObject());

					//Equip the new shield
					ItemWrapper newShield = m_playerInventory[slot];
					m_equippedShieldItem = newShield;
					GameObject shield = Network.Instantiate(m_equippedShieldItem.GetComponent<ItemWrapper>().GetItemPrefab(), this.transform.position, this.transform.rotation, 0) as GameObject;
                    if (shield != null)
                    {
                        shield.transform.parent = this.transform;

    					ShieldScript ssc = shield.GetComponent<ShieldScript>();
    					shield.transform.localPosition = ssc.GetOffset();
                    
    					string name = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameStateController>().GetNameFromNetworkPlayer(m_owner);
    					ssc.ParentToPlayer(name);
    					
    					//TODO: Add changes to HPscript here
    					HealthScript HP = this.GetComponent<HealthScript>();
    					Debug.Log ("Attempting to access shield script on item: " + shield.name);
    					HP.EquipNewShield(ssc.GetShieldMaxCharge(), ssc.GetShieldRechargeRate(), ssc.GetShieldRechargeDelay());
                    }

					//Remove new shield from inv
					RemoveItemFromInventory(m_playerInventory[slot]);

					//Place old shield into inv
					AddItemToInventory(temp);
					break;
				}
				case ItemType.Engine:
				{
					Debug.Log ("Equipping engine " + m_playerInventory[slot].GetComponent<ItemWrapper>().GetItemName() + " on player #" + m_owner);

					//Unequip old engine
					ItemWrapper temp = m_equippedEngineItem;
					//Destroy the engine object
					Network.Destroy(GetEngineObject());

					//Equip new shield
					ItemWrapper newEngine = m_playerInventory[slot];
					m_equippedEngineItem = newEngine;
					GameObject engine = Network.Instantiate(m_equippedEngineItem.GetComponent<ItemWrapper>().GetItemPrefab(), this.transform.position, this.transform.rotation, 0) as GameObject;

                    if (engine != null)
                    {
    					engine.transform.parent = this.transform;

    					EngineScript esc = engine.GetComponent<EngineScript>();
    					engine.transform.localPosition = esc.GetOffset();
    				
    					string name = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameStateController>().GetNameFromNetworkPlayer(m_owner);
    					esc.ParentToPlayer(name);

    					//Change our move stats
    					SetMaxShipSpeed(m_baseEngineSpeed + esc.GetMoveSpeed());
                        SetCurrentShipSpeed(m_baseEngineSpeed + esc.GetMoveSpeed());
    					SetRotateSpeed(m_baseEngineTurnSpeed + esc.GetTurnSpeed());	
                    }

					//Remove new engine from inv
					RemoveItemFromInventory(m_playerInventory[slot]);

					//Place old engine into inv
                    AddItemToInventory(temp);
                    
					break;
				}
				case ItemType.Plating:
				{
					Debug.Log ("Equipping plating " + m_playerInventory[slot].GetComponent<ItemWrapper>().GetItemName() + " on player #" + m_owner);

					//Unequip old plating
					ItemWrapper temp = m_equippedPlatingItem;
					//Destroy plating object
					Network.Destroy(GetPlatingObject());

					//Equip new plating
					ItemWrapper newPlating = m_playerInventory[slot];
					m_equippedPlatingItem = newPlating;
					GameObject plating = Network.Instantiate(m_equippedPlatingItem.GetComponent<ItemWrapper>().GetItemPrefab(), this.transform.position, this.transform.rotation, 0) as GameObject;

                    if (plating != null)
                    {
    					plating.transform.parent = this.transform;

    					PlatingScript psc = plating.GetComponent<PlatingScript>();
    					plating.transform.localPosition = psc.GetOffset();
    				
    					string name = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameStateController>().GetNameFromNetworkPlayer(m_owner);
    					psc.ParentToPlayer(name);

    					//Update our HP
    					this.GetComponent<HealthScript>().EquipNewPlating (psc.GetPlatingHealth() + m_baseShipHull);

    					//Update RB
    					rigidbody.mass = m_baseShipWeight + psc.GetPlatingWeight();
                    }

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
			switch(m_playerInventory[slot].GetComponent<ItemWrapper>().GetItemType())
			{
				case ItemType.Weapon:
				{
					//If we're told to equip a weapon:
					
					//Unequip old weapon
					ItemWrapper temp = m_equippedWeaponItem;
					
					//If it's a homing weapon, alert the GUI
					if(m_playerInventory[slot].GetItemPrefab().GetComponent<EquipmentWeapon>().GetNeedsLockon())
					{
                        //TODO: HOMING STUFF AGAIN
						//GameObject.FindGameObjectWithTag("GUIManager").GetComponent<GUIManager>().SetCurrentWeaponNeedsLockon(true);
						Debug.Log ("New weapon is homing, alerting GUI...");
					}
					else
					{
                        //TODO: HOMING STUFF AGAIN
						//GameObject.FindGameObjectWithTag("GUIManager").GetComponent<GUIManager>().SetCurrentWeaponNeedsLockon(false);
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
					ItemWrapper temp = m_equippedShieldItem;

					//Don't destroy, handled by server
					//Equip new
                    ItemWrapper newShield = m_playerInventory[slot];
					m_equippedShieldItem = newShield;

					//Update local inventory
					RemoveItemFromInventoryLocalOnly(m_playerInventory[slot]);
					AddItemToInventoryLocalOnly(temp);
					break;
				}
				case ItemType.Engine:
				{
					//Unequip old
                    ItemWrapper temp = m_equippedEngineItem;
					
					//Don't destroy, handled by server
					//Equip new
                    ItemWrapper newEngine = m_playerInventory[slot];
					m_equippedEngineItem = newEngine;
					
					//Update local inventory
					RemoveItemFromInventoryLocalOnly(m_playerInventory[slot]);
					AddItemToInventoryLocalOnly(temp);
					break;
				}
				case ItemType.Plating:
				{
					//Unequip old
                    ItemWrapper temp = m_equippedPlatingItem;
					
					//Don't destroy, handled by server
					//Equip new
                    ItemWrapper newPlating = m_playerInventory[slot];
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

	[RPC] void TellServerEquipItemInSlot(int slot)
	{
		EquipItemInSlot(slot);
	}

	[RPC] void ReturnInfoToEquippingClient(int weaponID)
	{
        ItemWrapper equipmentObject = GameObject.FindGameObjectWithTag("ItemManager").GetComponent<ItemIDHolder>().GetItemWithID(weaponID);
		m_equippedWeaponItem = equipmentObject;
        
        m_gscCache.PassNewWeaponReferenceToGUI(GetWeaponObject());
	}
    
    /// <summary>
    /// You probably don't want to use this. This will return the weapon, shield, plating or engine based on what number you pass.
    /// The numbers correspond to how they're displayed in the GUI to the player
    /// </summary>
    public ItemWrapper GetEquipmentFromSlot (int slotNumber)
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
    #endregion

    #region Network Sync
	/*float timeSinceLastPacket;
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
	}*/
    #endregion

    #region HomingFunctions 
    void BreakLock()
    {
        m_lockOnTime = 0.0f;
        m_lockedOnTarget = null;
        m_guiCache.PassThroughHomingState(false, false);
        UnsetTargetLock();
    }
    void ResetHomingLockVars()
    {
        m_lockingTarget = null;
        m_lockedOnTarget = null;
        m_lockOnTime = 0.0f;
        m_isLockingOn = false;
        m_guiCache.PassThroughHomingState(false, false);
    }
    void UpdateHomingVars()
    {
        if(m_isLockingOn)
        {
            m_lockOnTime += Time.deltaTime;
            if(m_lockOnTime >= m_reqLockOnTime)
            {
                m_lockedOnTarget = m_lockingTarget;
                m_lockingTarget = null;
                m_isLockingOn = false;
                m_lockOnTime = 0.0f;
                
                //Tell weapon
                SetNewTargetLock(m_lockedOnTarget);
                
                //Update lock state to gui
                m_guiCache.PassThroughHomingState(true, false);
            }
        }
    }
    void UpdateHoming()
    {
        //If we have no target at all, begin looking for one
        if(m_lockedOnTarget == null)
        {
            RaycastHit info;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            int mask = (1 << 11 | 1 << 24);
            bool hit = Physics.Raycast(ray, out info, 200, mask);
            
            //Only do stuff if the raycast actually hits anything
            if(hit)
            {
                //If we're currently trying to lock on to something, make sure we're still hovering and then increase the timer
                //Otherwise, look for a new target
                if(m_isLockingOn)
                {
                    //If the target we're hovering over isn't the target we were trying to lock to, reset the lock
                    //Otherwise, carry on letting the timer tick up (read: do nothing)
                    if(m_lockingTarget != info.collider.attachedRigidbody.gameObject)
                    {
                        ResetHomingLockVars();
                    }
                }
                else
                {
                    m_lockingTarget = info.collider.attachedRigidbody.gameObject;
                    m_isLockingOn = true;
                    m_lockOnTime = 0.0f;
                    m_guiCache.PassThroughHomingState(false, true);
                }
            }
            else
            {
                //Ensure homing vars are unset
                ResetHomingLockVars();
            }
        }
        //If we do have a target, make sure it's still in range
        else
        {
            float distanceToTarget = Vector3.Distance(m_lockedOnTarget.transform.position, this.transform.position);
            if(distanceToTarget > GetWeaponObject().GetComponent<EquipmentWeapon>().GetBulletMaxDistance())
            {
                BreakLock();
            }
        }
    }
    public void SetNewTargetLock(GameObject target)
    {
        GetWeaponObject().GetComponent<EquipmentWeapon>().SetTarget(target);
    }
    
    public void UnsetTargetLock()
    {
        GetWeaponObject().GetComponent<EquipmentWeapon>().UnsetTarget();
    }
    #endregion

    #region Setup, Init and Respawning
	public void InitPlayerOnCShip(GameObject CShip)
	{
		this.m_cShipCache = CShip;
		m_targetPoint = CShip.transform.position;

		networkView.RPC ("PropagateInvincibility", RPCMode.All, false);
		rigidbody.isKinematic = true;

		m_isAnimating = true;
		m_currentDockingState = DockingState.Docked;
		GameObject.FindGameObjectWithTag("GameController").GetComponent<GameStateController>().NotifyLocalPlayerHasDockedAtCShip();
	}

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

	[RPC] void PropagateRespawn()
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
		m_guiCache.ResetPlayerList();
	}

	public void TellPlayerWeAreOwner(NetworkPlayer player)
	{
        m_guiCache.PassThroughPlayerReference(this.gameObject);
		Camera.main.GetComponent<CameraScript>().InitPlayer(this.gameObject);//.m_currentPlayer = this.gameObject;
		m_owner = player;
		TellOtherClientsShipHasOwner(player);

		//Ensure weapon is up to date
		//ResetEquippedWeapon();
	}

	[RPC] void SetOwner(NetworkPlayer player)
	{
		//this.gameObject.AddComponent<RemotePlayerInterp>();
		//this.GetComponent<RemotePlayerInterp>().localPlayer = this.gameObject;
		m_owner = player;
	}
    #endregion

    [RPC] void PropagateIsPlayingSound(bool isPlaying)
    {
        //m_shouldPlaySound = isPlaying;
        
        if(isPlaying)
        {
            this.audio.volume = m_volumeHolder;
            this.audio.Play();
        }
        else
        {
            this.audio.Stop();
        }
    }

	[RPC] void PropagateExplosiveForce (float x, float y, float range, float minForce, float maxForce, int mode = (int) ForceMode.Force)
	{
		// Use the players z position to stop the force causing players to move upwards all the time
		Vector3 position = new Vector3 (x, y, transform.position.z);
		rigidbody.AddCustomExplosionForce (position, range, minForce, maxForce, (ForceMode) mode);
	}

	public void ApplyExplosiveForceOverNetwork (float x, float y, float range, float minForce, float maxForce, ForceMode mode = ForceMode.Force)
	{
		networkView.RPC ("PropagateExplosiveForce", RPCMode.Others, x, y, range, minForce, maxForce, (int) mode);
	}
}

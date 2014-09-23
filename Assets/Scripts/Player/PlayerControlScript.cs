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
    #region Serializable Members
	[SerializeField] bool               m_shouldRecieveInput = true;
	[SerializeField] float              m_baseEngineSpeed = 5.0f;
	[SerializeField] float              m_baseEngineTurnSpeed = 1.0f;
	[SerializeField] int                m_baseShipHull = 25;
	[SerializeField] float              m_baseShipWeight = 0.05f;
	[SerializeField] float              m_maxDockingSpeed = 225f;		//Maxmium docking speed for players
	[SerializeField] float              m_dockRotateSpeed = 3f;			//How quickly to rotate the ship towards the dock
    [SerializeField] float              m_playerStrafeMod = 0.6f;
	[SerializeField] int                m_currentCash = 0;
    #endregion

    #region Internal Members
    // Misc Stuff
    float m_volumeHolder = 1.0f;
    bool m_useController = false;
    Quaternion m_targetAngle;
    bool m_playerIsOutOfBounds = false;

    // Docking Stuff
    float m_dockingTime = 0.0f;                 //Used to determine if the player should continue the docking attempt
    bool m_isInRangeOfCapitalDock = false;
    bool m_isInRangeOfTradingDock = false;
    GameObject m_nearbyShop = null;
    bool m_isAnimating = false;
    DockingState m_currentDockingState = DockingState.NOTDOCKING;
    Vector3 m_targetPoint = Vector3.zero;
    
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
    #endregion

    #region Cached Members
    GameObject m_cShipCache = null;
    GameObject[] m_shops = null;
    GameStateController m_gscCache = null;
    GUIInGameMaster m_guiCache = null;
    #endregion

    #region Get/Set
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
        m_guiCache = GameObject.FindGameObjectWithTag("GUIManager").GetComponent<GUIInGameMaster>();
        base.Awake();
    }

    protected override void Start()
    {
        m_volumeHolder = PlayerPrefs.GetFloat("EffectVolume", 1.0f);

        m_gscCache = GameStateController.Instance();
        ResetShipSpeed();
        //StartCoroutine(EnsureEquipmentValidity());
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
            if(nearestShop != null)
            {
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
            //m_gscCache.EnterCShip();
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
            if(shop != null)
            {
                float distance = Vector3.Distance(shop.transform.position, this.transform.position);
                if (shortestShop == null || distance < shortestDistance)
                {
                    shortestDistance = distance;
                    shortestShop = shop;
                }
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
                Vector3 force = this.transform.up * GetCurrentMomentum() * Time.deltaTime;
                this.rigidbody.AddForce(force);
                
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
        
        Debug.Log ("Player told to stop docking.");
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
            this.GetComponent<EquipmentTypeWeapon>().PlayerRequestsFire();
        }
        
        if (Input.GetMouseButtonUp(0))
        {
            this.GetComponent<EquipmentTypeWeapon>().PlayerReleaseFire();
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
            this.GetComponent<EquipmentTypeWeapon>().PlayerRequestsFire();
        else if (Input.GetAxis("X360Triggers") == 0)
            this.GetComponent<EquipmentTypeWeapon>().PlayerReleaseFire();
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
    [RPC] void PropagateWeaponResetHomingBool(bool state)
	{
		if(m_owner == Network.player)
		{
            m_currentWeaponNeedsLockon = state;
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
            /*if(distanceToTarget > GetWeaponObject().GetComponent<EquipmentWeapon>().GetBulletMaxDistance())
            {
                BreakLock();
            }*/
        }
    }
    public void SetNewTargetLock(GameObject target)
    {
        //GetWeaponObject().GetComponent<EquipmentWeapon>().SetTarget(target);
    }
    
    public void UnsetTargetLock()
    {
        //GetWeaponObject().GetComponent<EquipmentWeapon>().UnsetTarget();
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
        ResetShipSpeed();
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

using UnityEngine;



public sealed class EquipmentTypeWeapon : BaseEquipment 
{

    #region Serializable Properties
    [SerializeField]                        Material            m_fireBeamMat;
    [SerializeField]                        Material            m_iceBeamMat;
    [SerializeField]                        Material            m_earthBeamMat;
    [SerializeField]                        Material            m_lightningBeamMat;
    [SerializeField]                        Material            m_lightBeamMat;
    [SerializeField]                        Material            m_darkBeamMat;
    [SerializeField]                        Material            m_spiritBeamMat;
    [SerializeField]                        Material            m_gravityBeamMat;
    [SerializeField]                        Material            m_airBeamMat;
    [SerializeField]                        Material            m_organicBeamMat;

    [SerializeField]                        GameObject          m_bulletRef;
    [SerializeField]                        GameObject          m_beamRef;
    //[SerializeField]                        Vector3             m_bulletOffset;
    [SerializeField]                        Vector3[]           m_bulletOffsets;

    // Base stats to reset to and start from
    [SerializeField]                        BulletProperties    m_baseBulletStats = null;
    [SerializeField, Range (0.001f, 10f)]   float               m_baseWeaponReloadTime = 0.7f;
    
    // Current stats (base + augment effects)
    public                                  BulletProperties    m_currentBulletStats = new BulletProperties();
    public                                  float               m_currentWeaponReloadTime = 0.0f;

    #endregion


    // Internal usage members
    int m_currentFirePoint = 0;
    Element m_cachedMajorElement = Element.NULL;
    GameObject m_currentHomingTarget = null;
    GameObject m_currentBeam = null;
    [SerializeField]    float m_currentReloadCounter = 0.0f;
    [SerializeField]    float m_currentRechargeDelay = 0.0f;
    bool m_isBeaming = false;
    
    // Cached vars
    GameStateController gscCache;
    
    #region Unity Functions
    void Start()
    {
        gscCache = GameStateController.Instance();
    }
    
    void Update()
    {
        if(m_isBeaming)
        {
            m_currentReloadCounter -= Time.deltaTime;
            if(m_currentReloadCounter <= 0.0f)
            {
                //Stop beaming
                networkView.RPC ("StopFiringBeamAcrossNetwork", RPCMode.All);
            }
        }
        
        if(!m_isBeaming)
        {
            if(m_currentRechargeDelay < m_currentBulletStats.beamRechargeDelay)
            {
                m_currentRechargeDelay += Time.deltaTime;
            }
            else if(m_currentReloadCounter < m_currentWeaponReloadTime)
            {
                m_currentReloadCounter += Time.deltaTime;
            }
        }
    }
    #endregion

    #region Visual Modification functions
    
    Element DetermineMajorityElement()
    {
        int[] counter = new int[10];
        
        for(int i = 0; i < m_augmentSlots.Length; i++)
        {
            if(m_augmentSlots[i] != null)
            {
                switch(m_augmentSlots[i].GetElement())
                {
                    case Element.Fire:
                    {
                        counter[0] += m_augmentSlots[i].GetTier();
                        break;
                    }
                    case Element.Ice:
                    {
                        counter[1] += m_augmentSlots[i].GetTier();
                        break;
                    }
                    case Element.Earth:
                    {
                        counter[2] += m_augmentSlots[i].GetTier();
                        break;
                    }
                    case Element.Lightning:
                    {
                        counter[3] += m_augmentSlots[i].GetTier();
                        break;
                    }
                    case Element.Light:
                    {
                        counter[4] += m_augmentSlots[i].GetTier();
                        break;
                    }
                    case Element.Dark:
                    {
                        counter[5] += m_augmentSlots[i].GetTier();
                        break;
                    }
                    case Element.Spirit:
                    {
                        counter[6] += m_augmentSlots[i].GetTier();
                        break;
                    }
                    case Element.Gravity:
                    {
                        counter[7] += m_augmentSlots[i].GetTier();
                        break;
                    }
                    case Element.Air:
                    {
                        counter[8] += m_augmentSlots[i].GetTier();
                        break;
                    }
                    case Element.Organic:
                    {
                        counter[9] += m_augmentSlots[i].GetTier();
                        break;
                    }
                }
            }
        }
        
        int highestID = -1;
        int highestVal = 0;
        
        for(int i = 0; i < counter.Length; i++)
        {
            if(counter[i] > highestVal)
            {
                highestVal = counter[i];
                highestID = i;
            }
        }
        
        switch(highestID)
        {
            case 0:
            {
                m_cachedMajorElement = Element.Fire;
                return Element.Fire;
            }
            case 1:
            {
                m_cachedMajorElement = Element.Ice;
                return Element.Ice;
            }
            case 2:
            {
                m_cachedMajorElement = Element.Earth;
                return Element.Earth;
            }
            case 3:
            {
                m_cachedMajorElement = Element.Lightning;
                return Element.Lightning;
            }
            case 4:
            {
                m_cachedMajorElement = Element.Light;
                return Element.Light;
            }
            case 5:
            {
                m_cachedMajorElement = Element.Dark;
                return Element.Dark;
            }
            case 6:
            {
                m_cachedMajorElement = Element.Spirit;
                return Element.Spirit;
            }
            case 7:
            {
                m_cachedMajorElement = Element.Gravity;
                return Element.Gravity;
            }
            case 8:
            {
                m_cachedMajorElement = Element.Air;
                return Element.Air;
            }
            case 9:
            {
                m_cachedMajorElement = Element.Organic;
                return Element.Organic;
            }
            default:
            {
                m_cachedMajorElement = Element.NULL;
                return Element.NULL;
            }
        }
    }
    #endregion

    #region Weapon Interaction functions
    public float GetBulletRange()
    {
        return m_currentBulletStats.reach;
    }
    public float GetBulletSpeed()
    {
        return m_currentBulletStats.reach / m_currentBulletStats.lifetime;
    }
    public Element GetMajorityElement()
    {
        if(m_cachedMajorElement == Element.NULL)
            DetermineMajorityElement();
        
        return m_cachedMajorElement;
    }
    public void SetTarget(GameObject target)
    {
        if(target != null)
        {
            m_currentHomingTarget = target;
            networkView.RPC ("PropagateTarget", RPCMode.Others, target.networkView.viewID, false);
        }
        else
        {
            UnsetTarget();
        }
    }
    
    public void UnsetTarget()
    {
        m_currentHomingTarget = null;
        networkView.RPC ("PropagateTarget", RPCMode.Others, networkView.viewID, true);
    }
    
    public bool CheckCanFire()
    {
        if(m_currentBulletStats.isBeam)
        {
            return m_currentReloadCounter > 1.0f;
        }
        else
        {
            return m_currentReloadCounter >= m_currentWeaponReloadTime;
        }
    }
    
    public void ActAsFired()
    {
        if(m_currentBulletStats.isBeam)
        {
            m_isBeaming = true;
        }
        else
        {
            m_currentReloadCounter = 0f;
        }
    }
    
    [RPC] void StopFiringBeamAcrossNetwork()
    {
        AlertBeamWeaponNotFiring();   
    }
    
    public void AlertBeamWeaponNotFiring()
    {
        if(m_currentBulletStats.isBeam)
        {
            m_isBeaming = false;
            
            Network.Destroy(m_currentBeam);
            m_currentBeam = null;
            
            m_currentRechargeDelay = 0.0f;
        }
    }
    
    public void MobRequestsFire()
    {
        if(Network.isServer)
        {
            FireLocalBullet();
        }
    }
    public void PlayerRequestsFire()
    {
        if(Network.isServer)
        {
            FireLocalBullet();
        }
        else
        {
            if(CheckCanFire())
            {
                networkView.RPC ("RequestFireOverNetwork", RPCMode.Server);
                ActAsFired();
            }
        }
    }
    public void PlayerReleaseFire()
    {
        if(Network.isServer)
        {
            if(m_currentBulletStats.isBeam)
                AlertBeamWeaponNotFiring();
        }
        else
        {
            if (m_currentBulletStats.isBeam)
            {
                AlertBeamWeaponNotFiring();
                networkView.RPC ("StopFireOverNetwork", RPCMode.Server);
            }
        }
    }
    
    void FireLocalBullet()
    {
        if(m_currentBulletStats.isBeam)
        {
            if(m_currentReloadCounter > (m_currentWeaponReloadTime * 0.25f))
            {
                ShootBeam();
            }
        }
        else
        {
            if(m_currentReloadCounter >= m_currentWeaponReloadTime)
            {
                ShootBullet();
            }
        }
    }
    public void PlayerRequestsFireNoRecoilCheck()
    {
        if(m_currentBulletStats.isBeam)
        {
            ShootBeam();
        }
        else
        {
            ShootBullet();
        }
    }
    
    [RPC] void StopFireOverNetwork()
    {
        AlertBeamWeaponNotFiring();
    }
    
    [RPC] void RequestFireOverNetwork()
    {
        PlayerRequestsFireNoRecoilCheck();
    }   
    
    void ShootBeam()
    {
        if(!m_isBeaming && m_currentBeam == null)
        {
            GameObject bullet = Network.Instantiate(m_beamRef, transform.position + (transform.rotation * m_bulletOffsets[m_currentFirePoint]), this.transform.rotation, 0) as GameObject;
            Bullet bbs = bullet.GetComponent<Bullet>();
            bbs.CloneProperties(m_currentBulletStats);
            bbs.SetReachModifier(1.0f);
            bbs.SetFirer(gameObject);
            if(m_currentHomingTarget != null)
                bbs.SetHomingTarget(m_currentHomingTarget);
            bbs.SetUpBeam(Vector3.zero, transform.up);
            bullet.transform.parent = this.transform;
            //bullet.transform.parent = this.transform;
            //bullet.transform.localScale = new Vector3(bullet.transform.localScale.x, 0f, bullet.transform.localScale.z);
            //bullet.GetComponent<BeamBulletScript>().SetOffset(m_bulletOffsets[m_currentFirePoint]);
            //bullet.GetComponent<BeamBulletScript>().SetFirer(gameObject);
            
            IncrementFirePos();
            
            //bullet.GetComponent<BeamBulletScript>().ParentBeamToFirer(gscCache.GetNameFromNetworkPlayer(gameObject.GetComponent<PlayerControlScript>().GetOwner()));
            m_currentBeam = bullet;
            m_isBeaming = true;
        }
    }
    
    void ShootBullet()
    {
        //Re-implement multiple fire points / coroutine firing later if augments can give those effects
    
        NetworkViewID id = Network.AllocateViewID();
        networkView.RPC("SpawnBasicBullet", RPCMode.All, id);
        
        m_currentReloadCounter = 0.0f;
    }
    
    void IncrementFirePos()
    {
        ++m_currentFirePoint;
        
        if(m_currentFirePoint >= m_bulletOffsets.Length)
            m_currentFirePoint = 0;
    }
    
    [RPC] void SpawnBasicBullet(NetworkViewID id)
    {
        GameObject bullet = Instantiate(m_bulletRef, transform.position + (transform.rotation * m_bulletOffsets[m_currentFirePoint]), transform.rotation) as GameObject;
        bullet.networkView.viewID = id;
        Bullet bbs = bullet.GetComponent<Bullet>();
        bbs.SetHomingTarget(m_currentHomingTarget);
        bbs.SetFirer(gameObject);
        bbs.CloneProperties(m_currentBulletStats);
        
        IncrementFirePos();
        
        if(transform.rigidbody)
        {
            bbs.SetReachModifier(Vector3.Dot(transform.up, transform.rigidbody.velocity));
        }
        
        if(m_cachedMajorElement == Element.NULL)
            DetermineMajorityElement();
            
        if(m_currentBulletStats.isBeam)
        {
            Material matToSet = null;
            switch(m_cachedMajorElement)
            {
                case Element.Fire:
                {
                    matToSet = m_fireBeamMat;
                    break;
                }
                case Element.Earth:
                {
                    matToSet = m_earthBeamMat;
                    break;
                }
                case Element.Lightning:
                {
                    matToSet = m_lightningBeamMat;
                    break;
                }
                case Element.Light:
                {
                    matToSet = m_lightBeamMat;
                    break;
                }
                case Element.Dark:
                {
                    matToSet = m_darkBeamMat;
                    break;
                }
                case Element.Spirit:
                {
                    matToSet = m_spiritBeamMat;
                    break;
                }
                case Element.Gravity:
                {
                    matToSet = m_gravityBeamMat;
                    break;
                }
                case Element.Air:
                {
                    matToSet = m_airBeamMat;
                    break;
                }
                case Element.Organic:
                {
                    matToSet = m_organicBeamMat;
                    break;
                }
                default:
                {
                    matToSet = m_iceBeamMat;
                    break;
                }
            }
            
            //Debug.Log ("Applying material: " + matToSet);
            //bullet.renderer.material = matToSet;
            bullet.renderer.material = matToSet;
        }
        
        if(m_currentBulletStats.appliedElements.Count > 0)
            bullet.renderer.enabled = false;
    }
    
    [RPC] void PropagateTarget(NetworkViewID id, bool unset)
    {
        if (!unset)
        {
            // Search for the target based on NetworkViewIDs
            NetworkView found = NetworkView.Find(id);
            m_currentHomingTarget = found ? found.gameObject : null;
        }
        
        else
        {
            m_currentHomingTarget = null;
        }
    }
    #endregion

    #region BaseEquipment overrides
    
    /// <summary>
    /// Resets the bullet stats and reload time to their default values.
    /// </summary>
    protected override void ResetToBaseStats()
    {
        m_currentBulletStats.CloneProperties (m_baseBulletStats);
        m_currentBulletStats.appliedElements = new System.Collections.Generic.List<Element>();
        m_currentWeaponReloadTime = m_baseWeaponReloadTime;
        m_cachedMajorElement = Element.NULL;
    }


    /// <summary>
    /// Calculates the current stats based on the equipped augments and their tier.
    /// </summary>
	protected override void CalculateCurrentStats()
    {
        for (int i = 0; i < m_augmentSlots.Length; i++)
        {
            if (m_augmentSlots[i] != null)
            {
                float scalar = ElementalValuesWeapon.TierScalar.GetScalar (m_augmentSlots[i].GetTier());
                Element element = m_augmentSlots[i].GetElement();

                switch (element)
                {
                    case Element.Fire:
                    {
                        ElementResponseFire (scalar);
                        break;
                    }

                    case Element.Ice:
                    {
                        ElementResponseIce (scalar);
                        break;
                    }

                    case Element.Earth:
                    {
                        ElementResponseEarth (scalar);
                        break;
                    }

                    case Element.Lightning:
                    {
                        ElementResponseLightning (scalar);
                        break;
                    }

                    case Element.Light:
                    {
                        ElementResponseLight (scalar);
                        break;
                    }

                    case Element.Dark:
                    {
                        ElementResponseDark (scalar);
                        break;
                    }

                    case Element.Spirit:
                    {
                        ElementResponseSpirit (scalar);
                        break;
                    }

                    case Element.Gravity:
                    {
                        ElementResponseGravity (scalar);
                        break;
                    }

                    case Element.Air:
                    {
                        ElementResponseAir (scalar);
                        break;
                    }

                    case Element.Organic:
                    {
                        ElementResponseOrganic (scalar);
                        break;
                    }
                }

                m_currentBulletStats.appliedElements.Add (element);
            }
        }
    }
   

    protected override void ElementResponseFire (float scalar)
    {
        // Change base effectiveness
        m_currentBulletStats.damage                     += (int) (m_baseBulletStats.damage * ElementalValuesWeapon.Fire.damageMulti * scalar);
        //m_currentWeaponReloadTime                       += m_baseWeaponReloadTime * ElementalValuesWeapon.Fire.reloadTimeMulti * scalar;
        AlterReloadTime(m_baseWeaponReloadTime * ElementalValuesWeapon.Fire.reloadTimeMulti * scalar);

        // AoE effectiveness
        m_currentBulletStats.aoe.isAOE                  = ElementalValuesWeapon.Fire.isAOE;
        m_currentBulletStats.aoe.aoeRange               += m_baseBulletStats.aoe.aoeRange * ElementalValuesWeapon.Fire.aoeRangeMulti * scalar;
        m_currentBulletStats.aoe.aoeMaxDamageRange      += m_baseBulletStats.aoe.aoeMaxDamageRange * ElementalValuesWeapon.Fire.aoeMaxDamageRangeMulti * scalar;
        m_currentBulletStats.aoe.aoeExplosiveForce      += m_baseBulletStats.aoe.aoeExplosiveForce * ElementalValuesWeapon.Fire.aoeExplosiveForceMulti * scalar;
        m_currentBulletStats.aoe.aoeMaxFalloff          += ElementalValuesWeapon.Fire.aoeMaxFalloffInc * scalar;
    }


    protected override void ElementResponseIce (float scalar)
    {
        // Change base effectiveness
        m_currentBulletStats.damage                     += (int) (m_baseBulletStats.damage * ElementalValuesWeapon.Ice.damageMulti * scalar);
        //m_currentWeaponReloadTime                       += m_baseWeaponReloadTime * ElementalValuesWeapon.Ice.reloadTimeMulti * scalar;
        AlterReloadTime(m_baseWeaponReloadTime * ElementalValuesWeapon.Ice.reloadTimeMulti * scalar);

        // Special effects
        m_currentBulletStats.special.slowDuration       += ElementalValuesWeapon.Ice.slowDurationInc * scalar;
    }


    protected override void ElementResponseEarth (float scalar)
    {
        // Change base effectiveness
        m_currentBulletStats.damage                     += (int) (m_baseBulletStats.damage * ElementalValuesWeapon.Earth.damageMulti * scalar);
        m_currentBulletStats.reach                      += m_baseBulletStats.reach * ElementalValuesWeapon.Earth.reachMulti * scalar;
        m_currentBulletStats.lifetime                   += m_baseBulletStats.lifetime * ElementalValuesWeapon.Earth.lifetimeMulti * scalar;
        //m_currentWeaponReloadTime                       += m_baseWeaponReloadTime * ElementalValuesWeapon.Earth.reloadTimeMulti * scalar;       
        AlterReloadTime(m_baseWeaponReloadTime * ElementalValuesWeapon.Earth.reloadTimeMulti * scalar);
    }


    protected override void ElementResponseLightning (float scalar)
    {
        // Change base effectiveness
        m_currentBulletStats.damage                     += (int) (m_baseBulletStats.damage * ElementalValuesWeapon.Lightning.damageMulti * scalar);
        
        // Special effects
        m_currentBulletStats.special.chanceToJump       += ElementalValuesWeapon.Lightning.chanceToJumpInc * scalar;
    }


    protected override void ElementResponseLight (float scalar)
    {
        // Change base effectiveness
        m_currentBulletStats.damage                     += (int) (m_baseBulletStats.damage * ElementalValuesWeapon.Light.damageMulti * scalar);
        //m_currentWeaponReloadTime                       += m_baseWeaponReloadTime * ElementalValuesWeapon.Light.reloadTimeMulti * scalar;
        
        // Enable beam effectiveness
        m_currentBulletStats.isBeam                     = ElementalValuesWeapon.Light.isBeam;
        
        //Turn any bullet reload 'upgrades' into beam reload upgrades
        float diff = m_baseWeaponReloadTime - m_currentWeaponReloadTime;
        m_currentWeaponReloadTime = m_baseWeaponReloadTime + diff;
        
        //Change this
        m_currentWeaponReloadTime += 2.0f;
        m_currentBulletStats.reach += (m_baseBulletStats.reach * ElementalValuesWeapon.Light.reachMulti * scalar);
        
        float amounttoAdd = m_baseWeaponReloadTime * ElementalValuesWeapon.Light.reloadTimeMulti * scalar;
        AlterReloadTime(amounttoAdd);
        
        m_currentBulletStats.lifetime = m_currentWeaponReloadTime * 0.2f;
    }


    protected override void ElementResponseDark (float scalar)
    {
        // Change base effectiveness
        m_currentBulletStats.damage                     += (int) (m_baseBulletStats.damage * ElementalValuesWeapon.Dark.damageMulti * scalar);

        // Increase disability functionality
        m_currentBulletStats.special.chanceToDisable    += ElementalValuesWeapon.Dark.chanceToDisableInc * scalar;
        m_currentBulletStats.special.disableDuration    += ElementalValuesWeapon.Dark.disableDurationInc * scalar;
    }


    protected override void ElementResponseSpirit (float scalar)
    {
        // Change base effectiveness
        m_currentBulletStats.damage                     += (int) (m_baseBulletStats.damage * ElementalValuesWeapon.Spirit.damageMulti * scalar);
        
        // Piercing effectiveness
        m_currentBulletStats.piercing.isPiercing        = ElementalValuesWeapon.Spirit.isPiercing;
        m_currentBulletStats.piercing.maxPiercings      += (int) (m_baseBulletStats.piercing.maxPiercings * ElementalValuesWeapon.Spirit.maxPiercingsMulti * scalar);
        m_currentBulletStats.piercing.pierceModifier    += ElementalValuesWeapon.Spirit.piercingModifierInc * scalar;
    }


    protected override void ElementResponseGravity (float scalar)
    {
        // Change base effectiveness
        m_currentBulletStats.damage                     += (int) (m_baseBulletStats.damage * ElementalValuesWeapon.Gravity.damageMulti * scalar);
        
        // Homing effectiveness
        m_currentBulletStats.homing.isHoming            = ElementalValuesWeapon.Gravity.isHoming;
        m_currentBulletStats.homing.homingRange         += m_baseBulletStats.homing.homingRange * ElementalValuesWeapon.Gravity.homingRangeMulti * scalar;
        m_currentBulletStats.homing.homingTurnRate      += m_baseBulletStats.homing.homingTurnRate * ElementalValuesWeapon.Gravity.homingTurnRateMulti * scalar;
    }


    protected override void ElementResponseAir (float scalar)
    {
        // Change base effectiveness
        m_currentBulletStats.reach                      += m_baseBulletStats.reach * ElementalValuesWeapon.Air.reachMulti * scalar;
        //m_currentWeaponReloadTime                       += m_baseWeaponReloadTime * ElementalValuesWeapon.Air.reloadTimeMulti * scalar;
        AlterReloadTime(m_baseWeaponReloadTime * ElementalValuesWeapon.Air.reloadTimeMulti * scalar);
    }


    protected override void ElementResponseOrganic (float scalar)
    {
        // DoT effectiveness
        m_currentBulletStats.special.dotDuration        += ElementalValuesWeapon.Organic.dotDurationInc * scalar;
        m_currentBulletStats.special.dotEffect          += (int) (m_baseBulletStats.damage * ElementalValuesWeapon.Organic.dotEffectInc * scalar);
    }
    
    void AlterReloadTime(float valueToAdd)
    {
        //Note: valuetoadd will 90% of the time be negative
        if(m_currentBulletStats.isBeam)
        {
            m_currentWeaponReloadTime -= valueToAdd;
        }
        else
        {
            m_currentWeaponReloadTime += valueToAdd;
        }
    }
    
    #endregion
}

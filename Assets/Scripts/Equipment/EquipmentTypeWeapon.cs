using UnityEngine;



public sealed class EquipmentTypeWeapon : BaseEquipment 
{
    #region Serializable Properties

    [SerializeField]                        GameObject          m_bulletRef;
    [SerializeField]                        Vector3             m_bulletOffset;

    // Base stats to reset to and start from
    [SerializeField]                        BulletProperties    m_baseBulletStats = null;
    [SerializeField, Range (0.001f, 10f)]   float               m_baseWeaponReloadTime = 0.7f;
    
    // Current stats (base + augment effects)
    public                                  BulletProperties    m_currentBulletStats = new BulletProperties();
    public                                  float               m_currentWeaponReloadTime = 0.0f;

    #endregion


    // Internal usage members
    GameObject m_currentHomingTarget = null;
    GameObject m_currentBeam = null;
    float m_currentReloadCounter = 0.0f;
    float m_currentRechargeDelay = 0.0f;
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
            if(m_currentReloadCounter < 0.0f)
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

    #region Weapon Interaction functions
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
        m_isBeaming = false;
        
        Network.Destroy(m_currentBeam);
        m_currentBeam = null;
        
        m_currentRechargeDelay = 0.0f;
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
            if(m_currentReloadCounter > 1.0f)
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
            GameObject bullet = Network.Instantiate(m_bulletRef, this.transform.position, this.transform.rotation, 0) as GameObject;
            bullet.transform.parent = this.transform;
            bullet.transform.localScale = new Vector3(bullet.transform.localScale.x, 0f, bullet.transform.localScale.z);
            bullet.GetComponent<BeamBulletScript>().SetOffset(m_bulletOffset);
            bullet.GetComponent<BeamBulletScript>().SetFirer(gameObject);
            
            bullet.GetComponent<BeamBulletScript>().ParentBeamToFirer(gscCache.GetNameFromNetworkPlayer(gameObject.GetComponent<PlayerControlScript>().GetOwner()));
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
    
    [RPC] void SpawnBasicBullet(NetworkViewID id)
    {
        GameObject bullet = Instantiate(m_bulletRef, transform.position + (transform.rotation * m_bulletOffset), transform.rotation) as GameObject;
        bullet.GetComponent<BasicBulletScript>().SetHomingTarget(m_currentHomingTarget);
        
        bullet.networkView.viewID = id;
        
        BasicBulletScript bbs = bullet.GetComponent<BasicBulletScript>();
        bbs.SetFirer(gameObject);
        
        if(transform.rigidbody)
        {
            bbs.SetBulletSpeedModifier(Vector3.Dot(transform.up, transform.rigidbody.velocity));
        }
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
        m_currentWeaponReloadTime = m_baseWeaponReloadTime;
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
        m_currentWeaponReloadTime                       += m_baseWeaponReloadTime * ElementalValuesWeapon.Fire.reloadTimeMulti * scalar;

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
        m_currentWeaponReloadTime                          += m_baseWeaponReloadTime * ElementalValuesWeapon.Ice.reloadTimeMulti * scalar;

        // Special effects
        m_currentBulletStats.special.slowDuration       += ElementalValuesWeapon.Ice.slowDurationInc * scalar;
    }


    protected override void ElementResponseEarth (float scalar)
    {
        // Change base effectiveness
        m_currentBulletStats.damage                     += (int) (m_baseBulletStats.damage * ElementalValuesWeapon.Earth.damageMulti * scalar);
        m_currentBulletStats.reach                      += m_baseBulletStats.reach * ElementalValuesWeapon.Earth.reachMulti * scalar;
        m_currentBulletStats.lifetime                   += m_baseBulletStats.lifetime * ElementalValuesWeapon.Earth.lifetimeMulti * scalar;
        m_currentWeaponReloadTime                          += m_baseWeaponReloadTime * ElementalValuesWeapon.Earth.reloadTimeMulti * scalar;       
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
        m_currentWeaponReloadTime                       += m_baseWeaponReloadTime * ElementalValuesWeapon.Light.reloadTimeMulti * scalar;
        
        // Enable beam effectiveness
        m_currentBulletStats.isBeam                     = ElementalValuesWeapon.Light.isBeam;
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
        m_currentWeaponReloadTime                       += m_baseWeaponReloadTime * ElementalValuesWeapon.Air.reloadTimeMulti * scalar;
    }


    protected override void ElementResponseOrganic (float scalar)
    {
        // DoT effectiveness
        m_currentBulletStats.special.dotDuration        += ElementalValuesWeapon.Organic.dotDurationInc * scalar;
        m_currentBulletStats.special.dotEffect          += (int) (m_baseBulletStats.damage * ElementalValuesWeapon.Organic.dotEffectInc * scalar);
    }
    
    #endregion
}

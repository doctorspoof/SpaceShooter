using UnityEngine;


#region ShieldProperties

[System.Serializable] public sealed class ShieldProperties
{
    // Base stats
    [Range (0, 10000)]      public int      baseMaxShield       = 100;
    [Range (0f, 5f)]        public float    baseRechargeRate    = 1f;
    [Range (0f, 10f)]       public float    baseRechargeDelay   = 1f;
    
    // Fire Burst
                            public bool     shouldFireBurst     = false;
    [Range (0, 1000)]       public int      burstDamage         = 0;
    [Range (0f, 100f)]      public float    burstRange          = 10f;
    [Range (0f, 100f)]      public float    burstMaxDamageRange = 0.1f;
    [Range (0.001f, 1f)]    public float    burstMaxFalloff     = 0.001f;
    
    // Debuff modifier
    [Range (0f, 1f)]        public float    debuffModifier      = 0f;
    
    // Static Shock
                            public bool     canStaticShock      = false;
    [Range (0f, 1f)]        public float    staticChance        = 0f;
    [Range (0, 1000)]       public int      staticDamage        = 100;
    [Range (0f, 100f)]      public float    staticRange         = 10f;
    [Range (0f, 10f)]       public float    staticCooldown      = 2f;
    
    // Flashbang
                            public bool     canFlashbang        = false;
    [Range (0f, 1f)]        public float    flashChance         = 0f;
    [Range (0f, 100f)]      public float    flashRange          = 10f;
    [Range (0f, 10f)]       public float    flashStunDuration   = 2f;
    
    // Absorb  
    [Range (0f, 1f)]        public float    absorbChance        = 0f;
    
    // Invis
    [Range (0f, 1f)]        public float    invisChance         = 0f;
    [Range (0f, 10f)]       public float    invisDuration       = 1f;
    
    // Deflection
    [Range (0f, 1f)]        public float    deflectChance       = 0f;
   


    public ShieldProperties() { }
    public ShieldProperties (ShieldProperties clone)
    {
        CloneProperties (clone);
    }

    public void CloneProperties (ShieldProperties clone)
    {
        if (clone != null)
        {
            // Base stats
            baseMaxShield = clone.baseMaxShield;
            baseRechargeDelay = clone.baseRechargeDelay;
            baseRechargeRate = clone.baseRechargeRate;

            // Fire burst
            shouldFireBurst = clone.shouldFireBurst;
            burstRange = clone.burstRange;
            burstDamage = clone.burstDamage;
            burstMaxFalloff = clone.burstMaxFalloff;

            // Debuff reduction
            debuffModifier = clone.debuffModifier;

            // Passive shock
            canStaticShock = clone.canStaticShock;
            staticChance = clone.staticChance;
            staticRange = clone.staticRange;
            staticDamage = clone.staticDamage;
            staticCooldown = clone.staticCooldown;

            // Disable burst
            canFlashbang = clone.canFlashbang;
            flashChance = clone.flashChance;
            flashRange = clone.flashRange;
            flashStunDuration = clone.flashStunDuration;

            // Absorbtion chance
            absorbChance = clone.absorbChance;

            // Invisibility
            invisChance = clone.invisChance;
            invisDuration = clone.invisDuration;

            // Deflection
            deflectChance = clone.deflectChance;
        }
    }
}

#endregion ShieldProperties



public sealed class EquipmentTypeShield : BaseEquipment 
{
    [SerializeField]    ShieldProperties    m_baseStats     = null;
    [SerializeField]    ShieldProperties    m_currentStats  = new ShieldProperties();
                        
    // Current shield stats
                        int                 m_currentShieldValue = 100;
    [SerializeField]    float               m_currentRechargeDelay = 0.0f;
    [SerializeField]    float               m_currentRechargeFloatCatch = 0.0f;
                        bool                m_shieldStatus = true;
                        
    Element m_cachedMajorElement = Element.NULL;
    Abilities           m_abilities = null;                         //!< A reference to the abilities component of the ship.
    Ship                m_ship = null;
    
    void Awake()
    {
        m_abilities = GetComponent<Abilities>();
        m_ship = GetComponent<Ship>();
        
        base.Awake();
    }

    protected override void ResetToBaseStats ()
    {
        m_currentStats.CloneProperties (m_baseStats);
    }

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
            }
        }
        
        //After all is done, update current stats
        UpdateAbilities();
        m_currentShieldValue = m_currentStats.baseMaxShield;
    }
    
    
    void UpdateAbilities()
    {
        if (m_abilities != null)
        {
            UpdateShieldFirestormCollapse();
            UpdateShieldFlashbangCollapse();
            UpdateShieldSlipawayCollapse();
        }
    }
    
    void UpdateShieldFirestormCollapse()
    {
        if(m_currentStats.shouldFireBurst)
        {
            AbilityShieldCollapseExplode firestorm = m_abilities.Unlock<AbilityShieldCollapseExplode>();
            
            firestorm.SetEffectRange(m_currentStats.burstRange);
            int mask = Layers.GetLayerMask (Layers.player, MaskType.AoE);
            firestorm.SetAoEMask(mask);
            firestorm.SetDamage(m_currentStats.burstDamage);
            firestorm.SetMaxDamRange(m_currentStats.burstMaxDamageRange);
            
            firestorm.SetMaxCooldown(99999f);
            firestorm.ImmediatelyCool();
        }
        else
        {
            m_abilities.Lock<AbilityShieldCollapseExplode>(true);
        }
    }
    void UpdateShieldFlashbangCollapse()
    {
        if(m_currentStats.canFlashbang)
        {
            AbilityShieldCollapseFlash flash = m_abilities.Unlock<AbilityShieldCollapseFlash>();
            
            flash.SetDuration(m_currentStats.flashRange);
            int mask = Layers.GetLayerMask (Layers.player, MaskType.AoE);
            flash.SetAoeMask(mask);
            
            flash.SetMaxCooldown(99999f);
            flash.ImmediatelyCool();
        }
        else
        {
            m_abilities.Lock<AbilityShieldCollapseFlash>(true);
        }
    }
    void UpdateShieldSlipawayCollapse()
    {
        if(m_currentStats.invisChance > 0.0f)
        {
            AbilityShieldCollapseInvisible invis = m_abilities.Unlock<AbilityShieldCollapseInvisible>();
            
            invis.SetDuration(m_currentStats.invisDuration);
            
            invis.SetMaxCooldown(99999f);
            invis.ImmediatelyCool();
        }
        else
        {
            m_abilities.Lock<AbilityShieldCollapseInvisible>(true);
        }
    }
    
    #region Shield Value Interaction
    void Update ()
    {
        if(GetShieldPercentage() < 1.0f)
        {
            if(m_currentRechargeDelay >= m_currentStats.baseRechargeDelay)
            {
                //Recharge
                if(!m_shieldStatus)
                {
                    GetComponent<HealthScript>().ShieldOnOff(true);
                    m_shieldStatus = true;
                }
                
                float rechargeAmount = m_currentStats.baseRechargeRate * Time.deltaTime; 
                int incrAmount = (int)rechargeAmount;
                m_currentRechargeFloatCatch += rechargeAmount - incrAmount;
                
                if((int)m_currentRechargeFloatCatch > 0)
                {
                    int catchInt = (int)m_currentRechargeFloatCatch;
                    m_currentRechargeFloatCatch -= catchInt;
                    
                    incrAmount += catchInt;
                }
                
                m_currentShieldValue += incrAmount;
                m_currentRechargeFloatCatch += m_currentRechargeFloatCatch;
                
                if(m_currentShieldValue >= m_currentStats.baseMaxShield)
                {
                    m_currentShieldValue = m_currentStats.baseMaxShield;
                    OnShieldRaise();
                }
            }
            else
            {
                m_currentRechargeDelay += Time.deltaTime;
            }
        }
    }
    
    /// <summary>
    /// Damages the shield.
    /// </summary>
    /// <returns><c>true</c>, if the shield is still up, <c>false</c> otherwise.</returns>
    /// <param name="damage">The amount of damage to deal.</param>
    public bool DamageShield(int damage)
    {
        m_currentShieldValue -= damage;
        networkView.RPC("PropagateShieldValue", RPCMode.Others, m_currentShieldValue);
        
        if(m_currentShieldValue <= 0)
        {
            OnShieldCollapse();
            m_currentShieldValue = 0;
            m_currentRechargeDelay = 0.0f;
            m_currentRechargeFloatCatch = 0.0f;
            return false;
        }
        else
        {
            m_currentRechargeDelay = 0.0f;
            return true;
        }
    }
    
    public float GetShieldPercentage()
    {
        return (float)m_currentShieldValue / (float)m_currentStats.baseMaxShield;
    }
    public int GetShieldCurrent()
    {
        return m_currentShieldValue;
    }
    public int GetShieldMax()
    {
        return m_currentStats.baseMaxShield;
    }
    public bool GetIsShieldUp()
    {
        return m_currentShieldValue > 0;
    }
    public Element GetMajorityElement()
    {
        if(m_cachedMajorElement == Element.NULL)
            DetermineMajorityElement();
        
        return m_cachedMajorElement;
    }
    public void ResetRechargeDelay()
    {
        m_currentRechargeDelay = 0.0f;
    }
    
    [RPC] void PropagateShieldValue(int value)
    {
        m_currentShieldValue = value;
    }
    
    void OnShieldCollapse()
    {
        //TODO: Add all the appropriate effects
        GetComponent<HealthScript>().ShieldOnOff(false);
        m_shieldStatus = false;
        
        if(m_currentStats.shouldFireBurst)
        {
            m_abilities.ActivateAbility<AbilityShieldCollapseExplode>(gameObject);
        }
        
        if(m_currentStats.canFlashbang)
        {
            float chance = Random.Range(0.0f, 1.0f);
            
            if(chance < m_currentStats.flashChance)
            {
                m_abilities.ActivateAbility<AbilityShieldCollapseFlash>(gameObject);
            }
        }
        
        if(m_currentStats.invisChance > 0.0f)
        {
            float chance = Random.Range(0.0f, 1.0f);
            
            if(chance < m_currentStats.invisChance)
            {
                m_abilities.ActivateAbility<AbilityShieldCollapseInvisible>(gameObject);
            }
        }
    }
    void OnShieldRaise()
    {
        if(m_abilities)
        {
            m_abilities.CoolImmediately<AbilityShieldCollapseExplode>();
            m_abilities.CoolImmediately<AbilityShieldCollapseFlash>();
            m_abilities.CoolImmediately<AbilityShieldCollapseInvisible>();
        }
    }
    
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
    
    #region ElementResponses    
    
    protected override void ElementResponseFire (float scalar)
    {
        // Base stats
        m_currentStats.baseRechargeDelay    += m_baseStats.baseRechargeDelay * ElementalValuesShield.Fire.baseRechargeDelayMulti * scalar;
        
        // Burst effect
        m_currentStats.shouldFireBurst      = ElementalValuesShield.Fire.shouldFireBurst;
        
        m_currentStats.burstDamage          += (int) (m_baseStats.burstDamage * ElementalValuesShield.Fire.burstDamageMulti * scalar);
        m_currentStats.burstRange           += m_baseStats.burstRange * ElementalValuesShield.Fire.burstRangeMulti * scalar;
        m_currentStats.burstMaxDamageRange  += m_baseStats.burstMaxDamageRange * ElementalValuesShield.Fire.burstMaxDamageRangeMulti * scalar;
        m_currentStats.burstMaxFalloff      += ElementalValuesShield.Fire.burstMaxFalloffInc * scalar;
    }


    protected override void ElementResponseIce (float scalar)
    {
        // Base stats
        m_currentStats.baseMaxShield        += (int) (m_baseStats.baseMaxShield * ElementalValuesShield.Ice.baseShieldMulti * scalar);
        
        // Debuff modifier
        m_currentStats.debuffModifier       += ElementalValuesShield.Ice.debuffModifierInc * scalar;
    }
    
    
    protected override void ElementResponseEarth (float scalar)
    {
        // Base stats
        m_currentStats.baseMaxShield        += (int) (m_baseStats.baseMaxShield * ElementalValuesShield.Earth.baseShieldMulti * scalar);
        m_currentStats.baseRechargeDelay    += m_baseStats.baseRechargeDelay * ElementalValuesShield.Earth.baseRechargeDelayMulti * scalar;
    }
    
    
    protected override void ElementResponseLightning (float scalar)
    {
        // Base stats
        m_currentStats.baseMaxShield        += (int) (m_baseStats.baseMaxShield * ElementalValuesShield.Lightning.baseShieldMulti * scalar);

        // Static shock
        m_currentStats.canStaticShock       = ElementalValuesShield.Lightning.canStaticShock;

        m_currentStats.staticChance         += ElementalValuesShield.Lightning.staticChanceInc * scalar;
        m_currentStats.staticRange          += m_baseStats.staticRange * ElementalValuesShield.Lightning.staticRangeMutli * scalar;
        m_currentStats.staticDamage         += (int) (m_baseStats.staticDamage * ElementalValuesShield.Lightning.staticDamageMulti * scalar);
        m_currentStats.staticCooldown       += m_baseStats.staticCooldown * ElementalValuesShield.Lightning.staticCooldownMulti * scalar;
    }
    
    
    protected override void ElementResponseLight (float scalar)
    {
        // Base stats
        m_currentStats.baseRechargeDelay    += m_baseStats.baseRechargeDelay * ElementalValuesShield.Light.baseRechargeDelayMulti * scalar;

        // Flashbang
        m_currentStats.canFlashbang         = ElementalValuesShield.Light.canFlashbang;

        m_currentStats.flashChance          += ElementalValuesShield.Light.flashChanceInc * scalar;
        m_currentStats.flashRange           += m_baseStats.flashRange * ElementalValuesShield.Light.flashRangeMutli * scalar;
        m_currentStats.flashStunDuration    += m_baseStats.flashStunDuration * ElementalValuesShield.Light.flashStunDurationMulti * scalar;
    }


    protected override void ElementResponseDark (float scalar)
    {
        // Base stats
        m_currentStats.baseMaxShield        += (int) (m_baseStats.baseMaxShield * ElementalValuesShield.Dark.baseShieldMulti * scalar);

        // Dark effect
        m_currentStats.absorbChance         += ElementalValuesShield.Dark.absorbChanceInc * scalar;
    }
    
    
    protected override void ElementResponseSpirit (float scalar)
    {
        // Base stats
        m_currentStats.baseRechargeDelay    += m_baseStats.baseRechargeDelay * ElementalValuesShield.Spirit.baseRechargeDelayMulti * scalar;

        // Invisibility
        m_currentStats.invisChance          += ElementalValuesShield.Spirit.invisChanceInc * scalar;
        m_currentStats.invisDuration        += m_baseStats.invisDuration * ElementalValuesShield.Spirit.invisDurationMulti * scalar;
    }

    
    protected override void ElementResponseGravity (float scalar)
    {
        // Deflection
        m_currentStats.deflectChance        += ElementalValuesShield.Gravity.deflectChanceInc * scalar;
    }
    
    
    protected override void ElementResponseAir (float scalar)
    {
        // Base stats
        m_currentStats.baseRechargeDelay    += m_baseStats.baseRechargeDelay * ElementalValuesShield.Air.baseRechargeDelayMulti * scalar;
    }


    protected override void ElementResponseOrganic (float scalar)
    {
        // Base stats
        m_currentStats.baseRechargeRate     += m_baseStats.baseRechargeRate * ElementalValuesShield.Organic.baseRechargeRateMulti * scalar;
    }

    #endregion
}

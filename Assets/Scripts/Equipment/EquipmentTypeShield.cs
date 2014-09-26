using UnityEngine;


#region ShieldProperties

[System.Serializable] public sealed class ShieldProperties
{
    // Base stats
    [Range (0, 10000)]      public int      baseMaxShield       = 100;
    [Range (0f, 100f)]      public float    baseRechargeRate    = 1f;
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
                        ShieldProperties    m_currentStats  = new ShieldProperties();
    

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
    }
    
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

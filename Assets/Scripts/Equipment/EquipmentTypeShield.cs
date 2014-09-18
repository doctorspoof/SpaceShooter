using UnityEngine;



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
        
    }
    
    #region ElementResponses
    protected override void ElementResponseAir (float scalar)
    {
        throw new System.NotImplementedException ();
    }
    
    protected override void ElementResponseFire (float scalar)
    {
        throw new System.NotImplementedException ();
    }
    
    protected override void ElementResponseDark (float scalar)
    {
        throw new System.NotImplementedException ();
    }
    
    protected override void ElementResponseEarth (float scalar)
    {
        throw new System.NotImplementedException ();
    }
    
    protected override void ElementResponseGravity (float scalar)
    {
        throw new System.NotImplementedException ();
    }
    
    protected override void ElementResponseIce (float scalar)
    {
        throw new System.NotImplementedException ();
    }
    
    protected override void ElementResponseLight (float scalar)
    {
        throw new System.NotImplementedException ();
    }
    
    protected override void ElementResponseLightning (float scalar)
    {
        throw new System.NotImplementedException ();
    }
    
    protected override void ElementResponseOrganic (float scalar)
    {
        throw new System.NotImplementedException ();
    }
    
    protected override void ElementResponseSpirit (float scalar)
    {
        throw new System.NotImplementedException ();
    }
    #endregion
}

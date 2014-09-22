using UnityEngine;



/// <summary>
/// Contains all of the unique statistics of the plating.
/// </summary>
[System.Serializable] public sealed class PlatingProperties
{
    [Range (0, 10000)]  public int      hp                  = 100;      //!< How much HP the plating should have.
    [Range (0f, 100f)]  public float    mass                = 1f;       //!< How much the plating should weigh.

    [Range (0f, 1f)]    public float    regen               = 0f;       //!< How much HP to regenerate a second (percentage).
    [Range (0f, 1f)]    public float    returnDamage        = 0f;       //!< How much damage should be returned to aggressors (percentage).
    [Range (0f, 10f)]   public float    slowDuration        = 0f;       //!< How long to slow targets which physically touch the plating.
    [Range (0f, 1f)]    public float    chanceToJump        = 0f;       //!< The chance of debuffs passing onto nearby enemies (percentage).
    [Range (0f, 1f)]    public float    chanceToCleanse     = 0f;       //!< The chance of cleansing the plating of debuffs (percentage).
    [Range (0f, 1f)]    public float    lifesteal           = 0f;       //!< How much of a lifesteal effect the plating should have (percentage).
    [Range (0f, 1f)]    public float    chanceToEthereal    = 0f;       //!< The chance of turning ethereal on hit (percentage).
    [Range (0f, 10f)]   public float    etherealDuration    = 2f;       //!< How long stay ethereal once triggered.

                        public bool     slowsIncoming       = false;    //!< Whether to slow incoming projectiles or not.
    [Range (0f, 1f)]    public float    speedReduction      = 0f;       //!< How much to reduce the projectiles speed by (percentage).
}



public sealed class EquipmentTypePlating : BaseEquipment 
{
    protected override void CalculateCurrentStats ()
    {
        //throw new System.NotImplementedException ();
    }
    
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
    
    protected override void ResetToBaseStats ()
    {
        //throw new System.NotImplementedException ();
    }
}

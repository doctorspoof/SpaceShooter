using UnityEngine;



/// <summary>
/// Contains all statistics for the engines.
/// </summary>
[System.Serializable] public sealed class EngineProperties
{
    // Base movement
    [Range (0f, 100f)]      public float            forwardSpeed        = 25f;      //!< The maximum speed the ship can move.
    [Range (0f, 100f)]      public float            turnSpeed           = 25f;      //!< The maximum rotation speed of the ship.
    [Range (0f, 100f)]      public float            strafeSpeed         = 25f;      //!< The maximum strafing speed of the ship.

    // Afterburners
    [Range (0f, 100f)]      public float            burnerSpeed         = 25;       //!< The extra speed given by the afterburners.
    [Range (0f, 10f)]       public float            burnerLength        = 2.5f;     //!< How long the afterburner can last.
    [Range (0f, 10f)]       public float            burnerRechargeRate  = 5f;       //!< How quickly the afterburners recharge.

    // Gravity control
                            public bool             gravityControl      = false;    //!< Unlocks the usage of gravity control.
    [Range (0f, 1f)]        public float            maxGravityChance    = 0f;       //!< How much the gravity effect can be increased or reduced (-100% to 100%).

    // Teleports
                            public bool             shortTeleport       = false;    //!< Unlocks the short-range teleport.
    [Range (0f, 10f)]       public float            shortTeleRange      = 5f;       //!< How far the short-range teleport reaches.
    [Range (0f, 120f)]      public float            shortTeleCooldown   = 60f;      //!< How long to wait before using the teleport again.

                            public bool             longTeleport        = false;    //!< Unlocks the long-range teleport.
    [Range (0f, 100f)]      public float            longTeleRange       = 50f;      //!< How far the long-range teleport reaches.
    [Range (0f, 120f)]      public float            longTeleCooldown    = 60f;      //!< How long to wait before using the teleport again.
}



[RequireComponent (typeof (Ship))]
public sealed class EquipmentTypeEngine : BaseEquipment 
{
    protected override void ResetToBaseStats()
    {
        //throw new System.NotImplementedException ();
    }


    protected override void CalculateCurrentStats()
    {
        //throw new System.NotImplementedException ();
    }


    protected override void ElementResponseFire (float scalar)
    {
        throw new System.NotImplementedException ();
    }


    protected override void ElementResponseIce (float scalar)
    {
        throw new System.NotImplementedException ();
    }


    protected override void ElementResponseEarth (float scalar)
    {
        throw new System.NotImplementedException ();
    }


    protected override void ElementResponseLightning (float scalar)
    {
        throw new System.NotImplementedException ();
    }

    
    protected override void ElementResponseLight (float scalar)
    {
        throw new System.NotImplementedException ();
    }


    protected override void ElementResponseDark (float scalar)
    {
        throw new System.NotImplementedException ();
    }


    protected override void ElementResponseSpirit (float scalar)
    {
        throw new System.NotImplementedException ();
    }


    protected override void ElementResponseGravity (float scalar)
    {
        throw new System.NotImplementedException ();
    }


    protected override void ElementResponseAir (float scalar)
    {
        throw new System.NotImplementedException ();
    }


    protected override void ElementResponseOrganic (float scalar)
    {
        throw new System.NotImplementedException ();
    }
}

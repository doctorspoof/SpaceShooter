using UnityEngine;


#region Engine Properties

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
    [Range (0f, 1f)]        public float            maxGravityChange    = 0f;       //!< How much the gravity effect can be increased or reduced (-100% to 100%).

    // Teleports
                            public bool             shortTeleport       = false;    //!< Unlocks the short-range teleport.
    [Range (0f, 10f)]       public float            shortTeleRange      = 5f;       //!< How far the short-range teleport reaches.
    [Range (0f, 120f)]      public float            shortTeleCooldown   = 60f;      //!< How long to wait before using the teleport again.

                            public bool             longTeleport        = false;    //!< Unlocks the long-range teleport.
    [Range (0f, 100f)]      public float            longTeleRange       = 50f;      //!< How far the long-range teleport reaches.
    [Range (0f, 120f)]      public float            longTeleCooldown    = 60f;      //!< How long to wait before using the teleport again.
    
    
    public void CloneProperties (EngineProperties clone)
    {
        if (clone != null)
        {
            // Base movement
            forwardSpeed = clone.forwardSpeed;
            turnSpeed = clone.turnSpeed;
            strafeSpeed = clone.strafeSpeed;
            
            // Afterburners
            burnerSpeed = clone.burnerSpeed;
            burnerLength = clone.burnerLength;
            burnerRechargeRate = clone.burnerRechargeRate;
            
            // Gravity control
            gravityControl = clone.gravityControl;
            maxGravityChange = clone.maxGravityChange;
            
            // Teleports
            shortTeleport = clone.shortTeleport;
            shortTeleRange = clone.shortTeleRange;
            shortTeleCooldown = clone.shortTeleCooldown;
            
            longTeleport = clone.longTeleport;
            longTeleRange = clone.longTeleRange;
            longTeleCooldown = clone.longTeleCooldown;
        }
    }
}

#endregion


[RequireComponent (typeof (Ship))]
public sealed class EquipmentTypeEngine : BaseEquipment 
{
    [SerializeField]    EngineProperties    m_baseStats = null;                         //!< Contains the base stats of the engine.
                        EngineProperties    m_currentStats = new EngineProperties();    //!< Contains the current calculated stats of the engine.


    #region Elemental responses

    protected override void ResetToBaseStats()
    {
        m_currentStats.CloneProperties (m_baseStats);
    }


    protected override void CalculateCurrentStats()
    {
        for (int i = 0; i < m_augmentSlots.Length; i++)
        {
            if (m_augmentSlots[i] != null)
            {
                float scalar = ElementalValuesEngine.TierScalar.GetScalar (m_augmentSlots[i].GetTier());
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
    
    #endregion Elemental responses
}

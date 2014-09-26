using UnityEngine;


#region Engine Properties

/// <summary>
/// Contains all statistics for the engines.
/// </summary>
[System.Serializable] public sealed class EngineProperties
{
    // Base movement
    [Range (0f, 1000f)]     public float            forwardSpeed        = 250f;     //!< The maximum speed the ship can move.
    [Range (0f, 1000f)]     public float            turnSpeed           = 250f;     //!< The maximum rotation speed of the ship.
    [Range (0f, 1000f)]     public float            strafeSpeed         = 250f;     //!< The maximum strafing speed of the ship.

    // Afterburners
    [Range (0f, 1000f)]     public float            burnerSpeed         = 250f;     //!< The extra speed given by the afterburners.
    [Range (0f, 10f)]       public float            burnerLength        = 2.5f;     //!< How long the afterburner can last.
    [Range (0f, 10f)]       public float            burnerRechargeTime  = 5f;       //!< How quickly the afterburners recharge.

    // Gravity control
                            public bool             hasGravityControl   = false;    //!< Unlocks the usage of gravity control.
    [Range (0f, 1f)]        public float            maxGravityChange    = 0f;       //!< How much the gravity effect can be increased or reduced (-100% to 100%).

    // Teleports
                            public bool             hasShortTeleport    = false;    //!< Unlocks the short-range teleport.
    [Range (0f, 10f)]       public float            shortTeleRange      = 5f;       //!< How far the short-range teleport reaches.
    [Range (0f, 120f)]      public float            shortTeleCooldown   = 60f;      //!< How long to wait before using the teleport again.

                            public bool             hasLongTeleport     = false;    //!< Unlocks the long-range teleport.
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
            burnerRechargeTime = clone.burnerRechargeTime;
            
            // Gravity control
            hasGravityControl = clone.hasGravityControl;
            maxGravityChange = clone.maxGravityChange;
            
            // Teleports
            hasShortTeleport = clone.hasShortTeleport;
            shortTeleRange = clone.shortTeleRange;
            shortTeleCooldown = clone.shortTeleCooldown;
            
            hasLongTeleport = clone.hasLongTeleport;
            longTeleRange = clone.longTeleRange;
            longTeleCooldown = clone.longTeleCooldown;
        }
    }
}

#endregion


[RequireComponent (typeof (Abilities)), RequireComponent (typeof (Ship))]
public sealed class EquipmentTypeEngine : BaseEquipment 
{
    #region Data members

    [SerializeField]    EngineProperties    m_baseStats = null;                         //!< Contains the base stats of the engine.
                        EngineProperties    m_currentStats = new EngineProperties();    //!< Contains the current calculated stats of the engine.
    
                        Abilities           m_abilities = null;                         //!< A reference to the abilities component of the ship.
                        Ship                m_ship = null;                              //!< A reference to the ship used in updating the speeds available.

    #endregion


    #region Behaviour functions

    protected override void Awake()
    {
        // Abilities is guaranteed by the RequireComponent() attribute.
        m_abilities = GetComponent<Abilities>();
        
        // Ship is guaranteed by the RequireComponent() attribute.
        m_ship = GetComponent<Ship>();

        base.Awake();
    }

    #endregion


    #region Stats & ability configuration

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

        UpdateShipValues();
        UpdateAbilities();
    }


    void UpdateShipValues()
    {
        if (m_ship != null)
        {
            m_ship.SetMaxShipSpeed (m_currentStats.forwardSpeed);
            m_ship.SetRotateSpeed (m_currentStats.turnSpeed);
            m_ship.SetStrafeSpeed (m_currentStats.strafeSpeed);

            m_ship.SetAfterburnerSpeed (m_currentStats.burnerSpeed);
            m_ship.SetAfterburnerLength (m_currentStats.burnerLength);
            m_ship.SetAfterburnerRechargeTime (m_currentStats.burnerRechargeTime);
        }
    }


    void UpdateAbilities()
    {
        if (m_abilities != null)
        {
            UpdateAbilityGravityControl();
            UpdateAbilityTeleportRandom();
            UpdateAbilityTeleportTargetted();
        }
    }


    void UpdateAbilityGravityControl()
    {
        if (m_currentStats.hasGravityControl)
        {
            // Unlock the ability
            AbilityGravityControl gravity = m_abilities.Unlock<AbilityGravityControl>();
            
            // Set the maximum change attribute
            gravity.SetMaxChange (m_currentStats.maxGravityChange);
        }

        else
        {
            m_abilities.Lock<AbilityGravityControl> (true);
        }
    }


    void UpdateAbilityTeleportRandom()
    {
        if (m_currentStats.hasLongTeleport)
        {
            // Unlock the ability
            AbilityTeleportRandom teleport = m_abilities.Unlock<AbilityTeleportRandom>();

            // Update the range and cooldown
            teleport.SetRange (m_currentStats.longTeleRange);
            teleport.SetMaxCooldown (m_currentStats.longTeleCooldown);
        }

        else
        {
            m_abilities.Lock<AbilityTeleportRandom> (true);
        }
    }


    void UpdateAbilityTeleportTargetted()
    {
        if (m_currentStats.hasShortTeleport)
        {
            // Unlock the ability
            AbilityTeleportTargetted teleport = m_abilities.Unlock<AbilityTeleportTargetted>();
            
            // Update the range and cooldown
            teleport.SetRange (m_currentStats.shortTeleRange);
            teleport.SetMaxCooldown (m_currentStats.shortTeleCooldown);
        }

        else
        {
            m_abilities.Lock<AbilityTeleportTargetted> (true);
        }
    }

    #endregion Stats & ability configuration

      
    #region Elemental responses


    protected override void ElementResponseFire (float scalar)
    {
        // Base movement
        m_currentStats.forwardSpeed += m_baseStats.forwardSpeed * ElementalValuesEngine.Fire.speedMulti * scalar;
        m_currentStats.turnSpeed += m_baseStats.turnSpeed * ElementalValuesEngine.Fire.turnMulti * scalar;
        m_currentStats.strafeSpeed += m_baseStats.strafeSpeed * ElementalValuesEngine.Fire.strafeMulti * scalar;      
    }


    protected override void ElementResponseIce (float scalar)
    {
        // Afterburners
        m_currentStats.burnerLength += m_baseStats.burnerLength * ElementalValuesEngine.Ice.burnerLengthMulti * scalar;
    }


    protected override void ElementResponseEarth (float scalar)
    {
        // Afterburners
        m_currentStats.burnerSpeed += m_baseStats.burnerSpeed * ElementalValuesEngine.Earth.burnerSpeedMulti * scalar;
        m_currentStats.burnerLength += m_baseStats.burnerLength * ElementalValuesEngine.Earth.burnerLengthMulti * scalar;
    }


    protected override void ElementResponseLightning (float scalar)
    {
        // Afterburners
        m_currentStats.burnerSpeed += m_baseStats.burnerSpeed * ElementalValuesEngine.Lightning.burnerSpeedMulti * scalar;
        m_currentStats.burnerLength += m_baseStats.burnerLength * ElementalValuesEngine.Lightning.burnerLengthMulti * scalar;
    }

    
    protected override void ElementResponseLight (float scalar)
    {
        // Base movement
        m_currentStats.forwardSpeed += m_baseStats.forwardSpeed * ElementalValuesEngine.Light.speedMulti * scalar;
        m_currentStats.turnSpeed += m_baseStats.turnSpeed * ElementalValuesEngine.Light.turnMulti * scalar;
        m_currentStats.strafeSpeed += m_baseStats.strafeSpeed * ElementalValuesEngine.Light.strafeMulti * scalar;     
    
        // Afterburners
        m_currentStats.burnerSpeed += m_baseStats.burnerSpeed * ElementalValuesEngine.Light.burnerSpeedMulti * scalar;
    }


    protected override void ElementResponseDark (float scalar)
    {
        // Teleports
        m_currentStats.hasLongTeleport = ElementalValuesEngine.Dark.hasLongTeleport;
        
        m_currentStats.longTeleRange += m_baseStats.longTeleRange * ElementalValuesEngine.Dark.longRangeMulti * scalar;
        m_currentStats.longTeleCooldown += m_baseStats.longTeleCooldown * ElementalValuesEngine.Dark.longCooldownMulti * scalar;
    }


    protected override void ElementResponseSpirit (float scalar)
    {
        // Teleports
        m_currentStats.hasShortTeleport = ElementalValuesEngine.Spirit.hasShortTeleport;
        
        m_currentStats.shortTeleRange += m_baseStats.shortTeleRange * ElementalValuesEngine.Spirit.shortRangeMulti * scalar;
        m_currentStats.shortTeleCooldown += m_baseStats.shortTeleCooldown * ElementalValuesEngine.Spirit.shortCooldownMulti * scalar;
    }


    protected override void ElementResponseGravity (float scalar)
    {
        // Gravity control
        m_currentStats.hasGravityControl = ElementalValuesEngine.Gravity.hasGravityControl;
        
        m_currentStats.maxGravityChange += ElementalValuesEngine.Gravity.maxGravityChangeInc * scalar;
    }


    protected override void ElementResponseAir (float scalar)
    {
        // Base movement
        m_currentStats.turnSpeed += m_baseStats.turnSpeed * ElementalValuesEngine.Air.turnMulti * scalar;
        m_currentStats.strafeSpeed += m_baseStats.strafeSpeed * ElementalValuesEngine.Air.strafeMulti * scalar;
    }


    protected override void ElementResponseOrganic (float scalar)
    {
        // Afterburners
        m_currentStats.burnerRechargeTime += m_baseStats.burnerRechargeTime * ElementalValuesEngine.Organic.burnerRechargeMulti * scalar;
    }
    
    #endregion Elemental responses
}
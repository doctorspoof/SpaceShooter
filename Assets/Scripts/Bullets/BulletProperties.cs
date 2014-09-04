using UnityEngine;
using System.Collections.Generic;



#region Attribute classes

/// <summary>
/// Contains each AoE attribute which bullets need to perform AoE functionality.
/// </summary>
[System.Serializable] public sealed class AOEAttributes
{
                            public bool         isAOE = false;          //!< Indicates wether the bullet should cause AoE damage or not.
    [Range (0f, 100f)]      public float        aoeRange = 0f;          //!< How far the AoE on the bullet should be.
    [Range (0f, 100f)]      public float        aoeMaxDamageRange = 0f; //!< The distance between the explosion point and the enemy in which there is no damage fall off.
    [Range (0f, 1000f)]     public float        aoeExplosiveForce = 0f; //!< The maximum amount of explosive force to apply to the enemy.
    [Range (0.001f, 1f)]    public float        aoeMaxFalloff = 0.001f; //!< How much force and damage can be reduced by distance from the AoE.
    
    
    /// <summary>
    /// Initializes a new instance of the <see cref="AOEAttributes"/> class.
    /// </summary>
    /// <param name="copy">The object to copy the data from.</param>
    public AOEAttributes (AOEAttributes copy)
    {
        if (copy == null)
        {
            Debug.LogError ("Attempt to create AOEAttributes using the copy constructor, copy == null");
        }
        
        else
        {
            isAOE = copy.isAOE;
            aoeRange = copy.aoeRange;
            aoeMaxDamageRange = copy.aoeMaxDamageRange;
            aoeExplosiveForce = copy.aoeExplosiveForce;
            aoeMaxFalloff = copy.aoeMaxFalloff;
        }
    }
    
    public AOEAttributes () { }
}



/// <summary>
/// Contains each homing attribute which bullets need to perform piercing functionality.
/// </summary>
[System.Serializable] public sealed class HomingAttributes
{
                            public bool         isHoming = false;       //!< Indicates whether the bullet should home in on targets.
    [Range (0f, 100f)]      public float        homingRange = 0f;       //!< If no target is present, this value will be used in an OverlapSphere().
    [Range (0f, 100f)]      public float        homingTurnRate = 0f;    //!< How quickly the bullet can home in on targets.
    [HideInInspector]       public GameObject   target = null;          //!< Used to provide a homing target for the bullet.
    
    
    /// <summary>
    /// Initializes a new instance of the <see cref="HomingAttributes"/> class.
    /// </summary>
    /// <param name="copy">The object to copy the data from.</param>
    public HomingAttributes (HomingAttributes copy)
    {
        if (copy == null)
        {
            Debug.LogError ("Attempt to create HomingAttributes using the copy constructor, copy == null");
        }
        
        else
        {
            isHoming = copy.isHoming;
            homingRange = copy.homingRange;
            homingTurnRate = copy.homingTurnRate;
            target = copy.target;
        }
    }
    
    public HomingAttributes () { }
}



/// <summary>
/// Contains each piercing attribute which bullets need to perform piercing functionality.
/// </summary>
[System.Serializable] public sealed class PiercingAttributes
{
                            public bool     isPiercing = false;     //!< Indicates whether the bullet should be piercing or not.
    [Range (0, 100)]        public int      maxPiercings = 0;       //!< How many times the bullet can pierce.
    [Range (0f, 1f)]        public float    pierceModifier = 0f;    //!< How much to reduce speed and damage by on pierce.
    
    
    /// <summary>
    /// Initializes a new instance of the <see cref="PiercingAttributes"/> class.
    /// </summary>
    /// <param name="copy">The object to copy the data from.</param>
    public PiercingAttributes (PiercingAttributes copy)
    {
        if (copy == null)
        {
            Debug.LogError ("Attempt to create PiercingAttributes using the copy constructor, copy == null");
        }
        
        else
        {
            isPiercing = copy.isPiercing;
            maxPiercings = copy.maxPiercings;
            pierceModifier = copy.pierceModifier;
        }
    }
    
    public PiercingAttributes () { }
}



/// <summary>
/// Contains each debuff/special attribute which bullets need to inform the health script about.
/// </summary>
[System.Serializable] public sealed class SpecialAttributes
{
    [Range (0f, 1f)]        public float    chanceToJump = 0f;      //!< How much of a percentage chance to jump to a another target on hit
    [Range (0f, 1f)]        public float    chanceToDisable = 0f;   //!< How much of a percentage chance to disable the target on hit the bullet should have.
    [Range (0f, 100f)]      public float    disableEffect = 0f;     //!< How many seconds to disable the target for on hit.
    [Range (0f, 100f)]      public float    slowEffect = 0f;        //!< How long to slow the enemy for on hit.
    [Range (0f, 100f)]      public float    dotDuration = 0f;       //!< How long damage over time should be applied.
    [Range (0f, 100f)]      public float    dotEffect = 0f;         //!< How much damage per second should be dealt by damage over time.


    /// <summary>
    /// Initializes a new instance of the <see cref="SpecialAttributes"/> class.
    /// </summary>
    /// <param name="copy">The object to copy the data from.</param>
    public SpecialAttributes (SpecialAttributes copy)
    {
        if (copy == null)
        {
            Debug.LogError ("Attempt to create SpecialAttributes using the copy constructor, copy == null");
        }

        else
        {
            chanceToJump = copy.chanceToJump;
            chanceToDisable = copy.chanceToDisable;
            disableEffect = copy.disableEffect;
            slowEffect = copy.slowEffect;
            dotDuration = copy.dotDuration;
            dotEffect = copy.dotEffect;
        }
    }
    
    public SpecialAttributes () { }
}

#endregion


#region Bullet properties

/// <summary>
/// A class to be used as a struct to contain all properties which a bullet needs to function with the flexible augment system.
/// </summary>
public sealed class BulletProperties
{
    #region Member variables

    // Base mechanics
                            public bool                 isBeam = false;                         //!< Indicates whether the bullet should act as a beam or not.
    [Range (0, 1000)]       public int                  damage = 0;                             //!< The damage that a bullet should deal with no modifiers.
    [Range (0f, 100f)]      public float                reach = 0f;                             //!< The desired length the bullet should reach/travel.
    [Range (0.001f, 100f)]  public float                lifetime = 1f;                          //!< How long it should take for the bullet to reach its destination.
        
    // Attribute collections
                            public AOEAttributes        aoe = null;                             //!< Contains each AoE attribute.
                            public HomingAttributes     homing = null;                          //!< Contains each homing attribute.
                            public PiercingAttributes   piercing = null;                        //!< Contains each piercing attribute.
                            public SpecialAttributes    special = null;                         //!< Contains each special attribute.

    // Hidden members
    [HideInInspector]       public GameObject           firer = null;                           //!< The GameObject of the weapon that fired the bullet.
    [HideInInspector]       public List<Element>        appliedElements = new List<Element>(0); //!< What elements have been applied to the bullet.

    #endregion


    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="BulletProperties"/> class using default values.
    /// </summary>
    public BulletProperties() { }


    /// <summary>
    /// Initializes a new instance of the <see cref="BulletProperties"/> class. Note: this will leave unnecessary values null, so if aoe.isAOE == false then
    /// aoe will remain null. This is to save on memory and initialisation time. If you merely want to share a pointer then use the operator= constructor.
    /// </summary>
    /// <param name="rhs">The BulletProperties to take values from.</param>
    public BulletProperties (BulletProperties copy)
    {
        if (copy == null)
        {
            Debug.LogError ("Attempt to create BulletProperties using the copy constructor, copy == null");
        }

        else
        {
            // Copy base values
            isBeam = copy.isBeam;
            damage = copy.damage;
            reach = copy.reach;
            lifetime = copy.lifetime;

            // Don't bother copying if the bullet won't cause AoE damage
            if (copy.aoe != null && copy.aoe.isAOE)
            {
                aoe = new AOEAttributes (copy.aoe);
            }

            // Again, no point in wasting memory unnecessarily
            if (copy.homing != null && copy.homing.isHoming)
            {
                homing = new HomingAttributes (copy.homing);
            }

            // Save some memory if possible
            if (copy.piercing != null && copy.piercing.isPiercing)
            {
                piercing = new PiercingAttributes (copy.piercing);
            }

            // Unfortunately there isn't a good way to check special attributes yet so just make a copy
            if (copy.special != null)
            {
                special = new SpecialAttributes (copy.special);
            }
        }
    }

    #endregion


    #region Functions

    /// <summary>
    /// Unlike the copy constructor CloneProperties will copy all values regardless of whether they're necessary or not.
    /// </summary>
    /// <param name="clone">The properties to clone.</param>
    public void CloneProperties (BulletProperties clone)
    {
        if (clone == null)
        {
            Debug.LogError ("Attempt to clone a BulletProperties object, clone == null");
        }

        else
        {
            isBeam = clone.isBeam;
            damage = clone.damage;
            reach = clone.reach;
            lifetime = clone.lifetime;

            aoe = new AOEAttributes (clone.aoe);
            homing = new HomingAttributes (clone.homing);
            piercing = new PiercingAttributes (clone.piercing);
            special = new SpecialAttributes (clone.special);
          
            firer = clone.firer;
            appliedElements = clone.appliedElements;
        }
    }

    #endregion
}

#endregion
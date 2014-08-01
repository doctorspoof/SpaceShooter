using UnityEngine;
using System.Collections.Generic;



/// <summary>
/// Contains each AoE attribute which bullets need to perform AoE functionality.
/// </summary>
[System.Serializable] public sealed class AOEAttributes
{
                            public bool         isAOE = false;          // Indicates wether the bullet should cause AoE damage or not
    [Range (0f, 100f)]      public float        aoeRange = 0f;          // How far the AoE on the bullet should be
    [Range (0f, 100f)]      public float        aoeMaxDamageRange = 0f; // The distance between the explosion point and the enemy in which there is no damage fall off
    [Range (0f, 1000f)]     public float        aoeExplosiveForce = 0f; // The maximum amount of explosive force to apply to the enemy
    [Range (0.001f, 1f)]    public float        aoeMaxFalloff = 0.001f; // How much force and damage can be reduced by distance from the AoE
}



/// <summary>
/// Contains each homing attribute which bullets need to perform piercing functionality.
/// </summary>
[System.Serializable] public sealed class HomingAttributes
{
                            public bool         isHoming = false;       // Indicates whether the bullet should home in on targets
    [Range (0f, 100f)]      public float        homingTurnRate = 0f;    // How quickly the bullet can home in on targets
    [HideInInspector]       public GameObject   target = null;          // Used to provide a homing target for the bullet
}



/// <summary>
/// Contains each piercing attribute which bullets need to perform piercing functionality.
/// </summary>
[System.Serializable] public sealed class PiercingAttributes
{
                            public bool     isPiercing = false;     // Indicates whether the bullet should be piercing or not
    [Range (0, 100)]        public int      maxPiercings = 0;       // How many times the bullet can pierce
    [Range (0f, 1f)]        public float    pierceModifier = 0f;    // How much to reduce speed and damage by on pierce
}



/// <summary>
/// Contains each debuff/special attribute which bullets need to inform the health script about.
/// </summary>
[System.Serializable] public sealed class SpecialAttributes
{
    [Range (0f, 1f)]        public float    chanceToDisable = 0f;   // How much of a percentage chance to disable the target on hit the bullet should have
    [Range (0f, 100f)]      public float    disableEffect = 0f;     // How many seconds to disable the target for on hit
    [Range (0f, 100f)]      public float    slowEffect = 0f;        // How long to slow the enemy for on hit
    [Range (0f, 100f)]      public float    dotDuration = 0f;       // How long damage over time should be applied
    [Range (0f, 100f)]      public float    dotEffect = 0f;         // How much damage per second should be dealt by damage over time
}



/// <summary>
/// A class to be used as a struct to contain all properties which a bullet needs to function with the flexible augment system.
/// </summary>
public sealed class BulletProperties
{
    #region Member variables

    // Base mechanics
                            public bool                 isBeam = false;                         // Indicates whether the bullet should act as a beam or not
    [Range (0, 1000)]       public int                  damage = 0;                             // The damage that a bullet should deal with no modifiers
    [Range (0f, 100f)]      public float                reach = 0f;                             // The desired length the bullet should reach/travel
    [Range (0.001f, 100f)]  public float                lifetime = 1f;                          // How long it should take for the bullet to reach its destination
        
    // Attribute collections
                            public AOEAttributes        aoe = null;                             // Contains each AoE attribute
                            public HomingAttributes     homing = null;                          // Contains each homing attribute
                            public PiercingAttributes   piercing = null;                        // Contains each piercing attribute
                            public SpecialAttributes    special = null;                         // Contains each special attribute

    // Hidden members
    [HideInInspector]       public GameObject           firer = null;                           // The GameObject of the weapon that fired the bullet
    [HideInInspector]       public List<Element>        appliedElements = new List<Element>(0); // What elements have been applied to the bullet

    #endregion
}

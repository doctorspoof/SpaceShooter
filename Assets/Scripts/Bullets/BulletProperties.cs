using UnityEngine;
using System.Collections.Generic;



/// <summary>
/// A class to be used as a struct to contain all properties which a bullet needs to function with the flexible augment system.
/// </summary>
public sealed class BulletProperties 
{
    #region Unity modifiable variables

    // Base mechanics
    [Range (0, 1000)]       public int      damage = 0;             // The damage that a bullet should deal with no modifiers
    [Range (0f, 100f)]      public float    reach = 0f;             // The desired length the bullet should reach/travel
    [Range (0.001f, 100f)]  public float    lifetime = 1f;          // How long it should take for the bullet to reach its destination (may not apply to beams)
                     
    // Piercing attributes
                            public bool     isPiercing = false;     // Indicates whether the bullet should be piercing or not
    [Range (0, 100)]        public int      maxPiercings = 0;       // How many times the bullet can pierce
    [Range (0f, 1f)]        public float    pierceModifier = 0f;    // How much to reduce speed and damage by on pierce

    // Homing attributes
                            public bool     isHoming = false;       // Indicates whether the bullet should home in on targets
    [Range (0f, 100f)]      public float    homingTurnRate = 0f;    // How quickly the bullet can home in on targets

    // AoE attributes
                            public bool     isAOE = false;          // Indicates wether the bullet should cause AoE damage or not
    [Range (0f, 100f)]      public float    aoeRange = 0f;         // How far the AoE on the bullet should be
    [Range (0f, 100f)]      public float    aoeMaxDamageRange = 0f; // The distance between the explosion point and the enemy in which there is no damage fall off
    [Range (0f, 1000f)]     public float    aoeExplosiveForce = 0f; // The maximum amount of explosive force to apply to the enemy
    [Range (0.001f, 1f)]    public float    aoeMaxFalloff = 0.001f;  // How much force and damage can be reduced by distance from the AoE

    // Debuff attributes
    [Range (0f, 1f)]        public float    chanceToDisable = 0f;   // How much of a percentage chance to disable the target on hit the bullet should have
    [Range (0f, 100f)]      public float    disableEffect = 0f;     // How many seconds to disable the target for on hit
    [Range (0f, 100f)]      public float    slowEffect = 0f;        // How long to slow the enemy for on hit
    [Range (0f, 100f)]      public float    dotDuration = 0f;       // How long damage over time should be applied
    [Range (0f, 100f)]      public float    dotEffect = 0f;         // How much damage per second should be dealt by damage over time

    #endregion


    #region Hidden members

    [HideInInspector]   public GameObject       firer = null;                           // The GameObject of the weapon that fired the bullet
    [HideInInspector]   public GameObject       target = null;                          // Used to provide a homing target for the bullet
    [HideInInspector]   public List<Element>    appliedElements = new List<Element>(0); // What elements have been applied to the bullet

    #endregion
}

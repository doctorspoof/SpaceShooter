using UnityEngine;
using System.Collections;
using System.Collections.Generic;



/// <summary>
/// Bullet is the class used by all projectiles in the game, it is incredibly flexible and will work with any
/// reasonable BulletProperties values. Bullet is very flexible and can function with any combination of elements
/// and attributes as defined in the passed BulletProperties.
/// </summary>
[RequireComponent (typeof (Rigidbody))]
public sealed class Bullet : MonoBehaviour 
{
    #region Internal data

    float               m_damageOverflow = 0f;                  //!< Tracks the float to int truncation caused by casting.
    float               m_reachModifier = 0f;                   //!< Used to change the velocity of the bullet based on the firers velocity.
    float               m_speed = 0f;                           //!< Caches the speed that the bullet should travel at.

    int                 m_pierceCount = 0;                      //!< How many times the bullet has actually pierced through an enemy.
    int                 m_damageMask = 0;                       //!< The layer mask for damagable enemies.
    int                 m_aoeMask = 0;                          //!< The layer mask used to deal AoE damage.
    int                 m_homingMask = 0;                       //!< The layer mask used to find targets to home in on.
    
    Vector2             m_hitPoint = Vector2.zero;              //!< Contains the exact point where the bullet hit a target, useful for shader effects.
    BulletProperties    m_properties = null;                    //!< Contains all bullet specific information which is required for the bullet to operate.
                                                                //!< This must be passed by the weapon when fired, preferably before the Awake() call.
    List<GameObject>    m_pastHits = new List<GameObject>(0);   //!< Prevents piercing from hitting the same enemy multiple times.

    #endregion


    #region Getters & setters

    /// <summary>
    /// Gets the last point of impact of the bullet.
    /// </summary>
    /// <returns>m_hitPoint which is the last valid hit point.</returns>
    public Vector2 GetHitPoint()
    {
        return m_hitPoint;
    }


    /// <summary>
    /// Sets the reach modifier, this will directly increase or decrease the bullet speed.
    /// </summary>
    /// <param name="reachModifier">The exact reach modifier.</param>
    public void SetReachModifier (float reachModifier)
    {
        m_reachModifier = reachModifier;
    }


    /// <summary>
    /// Sets the reach modifier, this will directly increase or decrease the bullet speed.
    /// </summary>
    /// <param name="velocity">The velocity of which the correct reach modifier will be calculated.</param>
    public void SetReachModifier (Vector3 velocity)
    {
        // Using the dot product of the bullets velocity vs the passed velocity results in a smooth speed modifier
        m_reachModifier = Vector3.Dot (rigidbody.velocity, velocity);
    }


    /// <summary>
    /// Creates a clone of all values in the passed BulletProperties, this creates a new instance instead of referencing the pointer.
    /// This allows the bullet to take ownership of its own properties once it has been fired.
    /// </summary>
    /// <param name="toClone">The BulletProperties which need to be cloned.</param>
    public void CloneProperties (BulletProperties toClone)
    {
        if (toClone != null)
        {
            // Allow the BulletProperties copy constructor to do the hard work
            m_properties = new BulletProperties (toClone);
        }

        else
        {
            // Handle the error by using default values
            m_properties = new BulletProperties();

            Debug.LogError ("Attempt to assign null to " + name + ".m_properties");
        }
    }

    #endregion


    #region Behavior functions

    /// <summary>
    /// Ensures that data is correct and determines whether to start the bullet or beam coroutine.
    /// </summary>
    void Awake()
    {
        if (m_properties == null)
        {
            // Initialise with default values, not that this is useful but it may be less game breaking.
            m_properties = new BulletProperties();

            Debug.LogError (name + ".Bullet.m_properties == null, bad things WILL happen.");
        }

        else
        {
            rigidbody.isKinematic = true;

            // Ensure layer masks are valid
            LayerMaskSetup();

            // Initialise m_speed
            CalculateMovementSpeed();

            // Start the correct coroutine
            if (m_properties.isBeam)
            {
                StartCoroutine (BeamUpdate());
            }

            else
            {
                StartCoroutine (BulletUpdate());
            }
        }
    }


    /// <summary>
    /// Catches the trigger event when the bullet hits a target, this is where damage will be dealt but only by the server.
    /// </summary>
    /// <param name="other">The collider that has been hit, there's no guarantee that this isn't a composite collider.</param>
    void OnTriggerEnter (Collider other)
    {

    }

    #endregion


    #region Update methods

    /// <summary>
    /// Performs all necessary actions for beam bullets in the FixedUpdate() loop. Seperating beam and non-beam updates allows for
    /// bullets to avoid checking if they're beams or not every frame.
    /// </summary>
    /// <returns>WaitForFixedUpdate().</returns>
    IEnumerator BeamUpdate()
    {
        while (true)
        {
            yield return new WaitForFixedUpdate();
        }
    }


    /// <summary>
    /// Performs all necessary actions for non-beam bullets in the FixedUpdate() loop. Seperating beam and non-beam updates allows for
    /// bullets to avoid checking if they're beams or not every frame.
    /// </summary>
    /// <returns>WaitForFixedUpdate().</returns>
    IEnumerator BulletUpdate()
    {
        while (true)
        {
            // We need to know if the bullet is homing or not
            if (m_properties.homing != null && m_properties.homing.isHoming)
            {
                // Ensure the homing bullet has a target
                if (m_properties.homing.target == null)
                {
                    m_properties.homing.target = FindClosestTarget();
                }

                // Start rotating towards the target before we move forward
                RotateTowardsTarget();
            }

            // Finally move forward
            MoveForwards();

            yield return new WaitForFixedUpdate();
        }
    }

    #endregion


    #region Bullet movement

    /// <summary>
    /// A simple function which uses rigidbody.MovePosition() to move forward by the calculated speed.
    /// </summary>
    void MoveForwards()
    {

    }


    void RotateTowardsTarget()
    {

    }

    #endregion


    #region Detonation

    [RPC] public void DetonateBullet()
    {

    }


    void ExplodeBullet()
    {

    }

    #endregion


    #region Damage

    void DamageMob (GameObject mob)
    {

    }


    void DamageAOE()
    {

    }


    [RPC] void ApplyPierceModifier()
    {

    }


    void AddExplosiveForce (Rigidbody mob)
    {

    }

    #endregion


    #region Utilities

    /// <summary>
    /// Assigns the correct values to each layer mask stored by Bullet using Layers.GetLayerMask().
    /// </summary>
    void LayerMaskSetup()
    {
        // Should be able to just use Layers.GetLayerMask and pass in the objects layer.
        m_damageMask = Layers.GetLayerMask (gameObject.layer);
        m_aoeMask = Layers.GetLayerMask (gameObject.layer, MaskType.AoE);
        m_homingMask = Layers.GetLayerMask (gameObject.layer, MaskType.Targetting);
    }


    /// <summary>
    /// Calculates the movement speed based on the reach, reachModifier and lifetime values give. Sets the calculated value to m_speed.
    /// </summary>
    void CalculateMovementSpeed()
    {

    }

    
    GameObject FindClosestTarget()
    {
        return null;
    }

    #endregion
}

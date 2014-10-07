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

    public float               m_currentLifetime = 0.0f;
    float               m_damageOverflow = 0f;                  //!< Tracks the float to int truncation caused by casting.
    float               m_reachModifier = 0f;                   //!< Used to change the velocity of the bullet based on the firers velocity.
    public float               m_speed = 0f;                           //!< Caches the speed that the bullet should travel at.
    float               m_bulletMinimumSpeed = 2.5f;

    int                 m_pierceCount = 0;                      //!< How many times the bullet has actually pierced through an enemy.
    int                 m_damageMask = 0;                       //!< The layer mask for damagable enemies.
    int                 m_aoeMask = 0;                          //!< The layer mask used to deal AoE damage.
    int                 m_homingMask = 0;                       //!< The layer mask used to find targets to home in on.
    bool                m_bulletHasBeenDestroyed = false;       //!< Uber safeguard against double damage shots
    
    GameObject          m_firer = null;
    
    Vector2             m_hitPoint = Vector2.zero;              //!< Contains the exact point where the bullet hit a target, useful for shader effects.
    public BulletProperties    m_properties = null;                    //!< Contains all bullet specific information which is required for the bullet to operate.
                                                                //!< This must be passed by the weapon when fired, preferably before the Awake() call.
    List<GameObject>    m_pastHits = new List<GameObject>(0);   //!< Prevents piercing from hitting the same enemy multiple times.
    Element             m_cachedMajorElement = Element.NULL;

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
    public int GetDamage()
    {
        return m_properties.damage;
    }
    public Element GetMajorElement()
    {
        if(m_cachedMajorElement == Element.NULL)
            DetermineMajorityElement();
            
        return m_cachedMajorElement;
    }
    
    Element DetermineMajorityElement()
    {
        int[] counter = new int[10];
        
        for(int i = 0; i < m_properties.appliedElements.Count; i++)
        {
            if(m_properties.appliedElements[i] != null)
            {
                switch(m_properties.appliedElements[i])
                {
                case Element.Fire:
                {
                    counter[0] += 1;
                    break;
                }
                case Element.Ice:
                {
                    counter[1] += 1;
                    break;
                }
                case Element.Earth:
                {
                    counter[2] += 1;
                    break;
                }
                case Element.Lightning:
                {
                    counter[3] += 1;
                    break;
                }
                case Element.Light:
                {
                    counter[4] += 1;
                    break;
                }
                case Element.Dark:
                {
                    counter[5] += 1;
                    break;
                }
                case Element.Spirit:
                {
                    counter[6] += 1;
                    break;
                }
                case Element.Gravity:
                {
                    counter[7] += 1;
                    break;
                }
                case Element.Air:
                {
                    counter[8] += 1;
                    break;
                }
                case Element.Organic:
                {
                    counter[9] += 1;
                    break;
                }
                }
            }
        }
        
        int highestID = -1;
        int highestVal = 0;
        
        for(int i = 0; i < counter.Length; i++)
        {
            if(counter[i] > highestVal)
            {
                highestVal = counter[i];
                highestID = i;
            }
        }
        
        switch(highestID)
        {
        case 0:
        {
            m_cachedMajorElement = Element.Fire;
            return Element.Fire;
        }
        case 1:
        {
            m_cachedMajorElement = Element.Ice;
            return Element.Ice;
        }
        case 2:
        {
            m_cachedMajorElement = Element.Earth;
            return Element.Earth;
        }
        case 3:
        {
            m_cachedMajorElement = Element.Lightning;
            return Element.Lightning;
        }
        case 4:
        {
            m_cachedMajorElement = Element.Light;
            return Element.Light;
        }
        case 5:
        {
            m_cachedMajorElement = Element.Dark;
            return Element.Dark;
        }
        case 6:
        {
            m_cachedMajorElement = Element.Spirit;
            return Element.Spirit;
        }
        case 7:
        {
            m_cachedMajorElement = Element.Gravity;
            return Element.Gravity;
        }
        case 8:
        {
            m_cachedMajorElement = Element.Air;
            return Element.Air;
        }
        case 9:
        {
            m_cachedMajorElement = Element.Organic;
            return Element.Organic;
        }
        default:
        {
            m_cachedMajorElement = Element.NULL;
            return Element.NULL;
        }
        }
    }
    

    /// <summary>
    /// Sets the reach modifier, this will directly increase or decrease the bullet speed.
    /// </summary>
    /// <param name="reachModifier">The exact reach modifier.</param>
    public void SetReachModifier (float reachModifier)
    {
        m_reachModifier = reachModifier;
        CalculateMovementSpeed();
    }
    
    public void SetHomingTarget(GameObject target)
    {
        m_properties.homing.target = target;
    }
    
    public void SetFirer(GameObject firer)
    {
        m_firer = firer;
    }


    /// <summary>
    /// Sets the reach modifier, this will directly increase or decrease the bullet speed.
    /// </summary>
    /// <param name="velocity">The velocity of which the correct reach modifier will be calculated.</param>
    public void SetReachModifier (Vector3 velocity)
    {
        // Using the dot product of the bullets velocity vs the passed velocity results in a smooth speed modifier
        m_reachModifier = Vector3.Dot (rigidbody.velocity, velocity);
        CalculateMovementSpeed();
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
        
        //Either way, cache the speed value for future use
        CalculateMovementSpeed();
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
            // TODO: Add elemental visual changes

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
        if (Network.isServer && (!other.isTrigger || other.gameObject.layer == Layers.enemyDestructibleBullet))
        {
            //try
            //{
                //if (!m_properties.aoe.isAOE)
                if(m_properties.aoe == null || !m_properties.aoe.isAOE)
                {
                    // Colliders may be part of a composite collider so we must use Collider.attachedRigidbody to get the HealthScript component
                    DamageMob(other.attachedRigidbody.gameObject, m_properties.damage);
                }
            //}
            
            //catch (System.Exception error)
            //{
            //    Debug.LogError ("Exception Occurred in Bullet: " + error.Message + " at " + error.Source);
            //    Debug.LogError ("Attempted to hit: " + other.transform.root.name);
            //}
            
            //finally
            //{
                // Piercing bullets continue until the end of their lifetime.
                if (m_properties.aoe != null && m_properties.aoe.isAOE || m_properties.piercing == null || m_properties.piercing.isPiercing || m_pierceCount > m_properties.piercing.maxPiercings || m_properties.damage < 1)
                {
                    DetonateBullet();
                }
                else
                {
                    Debug.Log("Didn't destroy " + gameObject.name);
                }
            //}
        }
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
                if(m_properties.homing.target != null)
                    RotateTowardsTarget();
            }

            // Finally move forward
            MoveForwards();
            
            if (m_currentLifetime < m_properties.lifetime)
            {
                m_currentLifetime += Time.deltaTime;
                if (m_currentLifetime >= m_properties.lifetime)
                {
                    if (Network.isServer)
                    {
                        DetonateBullet();
                    }
                }
            }

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
        rigidbody.MovePosition(rigidbody.position + (transform.up * m_speed * Time.deltaTime));
    }


    void RotateTowardsTarget()
    {
        Vector3 dir = m_properties.homing.target.transform.position - transform.position;
        
        Quaternion targetR = Quaternion.Euler(new Vector3(0, 0, (Mathf.Atan2(dir.y, dir.x) - Mathf.PI / 2) * Mathf.Rad2Deg));
        
        rigidbody.MoveRotation(Quaternion.Slerp(transform.rotation, targetR, m_properties.homing.homingTurnRate * Time.deltaTime));
    }

    #endregion


    #region Detonation

    [RPC] public void DetonateBullet()
    {
        if (m_properties.aoe != null && m_properties.aoe.isAOE)
        {  
            if (Network.isServer && !m_bulletHasBeenDestroyed)
            {
                m_bulletHasBeenDestroyed = true;
                networkView.RPC ("DetonateBullet", RPCMode.Others);
            }  
            
            DamageAOE();
            ExplodeBullet();
        }
        
        // Ensures bullet is completely destroyed
        else if (Network.isServer && !m_bulletHasBeenDestroyed)
        {
            m_bulletHasBeenDestroyed = true;
            Network.Destroy(gameObject);
        }
    }


    void ExplodeBullet()
    {
        Explode explode = this.GetComponent<Explode>();
        
        if (explode)
        {
            explode.Fire();
        }
        else
        {
            Debug.LogError ("Unable to find Explode component on: " + name);
        }
    }

    #endregion


    #region Damage

    void DamageMob (GameObject mob, int damage)
    {
        if (Network.isServer && mob && damage > 0)
        {
            if ((m_properties.piercing != null && !m_properties.piercing.isPiercing) || !m_pastHits.Contains(mob))
            {
                HealthScript health = mob.GetComponent<HealthScript>();
                if (health != null)
                {
                    health.DamageMob(damage, m_firer, gameObject);
                    
                    // Check if any of the current special stats will proc
                    if(m_properties.special != null && mob.GetComponent<Ship>())
                    {
                        if(m_properties.special.chanceToJump > 0f)
                        {
                            float rand = Random.Range(0.0f, 1.0f);
                            if(rand <= m_properties.special.chanceToJump)
                            {
                                Rigidbody[] enemiesInRange = Physics.OverlapSphere(transform.position, 15.0f, 1 << Layers.enemy).GetAttachedRigidbodies();
                                
                                if(enemiesInRange.Length > 1)
                                {
                                    int id = Random.Range(0, enemiesInRange.Length);
                                    while(enemiesInRange[id].gameObject == mob)
                                    {
                                        id = Random.Range (0, enemiesInRange.Length);
                                    }
                                    DamageMob(enemiesInRange[id].gameObject, (int)(damage * 0.5f));
                                }
                                
                                //TODO: Add visual effect here
                            }
                        }
                        
                        if(m_properties.special.chanceToDisable > 0f)
                        {
                            float rand = Random.Range(0.0f, 1.0f);
                            if(rand <= m_properties.special.chanceToDisable)
                            {
                                mob.GetComponent<Ship>().AddDebuff(new DebuffDisable(m_properties.special.disableDuration, mob));
                            }
                        }
                        
                        if(m_properties.special.slowDuration > 0f)
                        {
                            mob.GetComponent<Ship>().AddDebuff(new DebuffSlow(m_properties.special.slowDuration, 0.6f, mob));
                        }
                        
                        if(m_properties.special.dotEffect > 0f)
                        {
                            mob.GetComponent<Ship>().AddDebuff(new DebuffDoT(m_properties.special.dotDuration, m_properties.special.dotEffect, mob));
                        }
                    }
                    
                    if(m_properties.piercing != null)
                    {
                        m_properties.piercing.isPiercing = m_properties.piercing.isPiercing && health.GetCurrShield() == 0 ? true : false;
                        
                        if(m_properties.piercing.isPiercing)
                        {
                            m_pastHits.Add(mob);
                            networkView.RPC ("ApplyPierceModifiers", RPCMode.All);
                        }
                    }
                }
                else
                {
                    Debug.LogError("Unable to find HealthScript on: " + mob.name);
                }
            }
        }
    }


    void DamageAOE()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, m_properties.aoe.aoeRange, m_aoeMask);
        Rigidbody[] unique = colliders.GetAttachedRigidbodies().GetUniqueOnly();
        
        // Cache values for the sake of efficiency, also avoid Vector3.Distance by squaring ranges
        float   distance = 0f, damage = 0f,
        maxDistance = m_properties.aoe.aoeRange - m_properties.aoe.aoeMaxDamageRange;
        
        foreach (Rigidbody mob in unique)
        {
            // Ensure the distance will equate to 0f - 1f for the Lerp function
            distance = (transform.position - colliders.GetClosestPointFromRigidbody(mob, transform.position)).magnitude - m_properties.aoe.aoeMaxDamageRange;
            distance = Mathf.Clamp(distance, 0f, maxDistance);
            damage = Mathf.Lerp(m_properties.damage, 1, distance / maxDistance);
            
            DamageMob(mob.gameObject, (int)damage);
            AddExplosiveForce(mob);
        }
    }


    [RPC] void ApplyPierceModifier()
    {
        // Update speed to correspond to the hit, ensure minimum speed is in effect using the bulletSpeedModifier property
        m_speed *= m_properties.piercing.pierceModifier;
        SetReachModifier (m_reachModifier);
        
        // Apply damage modifier whilst catching the float overflow
        float m_newDamage = m_properties.damage * m_properties.piercing.pierceModifier + m_damageOverflow;
        m_properties.damage = (int) m_newDamage;
        m_damageOverflow = m_newDamage - m_properties.damage;
        
        // Finally increment the pierce counter
        ++m_pierceCount;
    }


    void AddExplosiveForce (Rigidbody mob)
    {
        // Use the mobs z position to stop the force causing enemies to move upwards all the time
        Vector3 position = new Vector3(transform.position.x, transform.position.y, mob.transform.position.z);
        mob.AddCustomExplosionForce(position, m_properties.aoe.aoeRange, 0, m_properties.aoe.aoeExplosiveForce);
        
        switch (mob.gameObject.layer)
        {
            case Layers.asteroid:
                SyncAsteroid(mob);
                break;
                
            case Layers.player:
                SyncPlayer(mob);
                break;
                
            default:
                    break;
        }
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
        m_speed = (m_properties.reach + m_reachModifier) / m_properties.lifetime;
    }

    
    GameObject FindClosestTarget()
    {
        Collider[] enemyCollidersInRange = Physics.OverlapSphere(transform.position, m_properties.homing.homingRange, m_homingMask);
        //Debug.Log ("Found " + enemyCollidersInRange.Length + " objects in range");
        Rigidbody[] enemiesInRange = enemyCollidersInRange.GetAttachedRigidbodies();
        
        GameObject enemy = null;
        float shortestDistance = 9999f;
        
        if(enemiesInRange != null && enemiesInRange.Length > 0)
        {
            for(int i = 0; i < enemiesInRange.Length; i++)
            {
                //Debug.Log ("Trying to access enemy #" + i);
                float dist = Vector3.Distance(transform.position, enemiesInRange[i].transform.position);
                if(dist < shortestDistance)
                {
                    enemy = enemiesInRange[i].gameObject;
                    shortestDistance = dist;
                }
            }
        }
        
        return enemy;
    }
    
    void SyncAsteroid(Rigidbody asteroid)
    {
        AsteroidScript script = asteroid.GetComponent<AsteroidScript>();
        
        if (script)
        {
            // Wait one FixedUpdate frame to sync the asteroids
            script.DelayedVelocitySync(Time.fixedDeltaTime);
        }
    }
    // Players need to be told to apply their own explosive force to ensure they get knocked back
    void SyncPlayer(Rigidbody player)
    {
        PlayerControlScript script = player.GetComponent<PlayerControlScript>();
        
        if (script)
        {
            // Make the player apply it's own explosive force to ensure it reacts accordingly
            script.ApplyExplosiveForceOverNetwork(transform.position.x, transform.position.y, m_properties.aoe.aoeRange, 0, m_properties.aoe.aoeExplosiveForce);
        }
    }

    #endregion
}

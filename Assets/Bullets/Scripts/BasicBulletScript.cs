using UnityEngine;
using System.Collections.Generic;

public enum DamageType
{
    Physical = 0,
    Energy = 1,
    Laser = 2,
    Explosive = 3
}

[RequireComponent(typeof(Rigidbody))]
public class BasicBulletScript : MonoBehaviour
{
    // Used to represent the AOE accuracy
    private enum Accuracy
    {
        High,
        Low,
        Off
    }


    /// Unity modifiable values
    // Bullet damage
    [SerializeField] 
    int m_bulletDamage = 1;			                // The maximum damage dealt by the bullet
    [SerializeField] 
    int m_bulletMinDamage = 1;		                // The minimum damage, used in AOE and piercing falloff
    public DamageType m_damageType;

    // Speed and Lifetime
    [SerializeField]
    float m_bulletSpeed = 2.5f;	    	            // How fast the bullet should travel
    [SerializeField]
    float m_bulletMinimumSpeed = 2.5f;              // The minimum speed after modifiers
    [SerializeField, Range(0f, 60f)]
    float m_bulletMaxLifetime = 1f;		            // How long the bullet can survive before destroying itself

    // Self-propulsion
    [SerializeField]
    bool m_isSelfPropelled = false;		            // If a bullet is self propelled, it should only be affect initially by the player's move speed

    // Piercing attributes
    [SerializeField]
    bool m_isPiercing = false;			            // Whether the bullet should pierce through enemy hulls or not
    [SerializeField, Range(0, 100)]
    int m_maxPierceHits = 5;				        // How many times the bullet can pierce through targets
    [SerializeField, Range(0f, 1f)]
    float m_pierceDamageModifier = 0.4f;	        // Reduces the bullet damage by this factor
    [SerializeField, Range(0f, 1f)]
    float m_pierceSpeedModifier = 0.7f;	            // Reduces the bullet speed by this factor

    // Homing attributes
    [SerializeField]
    bool m_isHoming = false;				        // Whether the bullet should track targets or not
    [SerializeField]
    float m_homingRange = 5.0f;				        // How far away a target should be tracked
    [SerializeField]
    float m_homingRotateSpeed = 0.5f;		        // How quickly the bullet should rotate to chase its target

    // Area of effect attributes
    [SerializeField]
    bool m_isAOE = false;							// Whether the bullet should damage an area or not
    [SerializeField]
    Accuracy m_aoeDamageAccuracy = Accuracy.High;	// How accurate the AoE damage should be based on the enemies distance
    [SerializeField, Range(0f, 100f)]
    float m_aoeRange = 2f;				            // How far away the enemies can be from the explosion
    [SerializeField, Range(0f, 100f)]
    float m_aoeMaxDamageRange = 0.25f;	            // The margin between the explosion and the target for them to receive maximum damage
    [SerializeField, Range(0f, 1000f)]
    float m_aoeMaxExplosiveForce = 15f;	            // How much force can be applied
    [SerializeField, Range(0f, 1000f)]
    float m_aoeMinExplosiveForce = 5f;	            // The minimum amount of force which must be applied



    /// Internal data
    float m_bulletDamageOverflow = 0f;						// Used to catch lost damage from piercing due to float -> int casting
    float m_bulletSpeedModifier = 0f; 						// Changes the bullet speed to reflect the speed of the firer, it also maintains the minimum speed if necessary
    float m_currentLifetime = 0f;							// Simply keeps an eye on the current lifetime

    int m_pierceCounter = 0;								// Keeps a reference to how many times the bullet has pierced through an enemy
    int m_homingMask = 0;									// The layer mask used for homing functionality, this is set up using SetupLayerMask()
    int m_aoeMask = 0;										// The layer mask used for AoE functionality, it gets set up at the same time as m_homingMask

    [HideInInspector]
    public GameObject firer = null;		                    // A reference to which GameObject fired the bullet
    public GameObject m_homingTarget = null;				// A reference to the targetted enemy for homing purposes
    List<GameObject> m_pastHits = new List<GameObject>();	// Keeps a reference to GameObject's that have been damaged previously, solves composite collider issues


    /// Properties, getters and setters
    public float bulletSpeedModifier
    {
        get { return m_bulletSpeedModifier; }
        set
        {
            //Ensure the minimum bullet speed is capped
            if (m_bulletSpeed + value < m_bulletMinimumSpeed)
            {
                m_bulletSpeedModifier = m_bulletMinimumSpeed - m_bulletSpeed;
            }

            else
            {
                m_bulletSpeedModifier = value;
            }
        }
    }


    public int GetDamage()
    {
        return m_bulletDamage;
    }


    public float GetBulletSpeed()
    {
        return m_bulletSpeed;
    }


    public float GetAoERange()
    {
        return m_aoeRange;
    }


    public float CalculateMaxDistance()
    {
        return (m_bulletSpeed + m_bulletSpeedModifier) * m_bulletMaxLifetime;
    }



    /// Functions
    // Set up the bullet
    void Start()
    {
        if (m_bulletDamage < m_bulletMinDamage)
        {
            // Silly extension methods force the callee to be a copy so we have to pass the object as a reference
            m_bulletDamage.Swap(ref m_bulletDamage, ref m_bulletMinDamage);
        }

        if (m_bulletSpeed < m_bulletMinimumSpeed)
        {
            m_bulletSpeed.Swap(ref m_bulletSpeed, ref m_bulletMinimumSpeed);
        }

        if (m_aoeRange < m_aoeMaxDamageRange)
        {
            m_aoeRange.Swap(ref m_aoeRange, ref m_aoeMaxDamageRange);
        }

        LayerMaskSetup();
        rigidbody.isKinematic = true;
    }


    // Follow the target if homing and manage the lifetime of the bullet
    void FixedUpdate()
    {
        if (m_isHoming)
        {
            /*if (m_homingTarget == null)
            {
                m_homingTarget = GetClosestTarget (m_homingMask);
            }*/

            if (m_homingTarget == null)
            {
                MoveForwards();
            }

            else
            {
                RotateToTarget();
                MoveForwards();
            }
        }

        else
        {
            MoveForwards();
        }

        if (m_currentLifetime < m_bulletMaxLifetime)
        {
            m_currentLifetime += Time.deltaTime;
            if (m_currentLifetime >= m_bulletMaxLifetime)
            {
                if (Network.isServer)
				{
					DetonateBullet();
				}
			}
        }
    }


    // Handle bullet collision including all of its intricacies
    void OnTriggerEnter(Collider other)
    {
        if (Network.isServer && (!other.isTrigger || other.gameObject.layer == Layers.enemyDestructibleBullet))
        {
            switch (other.gameObject.layer)
            {
                // Do nothing is layer isn't applicable
                default:
                    break;

                // Bullets only need to interact with HealthScript containing GameObject's
                case Layers.player:
                case Layers.capital:
                case Layers.enemy:
                case Layers.enemyDestructibleBullet:
                case Layers.asteroid:
                case Layers.enemyCollide:
	            {
	                try
	                {
	                    if (!m_isAOE)
	                    {
	                        // Colliders may be part of a composite collider so we must use Collider.attachedRigidbody to get the HealthScript component
	                        DamageMob(other.attachedRigidbody.gameObject, m_bulletDamage, m_isPiercing);
	                    }
	                }

	                catch (System.Exception error)
	                {
	                    Debug.LogError("Exception Occurred in BasicBulletScript: " + error.Message + " at " + error.Source);
	                }

	                // This should absolutely always run and should never ever not run ever, literally ever.
	                finally
	                {
	                    // Piercing bullets continue until the end of their lifetime.
	                    if (m_isAOE || !m_isPiercing || m_pierceCounter > m_maxPierceHits || m_bulletDamage < 1)
	                    {
							DetonateBullet();
	                    }

	                    else
	                    {
	                        Debug.Log("Didn't destroy " + gameObject.name);
	                    }
	                }

	                break;
	            }
            }
        }
    }

    // This should be called at Awake() or Start() otherwise AoE and homing functionality won't work correctly
    void LayerMaskSetup()
    {
        const int 	player = (1 << Layers.player),
                    capital = (1 << Layers.capital),
                    enemy = (1 << Layers.enemy),
                    asteroid = (1 << Layers.asteroid),
                    enemyCollide = (1 << Layers.enemyCollide);

        switch (this.gameObject.layer)
        {
            case Layers.playerBullet:
                m_homingMask = enemy | enemyCollide;
                m_aoeMask = m_homingMask | player | asteroid;
                break;

            case Layers.capitalBullet:
                m_homingMask = enemy | enemyCollide;
                m_aoeMask = m_homingMask | asteroid;
                break;

            case Layers.enemyBullet:
            case Layers.enemyDestructibleBullet:
                m_homingMask = player | capital;
                m_aoeMask = m_homingMask | enemy | enemyCollide | asteroid;
                break;
        }
    }


    // Simply finds and returns the closest target
    GameObject GetClosestTarget(int layerMask)
    {
        float shortestDist = float.MaxValue;
        int closestID = -1;

        Rigidbody[] targets = Physics.OverlapSphere(this.transform.position, m_homingRange, layerMask).GetAttachedRigidbodies().GetUniqueOnly();

        for (int i = 0; i < targets.Length; i++)
        {
            // Use sqrMagnitude for the distance to improve performance
            float distance = (this.transform.position - targets[i].transform.position).sqrMagnitude;
            if (closestID == -1 || distance < shortestDist)
            {
                shortestDist = distance;
                closestID = i;
            }
        }

        return closestID == -1 ? null : targets[closestID].gameObject;
    }


    // Rotate towards the direction of the target
    void RotateToTarget()
    {
        Vector3 dir = m_homingTarget.transform.position - transform.position;

        Quaternion targetR = Quaternion.Euler(new Vector3(0, 0, (Mathf.Atan2(dir.y, dir.x) - Mathf.PI / 2) * Mathf.Rad2Deg));

        rigidbody.MoveRotation(Quaternion.Slerp(transform.rotation, targetR, m_homingRotateSpeed * Time.deltaTime));
    }


    //Uses MovePosition() to move the rigidbody forwards by the bullet speed
    void MoveForwards()
    {
        if (m_isSelfPropelled)
        {
            if (m_currentLifetime > 0.4f)
            {
                m_bulletSpeedModifier *= 0.95f;
            }
        }

        rigidbody.MovePosition(rigidbody.position + transform.up * (m_bulletSpeed + m_bulletSpeedModifier) * Time.deltaTime);
    }


    [RPC] public void DetonateBullet()
    {
		// AOE bullets get destroyed by the ExplodeBullet() function
		if (m_isAOE)
        {
            if (Network.isServer)
            {
                networkView.RPC ("DetonateBullet", RPCMode.Others);
            }
            
            DamageAOE();

            ExplodeBullet();
        }

		// Ensures bullet is completely destroyed
        else if (Network.isServer)
        {
            Network.Destroy(gameObject);
        }
    }


	void ExplodeBullet()
	{
		Explode explode = GetComponent<Explode>();
		explode.Fire();
	}


    // Attempts to damage the passed GameObject whilst managing the piercing state of the bullet
    void DamageMob(GameObject mob, int damage, bool incrementPierceCounter = false)
    {
        if (Network.isServer && mob)
        {
            if (!m_isPiercing || !m_pastHits.Contains(mob))
            {
                //Debug.Log("Attempting to damage mob: " + mob.name);
                HealthScript health = mob.GetComponent<HealthScript>();

                if (health)
                {
                    health.DamageMob(damage, firer, gameObject);

                    m_isPiercing = m_isPiercing && health.GetCurrShield() == 0 ? true : false;

                    if (incrementPierceCounter)
                    {
                        m_pastHits.Add(mob);
                        ApplyPierceModifiers();
                    }
                }

                else
                {
                    Debug.LogError("Unable to find HealthScript on: " + mob.name);
                }
            }
        }
    }


    // Adjust bullet speed, bullet damage and increase the pierce counter
    void ApplyPierceModifiers()
    {
        // Update speed to correspond to the hit, ensure minimum speed is in effect using the bulletSpeedModifier property
        m_bulletSpeed *= m_pierceSpeedModifier;
        bulletSpeedModifier = m_bulletSpeedModifier;

        // Apply damage modifier whilst catching the float overflow
        float m_newDamage = m_bulletDamage * m_pierceDamageModifier + m_bulletDamageOverflow;
        m_bulletDamage = (int)m_newDamage;
        m_bulletDamageOverflow = m_newDamage - m_bulletDamage;

        // Finally increment the pierce counter
        ++m_pierceCounter;
    }


    // The central AoE function which will call the correct AoE function based on the set accuracy level
    void DamageAOE()
    {
		if (Network.isServer)
		{
			switch (m_aoeDamageAccuracy)
			{
				case Accuracy.High:
					HighAccuracyAOE();
					break;
					
				case Accuracy.Low:
				case Accuracy.Off:
					LowAccuracyAOE(m_aoeDamageAccuracy == Accuracy.Low);
					break;
			}
			
			if (m_isPiercing)
			{
				ApplyPierceModifiers();
			}
		}
    }


    /// <summary>
    /// Gets a collection of colliders, obtains the unique Rigidbody objects, finds the closest collision point for each Rigidbody then damages 
    /// that GameObject based on the distance between the explosion the closest collider. Quite intensive and experimental but the most accuracte.
    /// </summary>
    void HighAccuracyAOE()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, m_aoeRange, m_aoeMask);
        Rigidbody[] unique = colliders.GetAttachedRigidbodies().GetUniqueOnly();


        // Cache values for the sake of efficiency, also avoid Vector3.Distance by squaring ranges
        float distance = 0f, damage = 0f,
                aoeMaxDamageRange = m_aoeMaxDamageRange.Squared(),
                maxDistance = m_aoeRange.Squared() - aoeMaxDamageRange;

        foreach (Rigidbody mob in unique)
        {
            // Ensure the distance will equate to 0f - 1f for the Lerp function
            distance = (transform.position - colliders.GetClosestPointFromRigidbody(mob, transform.position)).sqrMagnitude - aoeMaxDamageRange;
            distance = Mathf.Clamp(distance, 0f, maxDistance);

            damage = Mathf.Lerp(m_bulletDamage, m_bulletMinDamage, distance / maxDistance);
            // Debug.Log ("Hitting " + mob.name + " (" + distance + ") for " + ((int) damage) + " damage");

            DamageMob(mob.gameObject, (int)damage);
            AddExplosiveForce(mob);
        }
    }


    /// <summary>
    /// Using OverlapSphere it gets a collection of unique Rigidbody objects and just damages it based on the distance between the explosion
    /// and the core rigidbody. Can be extremely inaccurate because a ship could be much larger than the AoE size.
    /// </summary>
    void LowAccuracyAOE(bool damageByDistance)
    {
        // Obtain the rigidbodies to damage
        Rigidbody[] effected = Physics.OverlapSphere(transform.position, m_aoeRange, m_aoeMask).GetAttachedRigidbodies().GetUniqueOnly();

        // Attempt to optimise speed by declaring variables outside of the loop
        float distance = 0f, damage = 0f,
                aoeMaxDamageRange = m_aoeMaxDamageRange.Squared(),
                maxDistance = m_aoeRange.Squared() - aoeMaxDamageRange;

        foreach (Rigidbody mob in effected)
        {
            if (damageByDistance)
            {
                // Ensure the distance will equate to 0f - 1f for the Lerp function
                distance = (transform.position - mob.position).sqrMagnitude - aoeMaxDamageRange;
                distance = Mathf.Clamp(distance, 0f, maxDistance);
                damage = Mathf.Lerp(m_bulletDamage, m_bulletMinDamage, distance / maxDistance);
                // Debug.Log ("Hitting " + mob.name + " (" + distance + ") for " + ((int) damage) + " damage");
            }

            else
            {
                damage = m_bulletDamage;
            }

            DamageMob(mob.gameObject, (int)damage);
            AddExplosiveForce(mob);
        }
    }


    // Peforms any special force synchronisation required for different types of objects
    void AddExplosiveForce(Rigidbody mob)
    {
        // Use the mobs z position to stop the force causing enemies to move upwards all the time
        Vector3 position = new Vector3(transform.position.x, transform.position.y, mob.transform.position.z);
        mob.AddCustomExplosionForce(position, m_aoeRange, m_aoeMinExplosiveForce, m_aoeMaxExplosiveForce);

        switch (mob.gameObject.layer)
        {
            case Layers.asteroid:
                SyncAsteroid(mob);
                break;

            case Layers.player:
                SyncPlayer(mob);
                break;
        }
    }


    // Asteroids need to sync their velocity over the network after waiting for a FixedUpdate() call
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
            script.ApplyExplosiveForceOverNetwork(transform.position.x, transform.position.y, m_aoeRange, m_aoeMinExplosiveForce, m_aoeMaxExplosiveForce);
        }
    }
}

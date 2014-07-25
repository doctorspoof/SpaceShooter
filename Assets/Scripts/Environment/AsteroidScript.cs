using UnityEngine;
using System.Collections;

/// <summary>
/// The class used to provide all asteroid functionality, this includes allowing fragements to be spawned, synchronising velocity/position, etc.
/// </summary>
[RequireComponent (typeof (Rigidbody))]
public sealed class AsteroidScript : MonoBehaviour 
{
    ///////////////////////////////////
    /// Unity modifiable attributes ///
    ///////////////////////////////////



    // Initial setup
	[SerializeField, Range (-10f, 10f)] float[] m_rotationSpeedRange = new float[2] {-2f, 2f};  // The range of values the rotation speed can be
	[SerializeField, Range (1f, 10f)] float[] m_asteroidScaleRange = new float[2] {1f, 2.5f};   // The range of values the asteroid scale can be

    [SerializeField, Range (0f, 120f)] float m_velocitySyncInterval = 15f;                      // How often to synchronise the velocity of the asteroid over the network


    // Fragmentation
    [SerializeField] GameObject[] m_asteroidPrefabs;                        // A prefab will randomly be selected from this array
	
    [SerializeField] int m_splittingFragments = 4;                          // How many asteroids to split into upon destruction
    [SerializeField] float m_minimumMass = 0.02f;                           // How small the asteroid can get
	
    [SerializeField] float m_fragmentDistance = 0.2f;                       // How far away from the parent asteroid the new asteroid should spawn
	[SerializeField] float m_fragmentSplitForce = 1.5f;                     // How much additional force to apply when splitting
	
    [SerializeField, Range (0f, 1f)] float m_velocityToMaintain = 0.75f;    // How much of the original velocity to maintain upon splitting as a percentage



    /////////////////////
    /// Internal data ///
    /////////////////////



	bool m_isFirstAsteroid = true;  // Stops the asteroids from increasing size when splitting
    bool m_hasSplit = false;        // Ensures that the asteroid won't attempt to split whilst already splitting



    //////////////////////////
    /// Behavior functions ///
    //////////////////////////



    void Awake() 
    {
        // Perform initial setup
        if (Network.isServer)
        {
            if (m_isFirstAsteroid)
            {
                // Stop fragments from performing the same operation
                m_isFirstAsteroid = false;
                
                InitialScaleSetup();

                InitialRotationSetup();
            }
            
            // Ensure asteroids stay synchronised over the network
            StartCoroutine (PersistentAsteroidSync (Random.Range (0f, m_velocitySyncInterval), m_velocitySyncInterval));
        }

        // Check if any asteroid prefabs exist
        if (m_asteroidPrefabs.Length == 0)
        {
            Debug.LogError ("AsteroidScript has no asteroid prefabs, could cause unwanted problems.");
        }
    }


	void OnCollisionEnter (Collision collision)
    {
        if (Network.isServer && collision.gameObject.tag != "Player")
        {
            networkView.RPC ("SyncVelocity", RPCMode.Others, rigidbody.velocity, this.transform.position.x, this.transform.position.y);
        }
	}
    
    
    void OnCollisionExit (Collision collision)
    {
        if (Network.isServer && collision.gameObject.tag != "Player")
        {
            networkView.RPC ("SyncVelocity", RPCMode.Others, rigidbody.velocity, this.transform.position.x, this.transform.position.y);
        }
    }


	void OnCollisionStay (Collision collision)
	{
		if (Network.isServer && collision.gameObject.tag == "Player")
		{
			networkView.RPC ("SyncVelocity", RPCMode.Others, rigidbody.velocity, this.transform.position.x, this.transform.position.y);
		}
	}



    ///////////////////////////////
    /// Initial setup functions ///
    ///////////////////////////////



    /// <summary>
    /// Determines and propagates the initial size and mass which the asteroid should be based on m_asteroidScaleRange
    /// </summary>
    void InitialScaleSetup()
    {
        // Ensure enough values are in the array
        if (m_asteroidScaleRange.Length >= 2)
        {
            // Choose a random value for the multiplier
            float multiplier = Random.Range (m_asteroidScaleRange[0], m_asteroidScaleRange[1]);
            
            // Propagate the value to all clients
            networkView.RPC ("PropagateScaleAndMassMultiply", RPCMode.All, multiplier);
        }
        
        else
        {
            Debug.LogError ("Unable to set the intial scale for " + name);
        }
    }
    

    /// <summary>
    /// Determines and propagates the initial rotation for the asteroid based on m_rotationSpeedRange
    /// </summary>
    void InitialRotationSetup()
    {
        // Ensure enough values exist in the array
        if (m_rotationSpeedRange.Length >= 2)
        {
            // Calculate the correct torque to add
            float multiplier = Random.Range (m_rotationSpeedRange[0], m_rotationSpeedRange[1]);
            
            // Make sure every client gets the correct multiplier
            networkView.RPC ("PropagateForwardTorqueMultiply", RPCMode.All, multiplier);
        }
        
        else  
        {
            Debug.LogError ("Unable to set the initial rotation speed for " + name);
        }
    }
    

    /// <summary>
    /// Allows external propagation of the scale and mass of the asteroid, useful for synchronising fragment sizes.
    /// </summary>
    /// <param name="scale">The desired local scale.</param>
    /// <param name="mass">The desired mass.</param>
    public void TellToPropagateScaleAndMass (Vector3 scale, float mass)
    {
        networkView.RPC ("PropagateScaleAndMass", RPCMode.All, scale, mass);
    }
    

    /// <summary>
    /// Sets the raw values for the local scale and rigidbody mass, useful for synchronising over the network
    /// </summary>
    /// <param name="scale">The desired local scale of the object</param>
    /// <param name="mass">The desired rigidbody mass</param>
    [RPC] void PropagateScaleAndMass (Vector3 scale, float mass)
    {
        this.rigidbody.mass = mass;
        this.transform.localScale = scale;
    }
    

    /// <summary>
    /// Propagates a multiplier which the rigidbody mass and localScale are multiplied by
    /// </summary>
    /// <param name="multiplier">How much the mass and localScale should be multiplied</param>
    [RPC] void PropagateScaleAndMassMultiply (float multiplier)
    {
        this.rigidbody.mass *= multiplier;
        this.transform.localScale = new Vector3 (transform.localScale.x * multiplier, transform.localScale.y * multiplier, transform.localScale.z);
    }


    /// <summary>
    /// Propagates a multiplier to create asteroid rotation based on forqard torque
    /// </summary>
    /// <param name="multiplier">How much forward torque to apply</param>
    [RPC] void PropagateForwardTorqueMultiply (float multiplier)
    {
        rigidbody.AddTorque (Vector3.forward * multiplier);
    }



    ///////////////////////////////
    /// Network synchronisation ///
    /// ///////////////////////////
   


    /// <summary>
    /// A coroutine which ensures that asteroids are synchronised in position and velocity over a period of time.
    /// </summary>
    /// <param name="startTime">Allows for a shorter initial wait time so that asteroids synchronisation can be done at different intervals.</param>
    /// <param name="timeToWait">The actual time to wait before synchronising, this is in seconds.</param>
    IEnumerator PersistentAsteroidSync (float startTime, float timeToWait)
    {
        if (Network.isServer)
        {
            if (startTime > timeToWait)
            {
                startTime.Swap (ref startTime, ref timeToWait);
            }
            
            yield return new WaitForSeconds (timeToWait - startTime);
            
            while (true)
            {
                networkView.RPC ("SyncVelocity", RPCMode.Others, rigidbody.velocity, rigidbody.position.x, rigidbody.position.y);
                yield return new WaitForSeconds (timeToWait);
            }
        }
    }
    
    
    /// <summary>
    /// Simply calls SyncVelocity if we are the server, only really useful for Invoking after a certain time period.
    /// </summary>
    void SyncVelocityWithOthers()
    {
        if (Network.isServer)
        {
            networkView.RPC ("SyncVelocity", RPCMode.Others, rigidbody.velocity, transform.position.x, transform.position.y);
        }
    }
    
    /// <summary>
    /// Allows for the velocity to be synchronised after a delay, useful for when force is externally applied such as explosive force from missiles.
    /// Setting a delay ensures that the force gets applied THEN synchronised over the network.
    /// </summary>
    /// <param name="delay">Delay.</param>
    public void DelayedVelocitySync (float delay = 0f)
    {
        if (Network.isServer)
        {           
            if (delay > 0f)
            {
                Invoke ("SyncVelocityWithOthers", delay);
            }
        }
    }


    /// <summary>
    /// Used to set the position and rigidbody velocity of the asteroid.
    /// </summary>
    /// <param name="velocity">The desired velocity.</param>
    /// <param name="xPos">The desired position on the X axis.</param>
    /// <param name="yPos">the desired position on the Y axis.</param>
	[RPC] void SyncVelocity (Vector3 velocity, float xPos, float yPos)
    {
		rigidbody.velocity = velocity;
		this.transform.position = new Vector3 (xPos, yPos, 10.0f);
	}



    //////////////////////////
    /// Asteroid splitting ///
    //////////////////////////
     


    /// <summary>
    /// Causes the asteroid to split into smaller fragments, assuming the mass isn't below the minimum chosen from m_minimumMass.
    /// </summary>
    /// <param name="hitter">Allows the spawn position and directional velocity to be calculated from a Transform.</param>
	public void SplitAsteroid (Transform hitter)
	{
		if (!m_hasSplit)
		{
			Vector3 shotDirection, impactForce;
			
			shotDirection = hitter != null ?
								(transform.position - hitter.position).normalized :
								Vector3.forward;
			
			impactForce = hitter && hitter.rigidbody ? 
								shotDirection * hitter.rigidbody.velocity.magnitude :
								shotDirection * m_fragmentSplitForce;

			PerformSplit (shotDirection, impactForce);
        }
    }


    /// <summary>
    /// Causes the asteroid to split into smaller fragments, assuming the mass isn't below the minimum chosen from m_minimumMass.
    /// </summary>
    /// <param name="hitter">Allows the spawn position and directional velocity to be calculated from a Vector3</param>
	public void SplitAsteroid (Vector3 hitterPosition)
	{
		if (!m_hasSplit)
		{
			Vector3 shotDirection, impactForce;

			shotDirection = (transform.position - hitterPosition).normalized;

			impactForce = shotDirection * m_fragmentSplitForce;

			PerformSplit (shotDirection, impactForce);
		}
	}


    /// <summary>
    /// Actually performs the splitting of the asteroid, this will calculate where fragments should be spawned and their intiial velocity.
    /// </summary>
    /// <param name="shotDirection">The direction where the asteroid was hit.</param>
    /// <param name="impactForce">The impact force to be applied.</param>
	void PerformSplit (Vector3 shotDirection, Vector3 impactForce)
	{
		if (!m_hasSplit)
		{
			m_hasSplit = true;
			
			if (rigidbody.mass / m_splittingFragments > m_minimumMass)
			{
				// Create a smooth semi-circle spray based on the number of splits to perform
				if (m_splittingFragments > 1)
				{
					// Fixes an issue with the sun causing asteroids to spawn on top of each other
					if (shotDirection == transform.forward)
					{
						shotDirection = transform.up;
					}
					
					Vector3 right = Vector3.Cross (shotDirection, transform.forward);
					
					
					for (int i = 0; i < m_splittingFragments; ++i)
					{
						// Create an interpolated multiplier between -1f and 1f
						float rightMultiplier = Mathf.Lerp (-1f, 1f, i / (float) (m_splittingFragments - 1));
						
						
						// Forward multiplier should go from 0 to 1 and then back to 0 to create a sphere
						float forwardMultiplier = i / (float) (m_splittingFragments - 1);
						
						if (forwardMultiplier > 0.5f)
						{
							forwardMultiplier = 0.5f - (forwardMultiplier - 0.5f);
						}
						
						forwardMultiplier *= 2f;
						
						
						// The distance away from the initial position to spawn
						Vector3 spawnModifier = (right * rightMultiplier + shotDirection * forwardMultiplier) * (m_fragmentDistance * transform.localScale.x);
						spawnModifier.z = 0f;
						
						impactForce += spawnModifier.normalized * m_fragmentSplitForce;
						
						// Finally spawn the asteroid
						SpawnAsteroid (spawnModifier, impactForce);
					}
				}
			}
			
			Network.Destroy (gameObject);
		}
	}
	

    /// <summary>
    /// Spawns an asteroid fragment over the network.
    /// </summary>
    /// <param name="spawnModifier">The translation applied to transform.position of the current asteroid.</param>
    /// <param name="impactForce">The impact force to be applied to the spawned fragment.</param>
	void SpawnAsteroid (Vector3 spawnModifier, Vector3 impactForce)
	{
		// Obtain a random prefab
		GameObject asteroid = RandomPrefab();
		if (asteroid != null)
		{
			// Instantiate the asteroid
			asteroid = (GameObject) Network.Instantiate (asteroid, transform.position + spawnModifier, transform.rotation, 0);
			
			// Add the impact force to keep the asteroid moving
			asteroid.rigidbody.velocity = rigidbody.velocity * m_velocityToMaintain;
			asteroid.rigidbody.AddForce (impactForce);
			
			// Scale the asteroid correctly
			AsteroidScript script = asteroid.GetComponent<AsteroidScript>();
			if (script != null)
			{
				script.TellToPropagateScaleAndMass (transform.localScale / m_splittingFragments, rigidbody.mass / m_splittingFragments);
                script.DelayedVelocitySync (Time.fixedDeltaTime);
                script.m_isFirstAsteroid = false;
			}
		}
		
		else
		{
			Debug.LogError ("AsteroidScript couldn't generate a random prefab for splitting.");
		}
	}
	
	
    /// <summary>
    /// Chooses a random asteroid from the m_asteroidPrefabs array for usage by SpawnAsteroid().
    /// </summary>
    /// <returns>The selected prefab, returns null if one can't be found.</returns>
	GameObject RandomPrefab()
	{
		// Ensure the prefab array is an appropriate size
		if (m_asteroidPrefabs.Length > 0)
		{
			// Don't bother continuing after 5 attempts
			for (int i = 0; i < 5; ++i)
			{
				GameObject asteroid = m_asteroidPrefabs[Random.Range (0, m_asteroidPrefabs.Length)];
				if (asteroid != null)
				{
					return asteroid;
				}
			}
		}
		
		// Hopefully this will never happen
		return null;
	}
}

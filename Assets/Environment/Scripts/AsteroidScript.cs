using UnityEngine;
using System.Collections;

public class AsteroidScript : MonoBehaviour 
{
	// A prefab will randomly be selected from this array
	[SerializeField]
	GameObject[] m_asteroidPrefabs;
	
	// The range of values the rotation speed can be
	[SerializeField]
	float[] m_rotationSpeedRange = new float[2] {-2f, 2f};
	
	// The range of values the asteroid scale can be
	[SerializeField]
	float[] m_asteroidScaleRange = new float[2] {1f, 2.5f};
	
	// How many asteroids to split into upon destruction
	[SerializeField]
	int m_splittingFragments = 2;
	
	// How small the asteroid can get
	[SerializeField]
	float m_minimumMass = 0.02f;
	
	// How far away from the parent asteroid the new asteroid should spawn
	[SerializeField]
	float m_fragmentDistance = 0.2f;
	
	// How much additional force to apply when splitting
	[SerializeField]
	float m_fragmentSplitForce = 1.5f;
	
	// How much of the original velocity to maintain upon splitting
	[SerializeField, Range (0f, 1f)]
	float m_velocityToMaintain = 0.75f;
	
	// Stops the asteroids from increasing size when splitting
	[HideInInspector] 
	public bool isFirstAsteroid = true;
    


	void OnCollisionEnter(Collision collision)
    {
        if(Network.isServer && collision.gameObject.tag != "Player")
        {
            networkView.RPC ("SyncVelocity", RPCMode.Others, rigidbody.velocity, this.transform.position.x, this.transform.position.y);
        }
		/*if(collision.gameObject.layer == Layers.player)
		{
			if(collision.transform.root.GetComponent<CapitalShipScript>())
			{
				collision.transform.root.GetComponent<CapitalShipScript>().BeginShaderCoroutine(this.transform.position);
			}
			else if(collision.transform.root.GetComponent<PlayerControlScript>())
			{
				collision.transform.root.GetComponent<PlayerControlScript>().BeginShaderCoroutine(this.transform.position);
			}
			else if(collision.transform.root.GetComponent<EnemyScript>())
			{
				collision.transform.root.GetComponent<EnemyScript>().BeginShaderCoroutine(this.transform.position);
			}
		}*/
	}


	void OnCollisionStay(Collision collision)
	{
		if(Network.isServer && collision.gameObject.tag == "Player")
		{
			networkView.RPC ("SyncVelocity", RPCMode.Others, rigidbody.velocity, this.transform.position.x, this.transform.position.y);
		}

		/*if(collision.gameObject.layer == Layers.player)
		{
			//Debug.Log ("Shield should wibble!");
			if(collision.transform.root.GetComponent<CapitalShipScript>())
			{
				collision.transform.root.GetComponent<CapitalShipScript>().BeginShaderCoroutine(this.transform.position);
			}
			else if(collision.transform.root.GetComponent<PlayerControlScript>())
			{
				collision.transform.root.GetComponent<PlayerControlScript>().BeginShaderCoroutine(this.transform.position);
			}
			else if(collision.transform.root.GetComponent<EnemyScript>())
			{
				collision.transform.root.GetComponent<EnemyScript>().BeginShaderCoroutine(this.transform.position);
			}
		}*/
	}


	void OnCollisionExit(Collision collision)
	{
		if(Network.isServer && collision.gameObject.tag != "Player")
		{
			networkView.RPC ("SyncVelocity", RPCMode.Others, rigidbody.velocity, this.transform.position.x, this.transform.position.y);
		}
	}
    
    
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


	[RPC]
	void SyncVelocity(Vector3 vel, float xPos, float yPos)
    {
        Debug.Log ("Received requested to synchronise the velocity of: " + name);
		rigidbody.velocity = vel;
		this.transform.position = new Vector3(xPos, yPos, 10.0f);
	}
	
	// Use this for initialization
	void Start () 
	{
		if (m_splittingFragments > 1 && m_asteroidPrefabs.Length == 0)
		{
			Debug.LogError ("AsteroidScript has no asteroid prefabs, could cause unwanted problems.");
		}
		
		if (Network.isServer && isFirstAsteroid)
		{
			if (m_asteroidScaleRange.Length > 1)
			{
				isFirstAsteroid = false;
				float multiplier = Random.Range (m_asteroidScaleRange[0], m_asteroidScaleRange[1]);
				networkView.RPC ("PropagateScaleAndMassMultiply", RPCMode.All, multiplier);
				Vector3 torque = Vector3.forward * Random.Range (m_rotationSpeedRange[0], m_rotationSpeedRange[1]);
				rigidbody.AddTorque (torque);
				networkView.RPC ("PropagateInitialTorque", RPCMode.Others, torque);
			}
			
			else
			{
				Debug.LogError ("Incorrect .Length in AsteroidScript.m_asteroidScaleRange");
			}

			sendCounter = Random.Range(0, 4);
        }
        
        StartCoroutine (PersistentAsteroidSync (Random.Range (0f, 15f), 15f));
    }
    

	[RPC]
	void PropagateInitialTorque(Vector3 torque)
	{
		rigidbody.AddTorque(torque);
	}


	/*void FixedUpdate()
	{
		if(Network.isClient)
		{
			if(rigidbody.velocity != Vector3.zero)
			{
				rigidbody.MovePosition(rigidbody.position + (rigidbody.velocity*Time.deltaTime*Time.deltaTime));
			}
		}
	}*/
	int sendCounter = 0;
	//Vector3 lastVel = Vector3.zero;
	void OnSerializeNetworkView(BitStream stream, NetworkMessageInfo info)
	{
		float posX = this.transform.position.x;
		float posY = this.transform.position.y;

		//float rotZ = this.transform.rotation.eulerAngles.z;

		//Vector3 velocity = rigidbody.velocity;

		if(stream.isWriting)
		{
			sendCounter++;
			//when counter = 5, turns out to ~1 second between updates (assuming no drops)
			if(sendCounter > 4)
			{
				sendCounter = 0;
				stream.Serialize(ref posX);
				stream.Serialize(ref posY);
				//stream.Serialize(ref rotZ);

				//stream.Serialize(ref velocity);

			}
		}

		else
		{
			stream.Serialize(ref posX);
			stream.Serialize(ref posY);
			//stream.Serialize(ref rotZ);
			//stream.Serialize(ref velocity);

			//Debug.Log ("Recieved velocity:" + velocity);

			this.transform.position = new Vector3(posX, posY, 10.0f);
			//this.transform.rotation = Quaternion.Euler(0, 0, rotZ);

			//rigidbody.velocity = velocity;

			//Begin interp
			//StartCoroutine(BeginInterp());
		}
	}


	float t = 0;
	IEnumerator BeginInterp()
	{
		t = 0;

		while(t < 1)
		{
			t += Time.deltaTime;
            
			rigidbody.MovePosition(rigidbody.position + (rigidbody.velocity * Time.deltaTime * Time.deltaTime));
			yield return 0;
		}
	}


	public void SplitAsteroid (Transform hitter)
	{
		if (!m_hasSplit)
		{
			Vector3 shotDirection, impactForce;
			
			shotDirection = hitter ?
								(transform.position - hitter.position).normalized :
								Vector3.forward;
			
			impactForce = hitter && hitter.rigidbody ? 
								shotDirection * hitter.rigidbody.velocity.magnitude :
								shotDirection * m_fragmentSplitForce;

			PerformSplit (shotDirection, impactForce);
        }
    }


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


	
	bool m_hasSplit = false;
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
	
	void SpawnAsteroid (Vector3 spawnModifier, Vector3 impactForce)
	{
		// Obtain a random prefab
		GameObject asteroid = RandomPrefab();
		if (asteroid)
		{
			// Instantiate the asteroid
			asteroid = (GameObject) Network.Instantiate (asteroid, transform.position + spawnModifier, transform.rotation, 0);
			
			// Add the impact force to keep the asteroid moving
			asteroid.rigidbody.velocity = rigidbody.velocity * m_velocityToMaintain;
			asteroid.rigidbody.AddForce (impactForce);
			
			// Scale the asteroid correctly
			AsteroidScript script = asteroid.GetComponent<AsteroidScript>();
			if (script)
			{
				script.TellToPropagateScaleAndMass (transform.localScale / m_splittingFragments, rigidbody.mass / m_splittingFragments);
                script.DelayedVelocitySync (Time.fixedDeltaTime);
                script.isFirstAsteroid = false;
			}
		}
		
		else
		{
			Debug.LogError ("AsteroidScript couldn't generate a random prefab for splitting.");
		}
	}
	
	
	GameObject RandomPrefab()
	{
		// Ensure the prefab array is an appropriate size
		if (m_asteroidPrefabs.Length > 0)
		{
			// Don't bother continuing after 5 attempts
			for (int i = 0; i < 5; ++i)
			{
				GameObject asteroid = m_asteroidPrefabs[Random.Range (0, m_asteroidPrefabs.Length)];
				if (asteroid)
				{
					return asteroid;
				}
			}
		}
		
		// Hopefully this will never happen
		return null;
	}

	
	public void AlertScaleAssigned(float scale)
	{
		networkView.RPC ("PropagateScaleAndMassMultiply", RPCMode.All, scale);
	}


	public void TellToPropagateScaleAndMass (Vector3 scale, float mass)
	{
		networkView.RPC ("PropagateScaleAndMass", RPCMode.All, scale, mass);
	}


	public void DelayedVelocitySync (float delay = 0f)
	{
		if (Network.isServer)
		{			
			if (delay > 0f)
			{
				Invoke ("SyncVelocityWithOthers", delay);
			}

			else
			{
				SyncVelocityWithOthers();
			}
		}
	}


	void SyncVelocityWithOthers()
	{
		networkView.RPC ("SyncVelocity", RPCMode.Others, rigidbody.velocity, transform.position.x, transform.position.y);
	}


	[RPC]
	void PropagateScaleAndMass (Vector3 scale, float mass)
	{
		this.rigidbody.mass = mass;
		this.transform.localScale = scale;
	}
	
	
	[RPC]
	void PropagateScaleAndMassMultiply (float multiplier)
	{
		this.rigidbody.mass *= multiplier;
		this.transform.localScale = new Vector3 (transform.localScale.x * multiplier, transform.localScale.y * multiplier, transform.localScale.z);
	}
}

using UnityEngine;
using System.Collections.Generic;



/// <summary>
/// Asteroid manager is in charge of the spawning of asteroids. Given a range
/// </summary>
public sealed class AsteroidManager : MonoBehaviour 
{
    #region Unity modifiable values

	[SerializeField]                    AsteroidScript[] m_asteroidRef;     // A list of asteroid prefabs used for spawning the initial asteroids.
	[SerializeField, Range (0f, 500f)]  float m_range;                      // How wide of an area to spawn asteroids
	[SerializeField, Range (0f, 25f)]   float m_ringThickness;              // How thick a ring belt should be
	[SerializeField, Range (0, 800)]    int m_numAsteroids;                 // How many asteroids to spawn each time
	[SerializeField]                    bool m_shouldBeRingBelt = true;     // Determines whether to spawn a ring of asteroids or a circular field
    [SerializeField]                    bool m_testAsteroidSpawn = false;   // Test function to spawn asteroids + see where they lie
    
    [SerializeField]                    Texture m_asteroidManagerRingBlip;      // GUI will access this at runtime
    [SerializeField]                    Texture m_asteroidManagerFieldBlip;      // GUI will access this at runtime
	
    //Setters here, because procgen will need to alter these, and is too dumb to use the editor
    #region Setters
    public Texture GetMinimapBlip()
    {
        if(m_shouldBeRingBelt)
            return m_asteroidManagerRingBlip;
        else
            return m_asteroidManagerFieldBlip;
    }
    public float GetRange()
    {
        return m_range;
    }
    public void SetRange(float range_)
    {
        m_range = range_;
    }
    public void SetThickness(float thickness_)
    {
        m_ringThickness = thickness_;
    }
    public void SetAsteroidNum(int asteroids_)
    {
        m_numAsteroids = asteroids_;
    }
    public bool GetIsRing()
    {
        return m_shouldBeRingBelt;
    }
    public void SetIsRing(bool isRing_)
    {
        m_shouldBeRingBelt = isRing_;
    }
    public void SetTestSpawns(bool test_)
    {
        m_testAsteroidSpawn = test_;
    }
    public void ForceSpawnAsteroidsTestSP()
    {
        SpawnAsteroidsSPTEST();
    }
    #endregion
    #endregion



    #region Internal data
	static GameObject m_asteroids;  // The GameObject to parent each spawned asteroid to. Cleans up the scene view.
    #endregion


    #region Behavior functions

	void Awake() 
	{
        // Make a parent GameObject to organise the scene heirarchy
		if (m_asteroids == null)
        {
            m_asteroids = new GameObject ("Asteroids");
            m_asteroids.tag = "AsteroidParent";
            m_asteroids.AddComponent<NetworkView>();
            m_asteroids.networkView.stateSynchronization = NetworkStateSynchronization.Off;
        }

        // Ensure we have a clean array of asteroids
        RemoveNullsFromAsteroidArray();
	}
    
    void Update()
    {
        if(m_testAsteroidSpawn)
        {
            m_testAsteroidSpawn = false;
            SpawnAsteroidsSPTEST();
        }
    }

    #endregion
	

    #region Setup functionality

    void RemoveNullsFromAsteroidArray()
    {
        List<AsteroidScript> prefabs = new List<AsteroidScript>(0);

        for (int i = 0; i < m_asteroidRef.Length; ++i)
        {
            if (m_asteroidRef[i] != null)
            {
                prefabs.Add (m_asteroidRef[i]);
            }
        }

        m_asteroidRef = prefabs.ToArray();
    }

    #endregion


    #region Spawning functionality

    /// <summary>
    /// Spawn an asteroid belt with the assigned values. 
    /// Note: does not destroy the previous belt if it currently exists.
    /// </summary>
	public void SpawnAsteroids()
	{
		if (m_shouldBeRingBelt)
		{
            SpawnRingBelt();
		}

		else
		{
			SpawnCircleField();
		}
	}
    public void SpawnAsteroidsSPTEST()
    {
        if(m_shouldBeRingBelt)
        {
            SpawnRingBeltSP();
        }
        else
        {
            SpawnCircleFieldSP();
        }
    }


    void SpawnRingBelt()
    {
        // Calculate the angle to increment by
        float theta = (Mathf.PI * 2f) / m_numAsteroids;

        // Attempt to increase speed by avoiding variable creation
        Vector2 pos;
        Quaternion rot;
        GameObject asteroid;

        for (int i = 0; i < m_numAsteroids; ++i)
        {
            // Create evenly spaced asteroids
            pos = new Vector2 (transform.position.x, transform.position.y) + 
                  new Vector2 ((m_range + Random.Range (-m_ringThickness, m_ringThickness)) * Mathf.Cos (theta * i), 
                              (m_range + Random.Range (-m_ringThickness, m_ringThickness)) * Mathf.Sin (theta * i));

            // Assign each asteroid a random rotation
            rot = Random.rotation;
            rot = Quaternion.Euler (new Vector3 (0f, 0f, rot.eulerAngles.z));

            // Setup the asteroid
            asteroid = (GameObject) Network.Instantiate(m_asteroidRef[Random.Range (0, m_asteroidRef.Length)].gameObject, new Vector3(pos.x, pos.y, 10f), rot, 0);
            asteroid.transform.parent = m_asteroids.transform;
        }
    }
    void SpawnRingBeltSP()
    {
        // Calculate the angle to increment by
        float theta = (Mathf.PI * 2f) / m_numAsteroids;
        
        // Attempt to increase speed by avoiding variable creation
        Vector2 pos;
        Quaternion rot;
        GameObject asteroid;
        
        for (int i = 0; i < m_numAsteroids; ++i)
        {
            // Create evenly spaced asteroids
            pos = new Vector2 (transform.position.x, transform.position.y) + 
                  new Vector2 ((m_range + Random.Range (-m_ringThickness, m_ringThickness)) * Mathf.Cos (theta * i), 
                              (m_range + Random.Range (-m_ringThickness, m_ringThickness)) * Mathf.Sin (theta * i));
            
            // Assign each asteroid a random rotation
            rot = Random.rotation;
            rot = Quaternion.Euler (new Vector3 (0f, 0f, rot.eulerAngles.z));
            
            // Setup the asteroid
            asteroid = Instantiate(m_asteroidRef[Random.Range (0, m_asteroidRef.Length)].gameObject, new Vector3(pos.x, pos.y, 10f), rot) as GameObject;
            asteroid.transform.parent = m_asteroids.transform;
        }
    }


    void SpawnCircleField()
    {
        // Attempt to increase speed by avoiding variable creation
        Vector2 pos;
        Quaternion rot;
        GameObject asteroid;

        for(int i = 0; i < m_numAsteroids; ++i)
        {
            // Use Unity's beautiful insideUnitCircle function to calculate a random point in the circle
            pos = new Vector2(transform.position.x, transform.position.y) + (Random.insideUnitCircle * m_range);

            // Assign each asteroid a random rotation
            rot = Random.rotation;
            rot = Quaternion.Euler (new Vector3 (0f, 0f, rot.eulerAngles.z));

            // Setup the asteroid
            asteroid = (GameObject) Network.Instantiate (m_asteroidRef[Random.Range (0, m_asteroidRef.Length)].gameObject, new Vector3 (pos.x, pos.y, 10f), rot, 0);
            asteroid.transform.parent = m_asteroids.transform;
        }
    }
    void SpawnCircleFieldSP()
    {
        // Attempt to increase speed by avoiding variable creation
        Vector2 pos;
        Quaternion rot;
        GameObject asteroid;
        
        Debug.Log ("Running field for #" + m_numAsteroids + " asteroids");
        for(int i = 0; i < m_numAsteroids; ++i)
        {
            // Use Unity's beautiful insideUnitCircle function to calculate a random point in the circle
            pos = new Vector2(transform.position.x, transform.position.y) + (Random.insideUnitCircle * m_range);
            
            // Assign each asteroid a random rotation
            rot = Random.rotation;
            rot = Quaternion.Euler (new Vector3 (0f, 0f, rot.eulerAngles.z));
            
            // Setup the asteroid
            asteroid = Instantiate (m_asteroidRef[Random.Range (0, m_asteroidRef.Length)].gameObject, new Vector3 (pos.x, pos.y, 10f), rot) as GameObject;
            asteroid.transform.parent = m_asteroids.transform;
        }
    }

    #endregion
}

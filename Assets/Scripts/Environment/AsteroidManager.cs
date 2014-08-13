using UnityEngine;
using System.Collections.Generic;



/// <summary>
/// Asteroid manager is in charge of the spawning of asteroids. Given a range
/// </summary>
public sealed class AsteroidManager : MonoBehaviour 
{
    #region Unity modifiable values

	[SerializeField]                    AsteroidScript[] m_asteroidRef; // A list of asteroid prefabs used for spawning the initial asteroids.
	[SerializeField, Range (0f, 500f)]  float m_range;                  // How wide of an area to spawn asteroids
	[SerializeField, Range (0f, 25f)]   float m_ringThickness;          // How thick a ring belt should be
	[SerializeField, Range (0, 800)]    int m_numAsteroids;             // How many asteroids to spawn each time
	[SerializeField]                    bool m_shouldBeRingBelt = true; // Determines whether to spawn a ring of asteroids or a circular field
	
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
        }

        // Ensure we have a clean array of asteroids
        RemoveNullsFromAsteroidArray();
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
            pos = new Vector2 ((m_range + Random.Range (-m_ringThickness, m_ringThickness)) * Mathf.Cos (theta * i), 
                               (m_range + Random.Range (-m_ringThickness, m_ringThickness)) * Mathf.Sin (theta * i));

            // Assign each asteroid a random rotation
            rot = Random.rotation;
            rot = Quaternion.Euler (new Vector3 (0f, 0f, rot.eulerAngles.z));

            // Setup the asteroid
            asteroid = (GameObject) Network.Instantiate(m_asteroidRef[Random.Range (0, m_asteroidRef.Length)].gameObject, new Vector3(pos.x, pos.y, 10f), rot, 0);
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
            pos = Random.insideUnitCircle * m_range;

            // Assign each asteroid a random rotation
            rot = Random.rotation;
            rot = Quaternion.Euler (new Vector3 (0f, 0f, rot.eulerAngles.z));

            // Setup the asteroid
            asteroid = (GameObject) Network.Instantiate (m_asteroidRef[Random.Range (0, m_asteroidRef.Length)], new Vector3 (pos.x, pos.y, 10f), rot, 0);
            asteroid.transform.parent = m_asteroids.transform;
        }
    }

    #endregion
}

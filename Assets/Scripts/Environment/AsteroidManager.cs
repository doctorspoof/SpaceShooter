using UnityEngine;
using System.Collections;

public class AsteroidManager : MonoBehaviour 
{
	[SerializeField]                GameObject [] m_asteroidRef;
	[SerializeField, Range(0, 500)] float m_range;
	[SerializeField, Range(0, 25)]  float m_ringThickness;
	[SerializeField, Range(0, 800)] int m_numAsteroids;
	[SerializeField]                bool m_shouldBeRingBelt = true;
	[SerializeField]                bool m_shouldSpawnNow = false;
	
	static GameObject asteroids;
	
	void Start () 
	{
		if(asteroids == null)
			asteroids = new GameObject("Asteroids");
	}
	
	void Update () 
	{
		if(m_shouldSpawnNow)
		{
			SpawnAsteroids();
			m_shouldSpawnNow = false;
		}
	}
	
    /// <summary>
    /// Spawn an asteroid belt with the assigned values. 
    /// Note: does not destroy the previous belt if it currently exists.
    /// </summary>
	public void SpawnAsteroids()
	{
		if(m_shouldBeRingBelt)
		{
			float theta = (Mathf.PI * 2) / m_numAsteroids;
			for(int i = 0; i < m_numAsteroids; i++)
			{
				//Evenly spaced
				Vector2 pos = new Vector2((m_range + Random.Range(-m_ringThickness, m_ringThickness)) * Mathf.Cos(theta * i), (m_range + Random.Range(-m_ringThickness, m_ringThickness)) * Mathf.Sin(theta * i));
				Quaternion rot = Random.rotation;
				rot = Quaternion.Euler(new Vector3(0, 0, rot.eulerAngles.z));
				GameObject aster = (GameObject)Network.Instantiate(m_asteroidRef [Random.Range (0,m_asteroidRef.Length)], new Vector3(pos.x, pos.y, 10), rot, 0);
				aster.transform.parent = asteroids.transform;
			}
		}
		else
		{
			for(int i = 0; i < m_numAsteroids; i++)
			{
				Vector2 pos = Random.insideUnitCircle * m_range;
				Quaternion rot = Random.rotation;
				rot = Quaternion.Euler(new Vector3(0, 0, rot.eulerAngles.z));
				
				GameObject aster = (GameObject)Network.Instantiate(m_asteroidRef [Random.Range (0,m_asteroidRef.Length)], new Vector3(pos.x, pos.y, 10), rot, 0);
				aster.transform.parent = asteroids.transform;
			}
		}
	}
}

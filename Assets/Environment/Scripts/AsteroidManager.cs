using UnityEngine;
using System.Collections;

public class AsteroidManager : MonoBehaviour 
{
	[SerializeField]
	GameObject [] m_asteroidRef;
	
	[SerializeField]
	float m_range;
	[SerializeField]
	float m_ringThickness;
	
	[SerializeField]
	int m_numAsteroids;
	
	[SerializeField]
	bool m_shouldBeRingBelt = true;
	
	[SerializeField]
	bool m_shouldSpawnNow = false;
	
	static GameObject asteroids;
	
	// Use this for initialization
	void Start () 
	{
		if(asteroids == null)
			asteroids = new GameObject("Asteroids");
	}
	
	// Update is called once per frame
	void Update () 
	{
		if(m_shouldSpawnNow)
		{
			SpawnAsteroids();
			m_shouldSpawnNow = false;
		}
	}
	
	
	
	public void SpawnAsteroids()
	{
		if(m_shouldBeRingBelt)
		{
			float theta = (Mathf.PI * 2) / m_numAsteroids;
			for(int i = 0; i < m_numAsteroids; i++)
			{
				//Random way
				/*Vector2 pos = Random.insideUnitCircle.normalized;
				pos *= (m_range + Random.Range(-m_ringThickness, m_ringThickness));
				Quaternion rot = Random.rotation;
				rot = Quaternion.Euler(new Vector3(0, 0, rot.eulerAngles.z));
				//Network.Instantiate(m_asteroidRef, new Vector3(pos.x, pos.y, 10), rot, 0);
				Instantiate(m_asteroidRef, new Vector3(pos.x, pos.y, 10), rot);*/
				
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
				//aster.GetComponent<AsteroidScript>().InitialAsteroidSpawnRange();
			}
		}
	}
}

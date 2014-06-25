using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemySpawnPointScript : MonoBehaviour
{
	List<WaveInfo> m_wavesToBeSpawned;
	
	public GameObject m_CapitalShip;
	
	[SerializeField]
	float m_timeBetweenReleases;
	float m_timeSinceLastRelease;
	
	public bool m_shouldStartSpawning = false;
	public bool m_spawnerHasFinished = false;
	
	public bool m_shouldPause = false;

    float modifier = 1.0f;
	
	// Use this for initialization
	void Start()
	{
		m_wavesToBeSpawned = new List<WaveInfo>();
	}
	
	// Update is called once per frame
	void Update()
	{
		if (Network.isServer && m_shouldStartSpawning && !m_shouldPause)
		{
			m_timeSinceLastRelease += Time.deltaTime;
			if (m_timeSinceLastRelease >= m_timeBetweenReleases && m_wavesToBeSpawned.Count != 0)
				ReleaseEnemy();
		}
	}
	
	void ReleaseEnemy()
	{
		m_timeSinceLastRelease = 0;
		
		if (0 == m_wavesToBeSpawned.Count)
		{
			return;
		}
		
		GameObject groupObject = new GameObject("EnemyGroup");
		groupObject.tag = "EnemyGroup";
		
		EnemyGroup spawnedGroup = groupObject.AddComponent<EnemyGroup>();
		foreach (WaveEnemyType enemyType in m_wavesToBeSpawned[0].m_enemiesOnWave)
		{
			for(int i = 0; i < enemyType.m_numEnemy; ++i)
			{
				GameObject enemy = (GameObject)Network.Instantiate(enemyType.m_enemyRef, this.transform.position + new Vector3(Random.Range(-5.0f, 5.0f), Random.Range(-5.0f, 5.0f), 0), this.transform.rotation, 0);
				EnemyScript script = enemy.GetComponent<EnemyScript>();
				spawnedGroup.AddEnemyToGroup(script);
                HealthScript health = enemy.GetComponent<HealthScript>();
                health.SetModifier(modifier);
			}
		}
		
		GameObject CShip = GameObject.FindGameObjectWithTag("Capital");
		
		AIOrder<EnemyGroup> defaultOrder = new AIOrder<EnemyGroup> { Orderee = spawnedGroup, ObjectOfInterest = CShip };
		defaultOrder.AttachCondition(delegate(EnemyGroup group, GameObject objectOfInterest, Vector3 pointOfInterest)
		                             {
			return objectOfInterest == null;
		});
		defaultOrder.AttachAction(delegate(EnemyGroup group, GameObject objectOfInterest, Vector3 pointOfInterest)
		                          {
			group.CancelCurrentOrder();
			group.OrderAttack(objectOfInterest);
		});
		spawnedGroup.AddDefaultOrder(defaultOrder);
		
		m_wavesToBeSpawned.RemoveAt(0);
		
		m_spawnerHasFinished = true;
		
		//m_timeSinceLastRelease = 0;
		//int rand = Random.Range(0, m_wavesToBeSpawned.Count);
		//GameObject enemy = (GameObject)Network.Instantiate(m_enemiesToBeSpawned[rand], this.transform.position, this.transform.rotation, 0);
		//enemy.GetComponent<EnemyScript>().SetTarget(m_CapitalShip);
		//m_enemiesToBeSpawned.RemoveAt(rand);
		
		//if(m_enemiesToBeSpawned.Count == 0)
		//    m_spawnerHasFinished = true;
	}
	
	public void SetSpawnList(List<WaveInfo> waves, float relayTime)
	{
		m_spawnerHasFinished = false;
		m_shouldStartSpawning = false;
		//m_enemiesToBeSpawned.Clear();
		
		m_timeBetweenReleases = relayTime;
		m_wavesToBeSpawned = waves;
	}

    public void SetModifier(float modifier_)
    {
        modifier = modifier_;
    }
}

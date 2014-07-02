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
        if (Network.isServer && !m_shouldPause)
        {
            // m_timeSinceLastRelease += Time.deltaTime;
            //if (m_timeSinceLastRelease >= m_timeBetweenReleases && m_wavesToBeSpawned.Count != 0)
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


        foreach (WaveInfo info in m_wavesToBeSpawned)
        {

            /// set up group
            GameObject groupObject = new GameObject("EnemyGroup");
            groupObject.tag = "EnemyGroup";

            EnemyGroup spawnedGroup = groupObject.AddComponent<EnemyGroup>();
            foreach (WaveEnemyType enemyType in info.m_enemiesOnWave)
            {
                for (int i = 0; i < enemyType.m_numEnemy; ++i)
                {
                    GameObject enemy = (GameObject)Network.Instantiate(enemyType.m_enemyRef, this.transform.position + new Vector3(Random.Range(-5.0f, 5.0f), Random.Range(-5.0f, 5.0f), 0), this.transform.rotation, 0);
                    EnemyScript script = enemy.GetComponent<EnemyScript>();
                    spawnedGroup.AddEnemyToGroup(script);
                    HealthScript health = enemy.GetComponent<HealthScript>();
                    health.SetModifier(modifier);
                }
            }


            /// find the closest target to this spawn point out of all potential targets with the tag specified
            GameObject[] defaultTargets = GameObject.FindGameObjectsWithTag(info.GetDefaultOrderTargetTag());
            float closest = 0;
            GameObject closestTarget = null;
            foreach (GameObject potentialTarget in defaultTargets)
            {
                if (closestTarget == null || Vector2.SqrMagnitude(transform.position - closestTarget.transform.position) < closest)
                {
                    closestTarget = potentialTarget;
                    closest = Vector2.SqrMagnitude(transform.position - closestTarget.transform.position);
                }
            }


            /// attach order targetting closest target to group
            AIOrder<EnemyGroup> defaultOrder = new AIOrder<EnemyGroup> { Orderee = spawnedGroup, ObjectOfInterest = closestTarget };
            defaultOrder.AttachCondition(
                delegate(EnemyGroup group, GameObject objectOfInterest, Vector3 pointOfInterest)
                {
                    return objectOfInterest == null;
                });
            defaultOrder.AttachAction(
                delegate(EnemyGroup group, GameObject objectOfInterest, Vector3 pointOfInterest)
                {
                    group.CancelCurrentOrder();
                    group.OrderAttack(objectOfInterest);
                });
            spawnedGroup.AddDefaultOrder(defaultOrder);

        }

        m_wavesToBeSpawned.Clear();

    }

    //public void SetSpawnList(List<WaveInfo> waves, float relayTime)
    //{
    //    m_spawnerHasFinished = false;
    //    m_shouldStartSpawning = false;

    //    m_timeBetweenReleases = relayTime;

    //    m_wavesToBeSpawned.Clear();
    //    m_wavesToBeSpawned.AddRange(waves);
    //}

    public void AddToSpawnList(List<WaveInfo> waves_, float relayTime_)
    {

        m_timeBetweenReleases = relayTime_;

        m_wavesToBeSpawned.AddRange(waves_);
    }

    public void SetModifier(float modifier_)
    {
        modifier = modifier_;
    }
}

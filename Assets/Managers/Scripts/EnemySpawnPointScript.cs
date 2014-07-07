using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(MeshRenderer))]
public class EnemySpawnPointScript : MonoBehaviour
{
    List<WaveInfo> m_wavesToBeSpawned;

    [SerializeField]
    float activeTime;
    float m_timeSinceLastRelease;

    MeshRenderer renderer;
    [SerializeField]
    bool active = false;

    [SerializeField]
    Material idleMat;

    [SerializeField]
    Material activeMat;

    public bool m_shouldPause = false;

    float modifier = 1.0f;

    // Use this for initialization
    void Start()
    {
        m_wavesToBeSpawned = new List<WaveInfo>();
        renderer = GetComponent<MeshRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Network.isServer && !m_shouldPause)
        {
            if (active)
            {
                m_timeSinceLastRelease += Time.deltaTime;
                if (m_timeSinceLastRelease >= activeTime && active)
                    ReleaseEnemy();
            }

        }
    }

    void ReleaseEnemy()
    {
        m_timeSinceLastRelease = 0;

        if (0 == m_wavesToBeSpawned.Count)
        {
            return;
        }

        Activate(false);

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

            if(closestTarget == null)
            {
                Debug.LogError("Default target is null");
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

    public void AddToSpawnList(List<WaveInfo> waves_)
    {
        m_wavesToBeSpawned.AddRange(waves_);

        Activate(true);
    }

    void Activate(bool flag_)
    {
        renderer.material = (active = flag_) == true ? activeMat : idleMat;
    }

    public void SetModifier(float modifier_)
    {
        modifier = modifier_;
    }
}

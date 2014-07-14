using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SpawnLocation
{
    public GameObject spawner;
}

[RequireComponent(typeof(MeshRenderer))]
public class EnemySpawnPointScript : MonoBehaviour
{
    List<WaveInfo> m_wavesToBeSpawned;

    

    [SerializeField]
    float activeTime;
    float m_timeSinceLastRelease;

    MeshRenderer meshRenderer;
    [SerializeField]
    bool spawnPointActive = false;

    [SerializeField]
    GameObject spawnEffect;

    List<SpawnLocation> nextWave;

    public bool m_shouldPause = false;

    float modifier = 1.0f;

    // Use this for initialization
    void Start()
    {
        nextWave = new List<SpawnLocation>();
        m_wavesToBeSpawned = new List<WaveInfo>();
        meshRenderer = GetComponent<MeshRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        if (spawnPointActive)
        {
            m_timeSinceLastRelease += Time.deltaTime;
            if (m_timeSinceLastRelease >= activeTime && spawnPointActive)
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

        

        if (Network.isServer)
        {
            int currentSpawnLocationIndex = 0;

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
                        GameObject enemy = (GameObject)Network.Instantiate(enemyType.m_enemyRef, nextWave[currentSpawnLocationIndex++].spawner.transform.position, this.transform.rotation, 0);

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

                if (closestTarget == null)
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

        Activate(false);

    }

    public void AddToSpawnList(List<WaveInfo> waves_)
    {
        m_wavesToBeSpawned.AddRange(waves_);

        GenerateSpawnLocations();

        Activate(true);
    }

    void Activate(bool flag_)
    {
        spawnPointActive = flag_;

        //setup with if statement incase any other code is needed
        if (spawnPointActive)
        {
        }
        else
        {
            // spawn point is idle
            networkView.RPC("PropagateDestroySpawnLocations", RPCMode.All);
        }
    }

    public void GenerateSpawnLocations()
    {
        if (nextWave.Count > 0)
            networkView.RPC("PropagateDestroySpawnLocations", RPCMode.All);

        foreach (WaveInfo info in m_wavesToBeSpawned)
        {
            foreach (WaveEnemyType enemyType in info.m_enemiesOnWave)
            {
                for (int i = 0; i < enemyType.m_numEnemy; ++i)
                {
                    networkView.RPC("PropagateNewSpawnLocation", RPCMode.All, this.transform.position + new Vector3(Random.Range(-5.0f, 5.0f), Random.Range(-5.0f, 5.0f), 0));
                }
            }
        }
    }

    [RPC]
    public void PropagateDestroySpawnLocations()
    {
        if (nextWave.Count == 0)
            return;

        foreach (SpawnLocation spawn in nextWave)
        {
            Destroy(spawn.spawner, 5);
        }

        nextWave.Clear();
    }

    [RPC]
    private void PropagateNewSpawnLocation(Vector3 location_)
    {
        nextWave.Add(new SpawnLocation { spawner = (GameObject)Instantiate(spawnEffect, location_, Quaternion.identity) });
    }

    public int GetEnemyCountOfNextWave()
    {
        int returnee = 0;

        foreach (WaveInfo info in m_wavesToBeSpawned)
        {
            foreach (WaveEnemyType enemyType in info.m_enemiesOnWave)
            {

                returnee += enemyType.m_numEnemy;

            }
        }

        return returnee;
    }

    public void SetModifier(float modifier_)
    {
        modifier = modifier_;
    }
}

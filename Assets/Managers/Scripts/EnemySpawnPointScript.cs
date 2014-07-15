using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SpawnLocation
{
    public float timeUntilStart, currentTime = 0;
    public Vector3 location;
}

public class GroupedWaveInfo
{

    public EnemyGroup group;
    public GameObject[] wave;
    public int currentPositionInWave = 0;

    public GameObject NextEnemy()
    {
        return wave[currentPositionInWave++];
    }

    public bool Finished()
    {
        return currentPositionInWave >= wave.Length;
    }

}

[RequireComponent(typeof(MeshRenderer))]
public class EnemySpawnPointScript : MonoBehaviour
{
    List<GroupedWaveInfo> m_wavesToBeSpawned;

    [SerializeField]
    float activeTime;
    float m_timeSinceLastRelease;

    MeshRenderer meshRenderer;
    [SerializeField]
    bool spawnPointActive = false;

    [SerializeField]
    GameObject spawnEffect;
    [SerializeField]
    float maxTimeBetweenFirstAndLastSpawn;

    List<SpawnLocation> enemiesWaitingToSpawn, enemiesBeingSpawned;

    [SerializeField]
    GameObject wormholePrefab;
    [SerializeField]
    float timeBeforeScalingWormhole, timeTakenToScaleWormhole;
    float currentScalingTime = 0, currentT;
    Transform wormhole;
    [SerializeField]
    Vector3 wormholeOriginalScale;
    bool wormholeActive = false;


    public bool m_shouldPause = false;

    float modifier = 1.0f;

    // Use this for initialization
    void Start()
    {
        wormhole = ((GameObject)Instantiate(wormholePrefab, this.transform.position, this.transform.rotation)).transform;
        wormhole.localScale = Vector3.zero;

        enemiesWaitingToSpawn = new List<SpawnLocation>();
        enemiesBeingSpawned = new List<SpawnLocation>();
        m_wavesToBeSpawned = new List<GroupedWaveInfo>();
        meshRenderer = GetComponent<MeshRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        if (spawnPointActive)
        {
            if (Network.isServer)
                CheckSpawning();

            rigidbody.AddTorque(new Vector3(0, 0, 200 * Time.deltaTime));
        }

        CheckScalingWormhole();
    }

    void CheckScalingWormhole()
    {
        if (spawnPointActive && !wormholeActive)
        {
            currentScalingTime += Time.deltaTime;
            if (currentScalingTime >= timeBeforeScalingWormhole)
            {
                networkView.RPC("SetWormholeSize", RPCMode.All, (currentScalingTime - timeBeforeScalingWormhole) / timeTakenToScaleWormhole);

                if (currentScalingTime >= timeTakenToScaleWormhole + timeBeforeScalingWormhole)
                {
                    currentScalingTime = 0;
                    wormholeActive = true;
                }
            }
        }
        else if (!spawnPointActive && wormholeActive)
        {

            currentScalingTime += Time.deltaTime;
            if (currentScalingTime >= timeBeforeScalingWormhole)
            {
                networkView.RPC("SetWormholeSize", RPCMode.All, 1 - ((currentScalingTime - timeBeforeScalingWormhole) / timeTakenToScaleWormhole));

                if (currentScalingTime >= timeTakenToScaleWormhole + timeBeforeScalingWormhole)
                {
                    currentScalingTime = 0;
                    wormholeActive = false;
                }
            }
        }

    }

    void CheckSpawning()
    {


        float delta = Time.deltaTime;
        for (int i = enemiesWaitingToSpawn.Count - 1; i >= 0; --i)
        {
            SpawnLocation spawn = enemiesWaitingToSpawn[i];
            spawn.currentTime += delta;

            if (spawn.currentTime >= spawn.timeUntilStart)
            {

                networkView.RPC("PropagateNewSpawnEffect", RPCMode.All, enemiesBeingSpawned[i].location, activeTime);

                // move the SpawnLocation from waitingToSpawn to beingSpawned
                enemiesBeingSpawned.Add(spawn);
                enemiesWaitingToSpawn.RemoveAt(i);

                // reset the values so that they are correct
                spawn.currentTime = 0;
                spawn.timeUntilStart = activeTime;
            }
        }



        for (int i = enemiesBeingSpawned.Count - 1; i >= 0; --i)
        {
            SpawnLocation spawn = enemiesBeingSpawned[i];
            spawn.currentTime += delta;

            if (spawn.currentTime >= spawn.timeUntilStart)
            {

                if (Network.isServer)
                {
                    GameObject enemy = (GameObject)Network.Instantiate(m_wavesToBeSpawned[0].NextEnemy(), spawn.location, this.transform.rotation, 0);

                    EnemyScript script = enemy.GetComponent<EnemyScript>();
                    m_wavesToBeSpawned[0].group.AddEnemyToGroup(script);

                    HealthScript health = enemy.GetComponent<HealthScript>();
                    health.SetModifier(modifier);

                    if (m_wavesToBeSpawned[0].Finished())
                    {
                        m_wavesToBeSpawned.RemoveAt(0);
                    }
                }

                enemiesBeingSpawned.RemoveAt(i);

            }

        }

        if (enemiesWaitingToSpawn.Count == 0 && enemiesBeingSpawned.Count == 0)
        {
            Activate(false);
        }
    }

    public void AddToSpawnList(List<WaveInfo> waves_)
    {
        foreach (WaveInfo info in waves_)
        {
            m_wavesToBeSpawned.Add(CreateGroupedWaveInfo(info));
        }

        GenerateSpawnLocations();

        Activate(true);
    }


    /// <summary>
    /// Used for tying a group to a wave
    /// </summary>
    /// <param name="info_"></param>
    /// <returns></returns>
    GroupedWaveInfo CreateGroupedWaveInfo(WaveInfo info_)
    {
        GameObject groupObject = new GameObject("EnemyGroup");
        groupObject.tag = "EnemyGroup";

        EnemyGroup spawnedGroup = groupObject.AddComponent<EnemyGroup>();

        GameObject[] defaultTargets = GameObject.FindGameObjectsWithTag(info_.GetDefaultOrderTargetTag());
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


        // set all the objects into an array
        List<GameObject> enemies = new List<GameObject>();

        foreach (WaveEnemyType enemyType in info_.m_enemiesOnWave)
        {
            for (int i = 0; i < enemyType.m_numEnemy; ++i)
            {

                enemies.Add(enemyType.m_enemyRef);

            }
        }

        return new GroupedWaveInfo { group = spawnedGroup, wave = enemies.ToArray() };
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
            networkView.RPC("PropagateDestroySpawnLocations", RPCMode.All, 0.0f);
        }
    }


    public void GenerateSpawnLocations()
    {
        if (Network.isServer)
        {
            foreach (GroupedWaveInfo info in m_wavesToBeSpawned)
            {
                for (int i = 0; i < info.wave.Length; ++i)
                {
                    /*networkView.RPC("PropagateNewSpawnLocation", RPCMode.All, Random.Range(0, maxTimeBetweenFirstAndLastSpawn) + timeBeforeScalingWormhole + timeTakenToScaleWormhole,
                                    this.transform.position + new Vector3(Random.Range(-5.0f, 5.0f), Random.Range(-5.0f, 5.0f), 0));*/

                    NewSpawnLocation(Random.Range(0, maxTimeBetweenFirstAndLastSpawn) + timeBeforeScalingWormhole + timeTakenToScaleWormhole,
                                    this.transform.position + new Vector3(Random.Range(-5.0f, 5.0f), Random.Range(-5.0f, 5.0f), 0));
                }
            }
        }
    }

    //[RPC]
    //public void PropagateDestroySpawnLocations(float time_)
    //{
    //    if (enemiesWaitingToSpawn.Count > 0 && enemiesBeingSpawned.Count > 0)
    //        return;

    //    foreach (SpawnLocation spawn in enemiesBeingSpawned)
    //    {
    //        Destroy(spawn.spawner, time_);
    //    }

    //    enemiesWaitingToSpawn.Clear();
    //    enemiesBeingSpawned.Clear();
    //}

    private void NewSpawnLocation(float timeUntilStart_, Vector3 location_)
    {
        enemiesWaitingToSpawn.Add(new SpawnLocation { timeUntilStart = timeUntilStart_, location = location_ });
    }

    [RPC]
    private void PropagateNewSpawnEffect(float timeTillDestroy_, Vector3 location_)
    {
        GameObject spawnedEffect = (GameObject)Instantiate(spawnEffect, location_, Quaternion.identity);
        Destroy(spawnedEffect, timeTillDestroy_);
    }

    public void SetModifier(float modifier_)
    {
        modifier = modifier_;
    }

    [RPC]
    void SetWormholeSize(float t_)
    {
        Vector3 newScale = Vector3.Lerp(Vector3.zero, wormholeOriginalScale, t_);
        newScale.z = 1;
        wormhole.localScale = newScale;
    }
}

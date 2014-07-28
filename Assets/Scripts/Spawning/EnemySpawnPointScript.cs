using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SpawnLocation
{
    public float timeUntilStart, currentTime = 0, scale;
    public Vector3 location;
    public GameObject prefab;
    public EnemyGroup parentGroup;
}

public class GroupedWaveInfo
{

    public EnemyGroup group;
    public GameObject[] wave;
    public int currentPositionInWave = 0;

    public GameObject NextEnemy()
    {
        currentPositionInWave++;
        return wave[currentPositionInWave - 1];
    }

    public bool Finished()
    {

        return currentPositionInWave >= wave.Length;
    }

}

[RequireComponent(typeof(MeshRenderer))]
public class EnemySpawnPointScript : MonoBehaviour
{
    
    [SerializeField] float m_activeTime;
    
    [SerializeField] bool m_spawnPointActive = false;

    [SerializeField] GameObject m_spawnEffect, m_finalEffect;
    [SerializeField] float m_maxTimeBetweenFirstAndLastSpawn;

    [SerializeField] GameObject m_wormholePrefab;
    [SerializeField] float m_timeBeforeScalingWormhole;
    [SerializeField] float m_timeTakenToScaleWormhole;
    
    [SerializeField] Vector3 m_wormholeOriginalScale;
    


    List<GroupedWaveInfo> m_wavesToBeSpawned;

    float m_timeSinceLastRelease;

    MeshRenderer m_meshRenderer;

    List<SpawnLocation> m_enemiesWaitingToSpawn, m_enemiesBeingSpawned;

    float m_currentScalingTime = 0, currentT;

    bool m_shouldPause = false;

    float m_modifier = 1.0f;



    Transform m_wormhole;

    bool m_wormholeActive = false;



    #region getset

    public bool GetShouldPause()
    {
        return m_shouldPause;
    }

    public void SetShouldPause(bool flag_)
    {
        m_shouldPause = flag_;
    }

    public void SetModifier(float modifier_)
    {
        m_modifier = modifier_;
    }


    #endregion getset

    void Start()
    {
        m_wormhole = ((GameObject)Instantiate(m_wormholePrefab, this.transform.position, this.transform.rotation)).transform;
        m_wormhole.localScale = Vector3.zero;

        m_enemiesWaitingToSpawn = new List<SpawnLocation>();
        m_enemiesBeingSpawned = new List<SpawnLocation>();
        m_wavesToBeSpawned = new List<GroupedWaveInfo>();
        m_meshRenderer = GetComponent<MeshRenderer>();
    }

    void Update()
    {
        if (m_spawnPointActive)
        {
            rigidbody.AddTorque(new Vector3(0, 0, 200 * Time.deltaTime));
        }

        if (Network.isServer)
        {
            if (m_spawnPointActive)
            {
                CheckSpawning();
            }

            CheckScalingWormhole();
        }

    }

    // TODO: change wormhole scaling to client side with triggers only being sent across network for opening wormholes

    void CheckScalingWormhole()
    {
        if (m_spawnPointActive && !m_wormholeActive)
        {
            m_currentScalingTime += Time.deltaTime;
            if (m_currentScalingTime >= m_timeBeforeScalingWormhole)
            {
                networkView.RPC("SetWormholeSize", RPCMode.All, (m_currentScalingTime - m_timeBeforeScalingWormhole) / m_timeTakenToScaleWormhole);

                if (m_currentScalingTime >= m_timeTakenToScaleWormhole + m_timeBeforeScalingWormhole)
                {
                    m_currentScalingTime = 0;
                    m_wormholeActive = true;
                }
            }
        }
        else if (!m_spawnPointActive && m_wormholeActive)
        {

            m_currentScalingTime += Time.deltaTime;
            if (m_currentScalingTime >= m_timeBeforeScalingWormhole)
            {
                networkView.RPC("SetWormholeSize", RPCMode.All, 1 - ((m_currentScalingTime - m_timeBeforeScalingWormhole) / m_timeTakenToScaleWormhole));

                if (m_currentScalingTime >= m_timeTakenToScaleWormhole + m_timeBeforeScalingWormhole)
                {
                    m_currentScalingTime = 0;
                    m_wormholeActive = false;
                }
            }
        }

    }

    void CheckSpawning()
    {
        EnemyGroup group = null;

        float delta = Time.deltaTime;
        for (int i = m_enemiesWaitingToSpawn.Count - 1; i >= 0; --i)
        {
            SpawnLocation spawn = m_enemiesWaitingToSpawn[i];
            spawn.currentTime += delta;

            if (spawn.currentTime >= spawn.timeUntilStart)
            {
                networkView.RPC("PropagateNewSpawnEffect", RPCMode.All, m_activeTime, m_enemiesWaitingToSpawn[i].location, m_enemiesWaitingToSpawn[i].scale);

                // move the SpawnLocation from waitingToSpawn to beingSpawned
                m_enemiesBeingSpawned.Add(spawn);
                m_enemiesWaitingToSpawn.RemoveAt(i);

                // reset the values so that they are correct
                spawn.currentTime = 0;
                spawn.timeUntilStart = m_activeTime;
            }
        }



        for (int i = m_enemiesBeingSpawned.Count - 1; i >= 0; --i)
        {
            SpawnLocation spawn = m_enemiesBeingSpawned[i];
            spawn.currentTime += delta;

            if (spawn.currentTime >= spawn.timeUntilStart)
            {
                GameObject enemy = (GameObject)Network.Instantiate(spawn.prefab, spawn.location, this.transform.rotation, 0);

                EnemyScript script = enemy.GetComponent<EnemyScript>();
                spawn.parentGroup.AddEnemyToGroup(script);
                group = spawn.parentGroup;

                HealthScript health = enemy.GetComponent<HealthScript>();
                health.SetModifier(m_modifier);

                m_enemiesBeingSpawned.RemoveAt(i);

            }

        }

        if (m_enemiesWaitingToSpawn.Count == 0 && m_enemiesBeingSpawned.Count == 0)
        {
            group.CancelAllOrders();
            Activate(false);
        }
    }

    public void AddToSpawnList(List<WaveInfo> waves_)
    {
        if (Network.isServer)
        {
            foreach (WaveInfo info in waves_)
            {
                m_wavesToBeSpawned.Add(CreateGroupedWaveInfo(info));
            }

            GenerateSpawnLocations();

            Activate(true);
        }
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

        groupObject.transform.position = transform.position;

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
        m_spawnPointActive = flag_;

        if(Network.isServer)
        {
            networkView.RPC("SetActive", RPCMode.Others, m_spawnPointActive ? 1 : 0);
        }
    }

    /// <summary>
    /// Takes all current wavesToBeSpawned and starts the spawning sequence
    /// </summary>
    void GenerateSpawnLocations()
    {
        if (Network.isServer)
        {
            m_enemiesWaitingToSpawn.Clear();

            foreach (GroupedWaveInfo info in m_wavesToBeSpawned)
            {
                for (int i = 0; i < info.wave.Length; ++i)
                {
                    /*networkView.RPC("PropagateNewSpawnLocation", RPCMode.All, Random.Range(0, maxTimeBetweenFirstAndLastSpawn) + timeBeforeScalingWormhole + timeTakenToScaleWormhole,
                                    this.transform.position + new Vector3(Random.Range(-5.0f, 5.0f), Random.Range(-5.0f, 5.0f), 0));*/

                    Ship shipComponent = info.wave[i].GetComponent<Ship>();

                    NewSpawnLocation(Random.Range(0, m_maxTimeBetweenFirstAndLastSpawn) + m_timeBeforeScalingWormhole + m_timeTakenToScaleWormhole,
                                    this.transform.position + new Vector3(Random.Range(-5.0f, 5.0f), Random.Range(-5.0f, 5.0f), 0),
                                    shipComponent.GetMaxSize(),
                                    info.wave[i],
                                    info.group);
                }
            }

            m_wavesToBeSpawned.Clear();
        }
    }

    /// <summary>
    /// Creates a new spawn for a specified prefab
    /// </summary>
    /// <param name="timeUntilStart_">Time until the spawning starts</param>
    /// <param name="location_"></param>
    /// <param name="scale_"></param>
    /// <param name="prefab_"></param>
    /// <param name="parentGroup_"></param>
    void NewSpawnLocation(float timeUntilStart_, Vector3 location_, float scale_, GameObject prefab_, EnemyGroup parentGroup_)
    {
        m_enemiesWaitingToSpawn.Add(new SpawnLocation { timeUntilStart = timeUntilStart_, location = location_, scale = scale_, prefab = prefab_, parentGroup = parentGroup_});
    }

    [RPC] private void PropagateNewSpawnEffect(float timeTillDestroy_, Vector3 location_, float scale_)
    {
        GameObject spawnedEffect = (GameObject)Instantiate(m_spawnEffect, location_, Quaternion.identity);
        spawnedEffect.transform.localScale = new Vector3(scale_, scale_, 1);

        RunFinalSpawnEffect script = spawnedEffect.GetComponent<RunFinalSpawnEffect>();
        script.Run(timeTillDestroy_ - 1.3f);
        Destroy(spawnedEffect, timeTillDestroy_ - 0.2f);
    }

    

    [RPC] void SetWormholeSize(float t_)
    {
        Vector3 newScale = Vector3.Lerp(Vector3.zero, m_wormholeOriginalScale, t_);
        newScale.z = 1;
        m_wormhole.localScale = newScale;
    }

    [RPC] void SetActive(int i_)
    {
        m_spawnPointActive = i_ == 1;
    }
}

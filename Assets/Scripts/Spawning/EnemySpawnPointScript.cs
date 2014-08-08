using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SpawnLocation
{
    public float timeUntilStart, currentTime = 0, scale;
    public GameObject shipObject;
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
    


    float m_timeSinceLastRelease;

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
        }

        CheckScalingWormhole();

    }

    // TODO: change wormhole scaling to client side with triggers only being sent across network for opening wormholes

    void CheckScalingWormhole()
    {
        if (m_spawnPointActive && !m_wormholeActive)
        {
            m_currentScalingTime += Time.deltaTime;
            if (m_currentScalingTime >= m_timeBeforeScalingWormhole)
            {
                SetWormholeSize((m_currentScalingTime - m_timeBeforeScalingWormhole) / m_timeTakenToScaleWormhole);

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
                SetWormholeSize(1 - ((m_currentScalingTime - m_timeBeforeScalingWormhole) / m_timeTakenToScaleWormhole));

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
        float delta = Time.deltaTime;
        for (int i = m_enemiesWaitingToSpawn.Count - 1; i >= 0; --i)
        {
            SpawnLocation spawn = m_enemiesWaitingToSpawn[i];
            spawn.currentTime += delta;

            if (spawn.currentTime >= spawn.timeUntilStart)
            {
                networkView.RPC("PropagateNewSpawnEffect", RPCMode.All, m_activeTime, m_enemiesWaitingToSpawn[i].shipObject.transform.position, m_enemiesWaitingToSpawn[i].scale);

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
                ShipEnemy script = spawn.shipObject.GetComponent<ShipEnemy>();
                spawn.leader.AddChild(script.GetAINode());

                HealthScript health = enemy.GetComponent<HealthScript>();
                health.SetModifier(m_modifier);

                m_enemiesBeingSpawned.RemoveAt(i);

            }

        }

        if (m_enemiesWaitingToSpawn.Count == 0 && m_enemiesBeingSpawned.Count == 0)
        {
            Activate(false);
        }
    }

    public void AddToSpawnList(List<WaveInfo> waves_)
    {
        if (Network.isServer)
        {
            foreach (WaveInfo info in waves_)
            {
                GenerateSpawnLocations(info);
                //m_wavesToBeSpawned.Add(CreateGroupedWaveInfo(info));
            }

            Activate(true);
        }
    }


    /// <summary>
    /// Used for tying a group to a wave
    /// </summary>
    /// <param name="info_"></param>
    /// <returns></returns>
    //GroupedWaveInfo CreateGroupedWaveInfo(WaveInfo info_)
    //{
    //    // set all the objects into an array
    //    List<GameObject> enemies = new List<GameObject>();

    //    foreach (WaveEnemyType enemyType in info_.m_enemiesOnWave)
    //    {
    //        for (int i = 0; i < enemyType.m_numEnemy; ++i)
    //        {

    //            enemies.Add(enemyType.m_enemyRef);
                
    //        }
    //    }

    //    GameObject leaderAIObject = new GameObject("LeaderAI");
    //    AISpawnLeader leaderAI = leaderAIObject.AddComponent<AISpawnLeader>();
    //    leaderAI.GetTargetTags().AddRange(info_.GetDefaultOrderTargetTags());

    //    return new GroupedWaveInfo { leader = leaderAI, wave = enemies.ToArray() };
    //}

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
    void GenerateSpawnLocations(WaveInfo info_)
    {
        foreach (WaveEnemyType enemyType in info_.m_enemiesOnWave)
        {
            for (int i = 0; i < enemyType.m_numEnemy; ++i)
            {
                Ship shipComponent = enemyType.m_enemyRef.GetComponent<Ship>();

                Vector3 spawnLocation = this.transform.position + new Vector3(Random.Range(-5.0f, 5.0f), Random.Range(-5.0f, 5.0f), 0);
                GameObject ship = (GameObject)Network.Instantiate(enemyType.m_enemyRef, spawnLocation, Quaternion.identity, 0);
                ship.SetActive(false);

                NewSpawnLocation(Random.Range(0, m_maxTimeBetweenFirstAndLastSpawn) + m_timeBeforeScalingWormhole + m_timeTakenToScaleWormhole,
                                shipComponent.GetMaxSize(),
                                ship);
            }
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
    void NewSpawnLocation(float timeUntilStart_, float scale_, GameObject shipObject_)
    {
        m_enemiesWaitingToSpawn.Add(new SpawnLocation { timeUntilStart = timeUntilStart_, scale = scale_, shipObject = shipObject_ });
    }

    [RPC] private void PropagateNewSpawnEffect(float timeTillDestroy_, Vector3 location_, float scale_)
    {
        GameObject spawnedEffect = (GameObject)Instantiate(m_spawnEffect, location_, Quaternion.identity);
        spawnedEffect.transform.localScale = new Vector3(scale_, scale_, 1);

        RunFinalSpawnEffect script = spawnedEffect.GetComponent<RunFinalSpawnEffect>();
        script.Run(timeTillDestroy_ - 1.3f);
        Destroy(spawnedEffect, timeTillDestroy_ - 0.2f);
    }

    

    void SetWormholeSize(float t_)
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

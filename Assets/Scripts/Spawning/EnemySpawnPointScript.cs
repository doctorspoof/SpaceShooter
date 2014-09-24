using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SpawnLocation
{
    public float timeUntilStart, currentTime = 0, scale;
    public Vector3 position;
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
                spawn.shipObject.SetActive(true);

                HealthScript health = spawn.shipObject.GetComponent<HealthScript>();
                health.SetModifier(m_modifier);

                m_enemiesBeingSpawned.RemoveAt(i);

            }

        }

        if (m_enemiesWaitingToSpawn.Count == 0 && m_enemiesBeingSpawned.Count == 0)
        {
            Activate(false);
        }
    }

    public void AddToSpawnList(WaveInfo wave_)
    {
        if (Network.isServer)
        {
            // currently does nothing with any gameobjects that dont have a ship component, eg. AISpawnLeader

            List<GameObject> objectsInstantiated = wave_.Instantiate();

            List<Ship> ships = new List<Ship>();
            objectsInstantiated.ForEach(
                 x =>
                 {
                     Ship ship = null;
                     if ((ship = x.GetComponent<Ship>()) != null)
                     {
                         ships.Add(ship);
                     }
                 }
                 );

            List<SpawnLocation> locations = GenerateSpawnLocations(ships.Count);

            BindSpawns(ships, locations);

            Activate(true);

        }
    }

    void Activate(bool flag_)
    {
        m_spawnPointActive = flag_;

        if(Network.isServer)
        {
            networkView.RPC("SetActive", RPCMode.Others, m_spawnPointActive ? 1 : 0);
        }
    }

    void BindSpawns(List<Ship> ships_, List<SpawnLocation> spawns_)
    {
        for(int i = 0; i < spawns_.Count; ++i)
        {
            spawns_[i].scale = ships_[i].GetMaxSize();
            spawns_[i].shipObject = ships_[i].gameObject;
            spawns_[i].shipObject.transform.position = spawns_[i].position;
            spawns_[i].shipObject.SetActive(false);
        }

        m_enemiesWaitingToSpawn.AddRange(spawns_);
    }

    /// <summary>
    /// Takes all current wavesToBeSpawned and starts the spawning sequence
    /// </summary>
    List<SpawnLocation> GenerateSpawnLocations(int count_)
    {
        List<SpawnLocation> locations = new List<SpawnLocation>();

        for(int i = 0; i < count_; ++i)
        {
            Vector3 spawnLocation = this.transform.position + new Vector3(Random.Range(-5.0f, 5.0f), Random.Range(-5.0f, 5.0f), -1);

            float timeUntilStart = Random.Range(0, m_maxTimeBetweenFirstAndLastSpawn) + m_timeBeforeScalingWormhole + m_timeTakenToScaleWormhole;

            SpawnLocation newLocation = new SpawnLocation{ timeUntilStart = timeUntilStart, position = spawnLocation };
            bool added = false;

            for (int j = 0; j < locations.Count; ++j)
            {
                if(locations[j].timeUntilStart > newLocation.timeUntilStart)
                {
                    locations.Insert(j, newLocation);
                    added = true;
                    break;
                }
            }

            if(!added)
            {
                locations.Add(newLocation);
            }
        }

        return locations;
    }

    [RPC] void PropagateNewSpawnEffect(float timeTillDestroy_, Vector3 location_, float scale_)
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

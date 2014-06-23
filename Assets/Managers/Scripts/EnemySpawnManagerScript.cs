using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class WaveEnemyType
{
    public GameObject m_enemyRef;
    public int m_numEnemy;
}

[System.Serializable]
public class WaveInfo
{
    public WaveEnemyType[] m_enemiesOnWave;

    public int GetTotalSize()
    {
        int size = 0;
        foreach (WaveEnemyType type in m_enemiesOnWave)
        {
            size += type.m_numEnemy;
        }
        return size;
    }
    public GameObject[] GetRawWave()
    {
        int size = 0;
        foreach (WaveEnemyType type in m_enemiesOnWave)
        {
            size += type.m_numEnemy;
        }

        GameObject[] output = new GameObject[size];
        int place = 0;
        for (int i = 0; i < m_enemiesOnWave.Length; i++)
        {
            for (int j = 0; j < m_enemiesOnWave[i].m_numEnemy; j++)
            {
                output[place] = m_enemiesOnWave[i].m_enemyRef;
                place++;
            }
        }

        return output;
    }
}

public class EnemySpawnManagerScript : MonoBehaviour
{
    [SerializeField]
    WaveInfo[] m_waveInfos;

    [SerializeField]
    GameObject[] test;

    [SerializeField]
    GameObject[] m_allSpawnPoints;

    [SerializeField]
    int currentWave = 0;

    [SerializeField]
    GameObject m_capitalShip;

    bool shouldPause = false;

    public void RecieveInGameCapitalShip(GameObject ship)
    {
        m_allSpawnPoints = GameObject.FindGameObjectsWithTag("SpawnPoint");
        m_capitalShip = ship;
        foreach (GameObject spawner in m_allSpawnPoints)
        {
            spawner.GetComponent<EnemySpawnPointScript>().m_CapitalShip = ship;
        }
    }

    // Use this for initialization
    void Start()
    {
        //test = m_waveInfos[0].GetRawWave();
        //test = GetAllRawWavesTogether();
        InitSpawnPoints();
    }

    public void BeginSpawning()
    {
        shouldStart = true;
        if (currentWave == 0)
        {
            //Spawners haven't been initialised, try again:
            InitSpawnPoints();
        }
    }

    // Update is called once per frame
    public bool shouldStart = false;
    public bool allDone = true;
    void Update()
    {
        if (Network.isServer && m_allSpawnPoints.Length != 0 && !shouldPause && shouldStart)
        {
            allDone = true;

            for (int i = 0; i < m_allSpawnPoints.Length; i++)
            {
                if (!m_allSpawnPoints[i].GetComponent<EnemySpawnPointScript>().m_spawnerHasFinished)
                {
                    allDone = false;
                    break;
                }
            }

            if (allDone)
            {
                GameObject.FindGameObjectWithTag("GameController").GetComponent<GameStateController>().m_shouldCheckForFinished = true;
                SendNextWaveToPoints();
            }
        }
    }

    public void InitSpawnPoints()
    {
        if (Network.isServer)
        {
            //Debug.Log ("Initialising spawn points.");
            m_allSpawnPoints = GameObject.FindGameObjectsWithTag("SpawnPoint");

            //Debug.Log ("WaveInfo is of size: " + m_waveInfos.Length);
            if (m_waveInfos.Length != 0)
            {
                //Debug.Log ("Sending first wave to spawners");
                //Send the first wave over
                foreach (GameObject spawner in m_allSpawnPoints)
                {
                    EnemySpawnPointScript spawnPointScript = spawner.GetComponent<EnemySpawnPointScript>();

                    List<WaveInfo> waveToBePassed = new List<WaveInfo>();
                    waveToBePassed.Add(m_waveInfos[currentWave]);

                    spawnPointScript.SetSpawnList(waveToBePassed, 25.0f);
                }
                currentWave++;
            }
        }
    }

    void SendNextWaveToPoints()
    {
        if (currentWave < m_waveInfos.Length)
        {

            foreach (GameObject spawner in m_allSpawnPoints)
            {
                EnemySpawnPointScript spawnPointScript = spawner.GetComponent<EnemySpawnPointScript>();

                List<WaveInfo> waveToBePassed = new List<WaveInfo>();
                waveToBePassed.Add(m_waveInfos[currentWave]);

                spawnPointScript.SetSpawnList(waveToBePassed, 25.0f);
            }
            GameObject.FindGameObjectWithTag("GameController").GetComponent<GameStateController>().AlertAllClientsNextWaveReady();
            currentWave++;
        }
    }

    public void TellAllSpawnersBegin()
    {
        foreach (GameObject spawner in m_allSpawnPoints)
        {
            //Debug.Log("Telling spawner: " + spawner.name + " to begin spawning wave #" + currentWave);
            spawner.GetComponent<EnemySpawnPointScript>().m_shouldStartSpawning = true;
        }
    }

    GameObject[] GetAllRawWavesTogether()
    {
        int size = 0;
        foreach (WaveInfo info in m_waveInfos)
        {
            size += info.GetTotalSize();
        }

        GameObject[] output = new GameObject[size];
        int endMark = 0;
        foreach (WaveInfo info in m_waveInfos)
        {
            info.GetRawWave().CopyTo(output, endMark);
            endMark += info.GetTotalSize();
        }

        return output;
    }

    public void PauseSpawners(bool pauseStatus)
    {
        shouldPause = pauseStatus;
        foreach (GameObject spawner in m_allSpawnPoints)
        {
            spawner.GetComponent<EnemySpawnPointScript>().m_shouldPause = pauseStatus;
        }
    }
}

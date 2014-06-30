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
    WaveInfo[] smallMultipleWaves;

    [SerializeField]
    WaveInfo[] singleLargeWave;

    [SerializeField]
    GameObject[] test;

    [SerializeField]
    GameObject[] m_allSpawnPoints;

    //[SerializeField]
    //int currentWave = 0;

    [SerializeField]
    GameObject m_capitalShip;

    [SerializeField]
    float healthModifierIncrement;
    [SerializeField]
    float currentHealthModifier = 1.0f;
    [SerializeField]
    float timeCountInMinutes;

    [SerializeField]
    float multiplierAtTimeRequired;

    [SerializeField]
    int secondsBetweenWaves = 40;

    bool shouldPause = false, hasBegan = false;

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

        float increaseInPercentRequired = multiplierAtTimeRequired - 1;
        healthModifierIncrement = Mathf.Exp((Mathf.Log(increaseInPercentRequired) / (timeCountInMinutes * 60)));

        InitSpawnPoints();


    }

    public void BeginSpawning()
    {
        shouldStart = true;
        //if (currentWave == 0)
        //{
        //Spawners haven't been initialised, try again:
        InitSpawnPoints();
        //}
    }

    // Update is called once per frame
    public bool shouldStart = false;
    public bool allDone = true;

    [SerializeField]
    int lastTime = 0;
    void Update()
    {
        //-60 seconds so updates once per minute
        if (Time.time - lastTime >= 1 && hasBegan)
        {
            lastTime = (int)Time.time;
            currentHealthModifier *= healthModifierIncrement;
            foreach (GameObject spawn in m_allSpawnPoints)
            {
                EnemySpawnPointScript spawnPoint = spawn.GetComponent<EnemySpawnPointScript>();
                spawnPoint.SetModifier(currentHealthModifier);
            }
        }

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
            if (smallMultipleWaves.Length != 0)
            {
                //Debug.Log ("Sending first wave to spawners");
                //Send the first wave over
                foreach (GameObject spawner in m_allSpawnPoints)
                {
                    EnemySpawnPointScript spawnPointScript = spawner.GetComponent<EnemySpawnPointScript>();

                    List<WaveInfo> waveToBePassed = new List<WaveInfo>();
                    waveToBePassed.Add(smallMultipleWaves[Random.Range(0, smallMultipleWaves.Length)]);

                    spawnPointScript.SetSpawnList(waveToBePassed, secondsBetweenWaves);
                }
                //currentWave++;
            }
            waveCount++;
        }
    }

    int waveCount = 0;

    void SendNextWaveToPoints()
    {
        waveCount++;
        //if (currentWave < m_waveInfos.Length)
        //{
        if (waveCount % 5 == 0 && waveCount > 0)
        {
            EnemySpawnPointScript spawnPoint = m_allSpawnPoints[Random.Range(0, m_allSpawnPoints.Length)].GetComponent<EnemySpawnPointScript>();

            List<WaveInfo> waveToBePassed = new List<WaveInfo>();
			waveToBePassed.Add(singleLargeWave[Random.Range(0, singleLargeWave.Length)]);

            spawnPoint.SetSpawnList(waveToBePassed, secondsBetweenWaves);
        }
        else
        {
            foreach (GameObject spawner in m_allSpawnPoints)
            {
                EnemySpawnPointScript spawnPointScript = spawner.GetComponent<EnemySpawnPointScript>();

                List<WaveInfo> waveToBePassed = new List<WaveInfo>();
                waveToBePassed.Add(smallMultipleWaves[Random.Range(0, smallMultipleWaves.Length)]);

                spawnPointScript.SetSpawnList(waveToBePassed, secondsBetweenWaves);
            }
        }


        GameObject.FindGameObjectWithTag("GameController").GetComponent<GameStateController>().AlertAllClientsNextWaveReady();
        //currentWave++;
        //}

        
    }

    public void TellAllSpawnersBegin()
    {
        hasBegan = true;
        foreach (GameObject spawner in m_allSpawnPoints)
        {
            //Debug.Log("Telling spawner: " + spawner.name + " to begin spawning wave #" + currentWave);
            spawner.GetComponent<EnemySpawnPointScript>().m_shouldStartSpawning = true;
        }
    }

    //GameObject[] GetAllRawWavesTogether()
    //{
    //    int size = 0;
    //    foreach (WaveInfo info in smallMultipleWaves)
    //    {
    //        size += info.GetTotalSize();
    //    }

    //    GameObject[] output = new GameObject[size];
    //    int endMark = 0;
    //    foreach (WaveInfo info in smallMultipleWaves)
    //    {
    //        info.GetRawWave().CopyTo(output, endMark);
    //        endMark += info.GetTotalSize();
    //    }

    //    return output;
    //}

    public void PauseSpawners(bool pauseStatus)
    {
        shouldPause = pauseStatus;
        foreach (GameObject spawner in m_allSpawnPoints)
        {
            spawner.GetComponent<EnemySpawnPointScript>().m_shouldPause = pauseStatus;
        }
    }
}

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class WaveEnemyType
{
    public GameObject m_enemyRef;
    public int m_numEnemy;

    public WaveEnemyType Clone()
    {
        return new WaveEnemyType { m_enemyRef = this.m_enemyRef, m_numEnemy = this.m_numEnemy };
    }
}

[System.Serializable]
public class WaveInfo
{
    [SerializeField]
    public string defaultOrderTargetTag = "Capital";
    public string GetDefaultOrderTargetTag()
    {
        return defaultOrderTargetTag;
    }

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

    public WaveInfo Clone()
    {
        WaveInfo returnee = new WaveInfo();
        returnee.defaultOrderTargetTag = this.defaultOrderTargetTag;

        returnee.m_enemiesOnWave = new WaveEnemyType[this.m_enemiesOnWave.Length];
        for (int i = 0; i < this.m_enemiesOnWave.Length; ++i)
        {
            returnee.m_enemiesOnWave[i] = this.m_enemiesOnWave[i].Clone();
        }

        return returnee;
    }
}

public class EnemySpawnManagerScript : MonoBehaviour
{
    [SerializeField]
    WaveInfo[] smallMultipleWaves;

    [SerializeField]
    WaveInfo[] singleLargeWave;

    [SerializeField]
    WaveInfo[] specialWaves;

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
    int lastTimeModifier = 0, lastTimeSpawn = 0;
    void Update()
    {
        //-60 seconds so updates once per minute
        if (Time.time - lastTimeModifier >= 1 && hasBegan)
        {
            lastTimeModifier = (int)Time.time;
            currentHealthModifier *= healthModifierIncrement;
            foreach (GameObject spawn in m_allSpawnPoints)
            {
                EnemySpawnPointScript spawnPoint = spawn.GetComponent<EnemySpawnPointScript>();
                spawnPoint.SetModifier(currentHealthModifier);
            }
        }

        if (Network.isServer && m_allSpawnPoints.Length != 0 && /*!shouldPause && shouldStart &&*/ (Time.time - lastTimeSpawn - secondsBetweenWaves) >= 1)// && hasBegan)
        {
            lastTimeSpawn = (int)Time.time;
            SendNextWaveToPoints();
        }
    }

    public void InitSpawnPoints()
    {
        if (Network.isServer)
        {
            //Debug.Log ("Initialising spawn points.");
            m_allSpawnPoints = GameObject.FindGameObjectsWithTag("SpawnPoint");

            //Debug.Log ("WaveInfo is of size: " + m_waveInfos.Length);
            //if (smallMultipleWaves.Length != 0)
            //{
            //    //Debug.Log ("Sending first wave to spawners");
            //    //Send the first wave over
            //    foreach (GameObject spawner in m_allSpawnPoints)
            //    {
            //        EnemySpawnPointScript spawnPointScript = spawner.GetComponent<EnemySpawnPointScript>();

            //        List<WaveInfo> waveToBePassed = new List<WaveInfo>();
            //        waveToBePassed.Add(smallMultipleWaves[Random.Range(0, smallMultipleWaves.Length)]);

            //        spawnPointScript.SetSpawnList(waveToBePassed, secondsBetweenWaves);
            //    }
            //    //currentWave++;
            //}
            //waveCount++;

            SendNextWaveToPoints();
        }
    }

    int waveCount = 0;

    void SendNextWaveToPoints()
    {
        waveCount++;
        //if (currentWave < m_waveInfos.Length)
        //{

        List<GameObject> spawnersToBeSpawnedAt = null;
        //spawnersToBeSpawnedAt = GetRandomSpawnPoints(1, 1);
        // decide if this is a large or small wave
        if ((waveCount % 5 == 0 && waveCount > 0))
        {
            List<WaveInfo> waveToBePassed = new List<WaveInfo>();
            waveToBePassed.Add(singleLargeWave[Random.Range(0, singleLargeWave.Length)]);

            spawnersToBeSpawnedAt = GetRandomSpawnPoints(1, 1);

            EnemySpawnPointScript spawnPoint = spawnersToBeSpawnedAt[0].GetComponent<EnemySpawnPointScript>();
            spawnPoint.AddToSpawnList(waveToBePassed);

        }
        else
        {

            int random = Random.Range(0, smallMultipleWaves.Length);
            WaveInfo newWave = smallMultipleWaves[random].Clone();

            //waveToBePassed.Add(newWave);

            spawnersToBeSpawnedAt = GetRandomSpawnPoints(2, m_allSpawnPoints.Length);

            float[] ratios = new float[newWave.m_enemiesOnWave.Length];
            for (int i = 0; i < newWave.m_enemiesOnWave.Length; ++i)
            {
                ratios[i] = newWave.m_enemiesOnWave[i].m_numEnemy / (float)spawnersToBeSpawnedAt.Count;
            }

            for (int a = 0; a < spawnersToBeSpawnedAt.Count; ++a)
            {
                WaveInfo adjustedWave = newWave.Clone();

                for (int i = 0; i < newWave.m_enemiesOnWave.Length; ++i)
                {
                    int shipsCount = newWave.m_enemiesOnWave[i].m_numEnemy;
                    //                int shipsPerGroup = Mathf.Min(Mathf.CeilToInt(shipsCount / (float)spawnersToBeSpawnedAt.Count),
                    //shipsCount - a * Mathf.CeilToInt(shipsCount / (float)spawnersToBeSpawnedAt.Count));

                    int shipsPerGroup = Mathf.Min(Mathf.CeilToInt(ratios[i]), Mathf.CeilToInt(shipsCount / (float)(spawnersToBeSpawnedAt.Count - a)));

                    adjustedWave.m_enemiesOnWave[i].m_numEnemy = shipsPerGroup;
                    newWave.m_enemiesOnWave[i].m_numEnemy -= shipsPerGroup;

                }

                List<WaveInfo> waveToBePassed = new List<WaveInfo>();
                waveToBePassed.Add(adjustedWave);

                //Debug.Log("a = " + a + " length = " + spawnersToBeSpawnedAt.Count);
                EnemySpawnPointScript spawnPoint = spawnersToBeSpawnedAt[a].GetComponent<EnemySpawnPointScript>();
                spawnPoint.AddToSpawnList(waveToBePassed);
            }


        }

        // will a special wave spawn in addition?
        if (waveCount % 4 == 0)
        {
            List<WaveInfo> waveToBePassed = new List<WaveInfo>();
            waveToBePassed.Add(specialWaves[Random.Range(0, specialWaves.Length)]);

            foreach (GameObject obj in spawnersToBeSpawnedAt)
            {
                EnemySpawnPointScript spawnPoint = obj.GetComponent<EnemySpawnPointScript>();
                spawnPoint.AddToSpawnList(waveToBePassed);
            }
        }


        GameObject.FindGameObjectWithTag("GameController").GetComponent<GameStateController>().AlertAllClientsNextWaveReady();
        //currentWave++;
        //}

    }

    public List<GameObject> GetRandomSpawnPoints(int min_, int max_)
    {
        List<GameObject> tempListForSpawning = new List<GameObject>();
        tempListForSpawning.AddRange((GameObject[])m_allSpawnPoints.Clone());

        List<GameObject> pointsToBeSpawnedAt = new List<GameObject>();

        int amountOfPointsToSpawnAt = Random.Range(min_, max_ + 1);

        for (int i = 0; i < amountOfPointsToSpawnAt; ++i)
        {
            int index = Random.Range(0, tempListForSpawning.Count);
            pointsToBeSpawnedAt.Add(tempListForSpawning[index]);
            tempListForSpawning.RemoveAt(index);
        }

        return pointsToBeSpawnedAt;
    }

    //public void TellAllSpawnersBegin()
    //{
    //    hasBegan = true;
    //    foreach (GameObject spawner in m_allSpawnPoints)
    //    {
    //        //Debug.Log("Telling spawner: " + spawner.name + " to begin spawning wave #" + currentWave);
    //        spawner.GetComponent<EnemySpawnPointScript>().m_shouldStartSpawning = true;
    //    }
    //}

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

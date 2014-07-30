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
    [SerializeField] string defaultOrderTargetTag = "Capital";


    public WaveEnemyType[] m_enemiesOnWave;

    #region getset

    public string GetDefaultOrderTargetTag()
    {
        return defaultOrderTargetTag;
    }

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

    #endregion

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
    [SerializeField] WaveInfo[] m_smallMultipleWaves;

    [SerializeField] WaveInfo[] m_singleLargeWave;

    [SerializeField] WaveInfo[] m_specialWaves;

    [SerializeField] GameObject[] m_allSpawnPoints;

    [SerializeField] float m_healthModifierIncrement;
    [SerializeField] float m_currentHealthModifier = 1.0f;
    [SerializeField] float m_timeCountInMinutes;

    [SerializeField] float m_multiplierAtTimeRequired;

    [SerializeField] int m_secondsBetweenWaves = 40;

    [SerializeField] int m_lastTimeModifier = 0;
    [SerializeField] int m_lastTimeSpawn = 0;



    bool m_shouldPause = false, m_hasBegan = false;

    bool m_shouldStart = false;
    bool m_allDone = true;



    int waveCount = 0;

    #region getset

    public bool GetShouldStart()
    {
        return m_shouldStart;
    }

    public void SetShouldStart(bool flag_)
    {
        m_shouldStart = flag_;
    }

    public bool GetAllDone()
    {
        return m_allDone;
    }

    public void SetAllDone(bool flag_)
    {
        m_allDone = flag_;
    }

    #endregion getset

    void Start()
    {
        //float increaseInPercentRequired = multiplierAtTimeRequired - 1;
        //healthModifierIncrement = Mathf.Exp((Mathf.Log(increaseInPercentRequired) / (timeCountInMinutes * 60)));

        float increaseInPercentRequired = m_multiplierAtTimeRequired - 1;
        m_healthModifierIncrement = increaseInPercentRequired / (m_timeCountInMinutes * 60);

        InitSpawnPoints();
    }
    
    void Update()
    {
        //-60 seconds so updates once per minute
        if (Time.timeSinceLevelLoad - m_lastTimeModifier >= 1 && m_hasBegan)
        {
            m_lastTimeModifier = (int)Time.timeSinceLevelLoad;
            m_currentHealthModifier += m_healthModifierIncrement;
            foreach (GameObject spawn in m_allSpawnPoints)
            {
                EnemySpawnPointScript spawnPoint = spawn.GetComponent<EnemySpawnPointScript>();
                spawnPoint.SetModifier(m_currentHealthModifier);
            }
        }

        if (Network.isServer && m_allSpawnPoints.Length != 0 && !m_shouldPause && /*shouldStart &&*/ (Time.timeSinceLevelLoad - m_lastTimeSpawn - m_secondsBetweenWaves) >= 1)// && hasBegan)
        {
            m_lastTimeSpawn = (int)Time.timeSinceLevelLoad;
            SendNextWaveToPoints();
        }
    }

    public void BeginSpawning()
    {
        m_shouldStart = true;
        m_hasBegan = true;
    }

    public void InitSpawnPoints()
    {
        m_allSpawnPoints = GameObject.FindGameObjectsWithTag("SpawnPoint");
    }

    void SendNextWaveToPoints()
    {
        waveCount++;

        List<GameObject> spawnersToBeSpawnedAt = null;
        
        // decide if this is a large or small wave
        if ((waveCount % 5 == 0 && waveCount > 0))
        {
            List<WaveInfo> waveToBePassed = new List<WaveInfo>();
            waveToBePassed.Add(m_singleLargeWave[Random.Range(0, m_singleLargeWave.Length)]);

            spawnersToBeSpawnedAt = GetRandomSpawnPoints(1, 1);

            EnemySpawnPointScript spawnPoint = spawnersToBeSpawnedAt[0].GetComponent<EnemySpawnPointScript>();
            spawnPoint.AddToSpawnList(waveToBePassed);

        }
        else
        {

            int random = Random.Range(0, m_smallMultipleWaves.Length);
            WaveInfo newWave = m_smallMultipleWaves[random].Clone();
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
            waveToBePassed.Add(m_specialWaves[Random.Range(0, m_specialWaves.Length)]);

            foreach (GameObject obj in spawnersToBeSpawnedAt)
            {
                EnemySpawnPointScript spawnPoint = obj.GetComponent<EnemySpawnPointScript>();
                spawnPoint.AddToSpawnList(waveToBePassed);
            }
        }

        GameObject.FindGameObjectWithTag("GameController").GetComponent<GameStateController>().AlertAllClientsNextWaveReady();

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

    public void PauseSpawners(bool pauseStatus)
    {
        m_shouldPause = pauseStatus;
        foreach (GameObject spawner in m_allSpawnPoints)
        {
            spawner.GetComponent<EnemySpawnPointScript>().SetShouldPause(pauseStatus);
        }
    }
}

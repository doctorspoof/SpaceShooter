﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class WaveEnemyType
{
    public GameObject m_enemyRef;
    public int m_numEnemy;
}

public class WaveInfo
{

    /// <summary>
    /// Needs to instantiate from the children upwards. Can only instantiate the ship aft
    /// </summary>

    public GameObject leader;

    #region getset

    public List<GameObject> Instantiate()
    {
        List<GameObject> objectsInstatiated = new List<GameObject>();

        GameObject newObject = leader.GetComponent<Cloneable>().Clone(new Vector3(), Quaternion.identity);

        SetHierarchy(GetIEntity(leader).GetAINode(), GetIEntity(newObject).GetAINode(), ref objectsInstatiated);

        return objectsInstatiated;
    }

    int depth = 0;

    void SetHierarchy(AINode prefabParent_, AINode newParent_, ref List<GameObject> objectsInstantiated_)
    {
        depth++;
        // Setup the parent so that it works in the scene
        GameObject parent = ((Component)newParent_.GetEntity()).gameObject;

        // add it to object list so that we can manipulate where it starts/spawns
        objectsInstantiated_.Add(parent);

        // traverse through the prefabs children and instantiate them aswell
        foreach (AINode child in prefabParent_.GetChildren())
        {
            // instantiate the child
            Component childComp = (Component)child.GetEntity();
            GameObject newObj = childComp.GetComponent<Cloneable>().Clone(new Vector3(), Quaternion.identity);

            newObj.GetComponent<Ship>().depth = depth;

            // get the new instatiated objects entity
            IEntity newChild = GetIEntity(newObj);

            // set the hierarchy
            newParent_.AddChild(newChild.GetAINode(), false);

            // recurse
            SetHierarchy(child, newChild.GetAINode(), ref objectsInstantiated_);
        }
        depth--;
    }

    public static IEntity GetIEntity(GameObject object_)
    {
        foreach (Component comp in object_.GetComponents<Component>())
        {
            if (comp is IEntity)
            {
                return comp as IEntity;
            }
        }

        return null;
    }

    #endregion
}

public class EnemySpawnManagerScript : MonoBehaviour
{
    [SerializeField] GameObject[] m_allSpawnPoints;

    //[SerializeField] float m_healthModifierIncrement;
    //[SerializeField] float m_currentHealthModifier = 1.0f;
    [SerializeField] float m_timeCountInMinutes;

    //[SerializeField] float m_multiplierAtTimeRequired;

    [SerializeField] int m_secondsBetweenWaves = 20;

    [SerializeField] int m_lastTimeModifier = 0;
    [SerializeField] int m_lastTimeSpawn = 0;



    Dictionary<string, WaveInfo> m_waves;
    GameObject m_wavesContainer;

    bool m_shouldPause = false, m_hasBegan = false;

    bool m_shouldStart = false;
    bool m_allDone = true;
    
    float   m_playerBehindTimesFactor = 0f;

    

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
    
    public void SetUpTimeCatchup(float newCatchup)
    {
        m_playerBehindTimesFactor = newCatchup;
    }   

    #endregion getset

    void Awake()
    {
        m_waves = new Dictionary<string, WaveInfo>();

        m_wavesContainer = new GameObject("WavesPrefabsContainer");
        m_wavesContainer.SetActive(false);
    }

    void Start()
    {
        //float increaseInPercentRequired = multiplierAtTimeRequired - 1;
        //healthModifierIncrement = Mathf.Exp((Mathf.Log(increaseInPercentRequired) / (timeCountInMinutes * 60)));

        //float increaseInPercentRequired = m_multiplierAtTimeRequired - 1;
        //m_healthModifierIncrement = increaseInPercentRequired / (m_timeCountInMinutes * 60);

        InitSpawnPoints();

        LoadHardCodedWaves();
    }
    
    void Update()
    {
        //-60 seconds so updates once per minute
        /*if (Time.timeSinceLevelLoad - m_lastTimeModifier >= 1 && m_hasBegan)
        {
            m_lastTimeModifier = (int)Time.timeSinceLevelLoad;
            m_currentHealthModifier += m_healthModifierIncrement;
            foreach (GameObject spawn in m_allSpawnPoints)
            {
                EnemySpawnPointScript spawnPoint = spawn.GetComponent<EnemySpawnPointScript>();
                spawnPoint.SetModifier(m_currentHealthModifier);
            }
        }*/

        if (Network.isServer && m_allSpawnPoints.Length != 0 && !m_shouldPause && /*shouldStart &&*/ (Time.timeSinceLevelLoad - m_lastTimeSpawn - m_secondsBetweenWaves) >= 1 && m_hasBegan)
        {
            m_lastTimeSpawn = (int)Time.timeSinceLevelLoad;
            SendNextWaveToPoints();
        }
    }

    void LoadWaves(string path_)
    {
        // Get files only with .hs extension
        string[] filePaths = Directory.GetFiles(@path_, "*.hs");
        foreach(string file in filePaths)
        {
            Debug.Log("Loading = " + file);
            Scripter script = new Scripter();
            script.LoadFromFile(file);

            AddFunctionsToWaveScripter(script);

            script.Run();
        }
    }

    bool SetWave(string name_, Component leader_)
    {
        m_waves.Add(name_, new WaveInfo { leader = leader_.gameObject });

        GameObject container = new GameObject("Wave" + name_);
        container.SetActive(false);
        leader_.transform.parent = container.transform;

        container.transform.parent = m_wavesContainer.transform;

        ((IEntity)leader_).GetAINode().Recurse(
            x => ((Component)x).transform.parent = container.transform
            );

        return true;
    }

    IEntity CreateShip(string prefabName_, IEntity parent_)
    {
        GameObject prefab = GetPrefab(prefabName_);

        GameObject gObject = (GameObject)Instantiate(prefab);
        gObject.SetActive(false);

        IEntity newShip = GetIEntity(gObject);

        if(parent_ != null)
        {
            parent_.GetAINode().AddChild(newShip.GetAINode(), false);
        }

        return newShip;
    }

    IEntity GetIEntity(GameObject object_)
    {
        foreach(Component comp in object_.GetComponents<Component>())
        {
            if(comp is IEntity)
            {
                return (IEntity)comp;
            }
        }
        return null;
    }

    GameObject GetPrefab(string shipName_)
    {
        //Debug.Log ("Attempting to access prefab for enemy " + shipName_);
        return UnityEngine.Resources.Load<GameObject>("Prefabs/Enemies/" + shipName_);
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
        List<GameObject> spawnersToBeSpawnedAt = null;
        
        spawnersToBeSpawnedAt = GetRandomSpawnPoints(1, 1);

        List<WaveInfo> types = new List<WaveInfo>(m_waves.Values);

        EnemySpawnPointScript spawnPoint = spawnersToBeSpawnedAt[0].GetComponent<EnemySpawnPointScript>();
        spawnPoint.AddToSpawnList(types[Random.Range(0, types.Count)]);
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

    void LoadHardCodedWaves()
    {
        int numPlayers = GameStateController.Instance().GetConnectedPlayers().Count;
        string[] scripts = new string[3];
                   
        // Light enemies = (3*P)groups of 5
        System.Text.StringBuilder builderTest1 = new System.Text.StringBuilder();
        builderTest1.Append("var waveLeader = CreateShip(\"AISpawnLeader\", null); waveLeader.AddTargetTag(\"Capital\"); waveLeader.AddTargetTag(\"Player\");");
            //scripts[0] =    "var waveLeader = CreateShip(\"AISpawnLeader\", null);" +
            //"waveLeader.AddTargetTag(\"Capital\");" +
            //"waveLeader.AddTargetTag(\"Player\");";
                        
        //Every minute the player is behind the times, add another group of enemies per player in-game
        int timeExtraFactor = (int)(m_playerBehindTimesFactor / 60);
        
        for(int i =0; i < (numPlayers * 3) + (timeExtraFactor * numPlayers); i++)
        {
            builderTest1.Append("var subLeader" + i + " = CreateShip(\"EnemyFast\", waveLeader);")
                        .Append("var child" + i + "1 = CreateShip(\"EnemyFast\", subLeader" + i + ");")
                        .Append("var child" + i + "2 = CreateShip(\"EnemyFast\", subLeader" + i + ");")
                        .Append("var child" + i + "3 = CreateShip(\"EnemyFast\", subLeader" + i + ");")
                        .Append("var child" + i + "4 = CreateShip(\"EnemyFast\", subLeader" + i + ");");
        
            //scripts[0] += "var subLeader" + i + " = CreateShip(\"EnemyFast\", waveLeader);";
            //scripts[0] += "var child" + i + "1 = CreateShip(\"EnemyFast\", subLeader" + i + ");";
            //scripts[0] += "var child" + i + "2 = CreateShip(\"EnemyFast\", subLeader" + i + ");";
            //scripts[0] += "var child" + i + "3 = CreateShip(\"EnemyFast\", subLeader" + i + ");";
            //scripts[0] += "var child" + i + "4 = CreateShip(\"EnemyFast\", subLeader" + i + ");";
        }
        
        builderTest1.Append("SetWave(\"EnemyFast\", waveLeader);");
        scripts[0] = builderTest1.ToString();
        //scripts[0] += "SetWave(\"EnemyFast\", waveLeader);";  
        
        // Medium enemies = (3*P)groups of 3
        /*scripts[1] =    "var waveLeader = CreateShip(\"AISpawnLeader\", null);" +
                        "waveLeader.AddTargetTag(\"Capital\");" +
                        "waveLeader.AddTargetTag(\"Player\");";*/
                        
        System.Text.StringBuilder builder2 = new System.Text.StringBuilder();
        builder2.Append("var waveLeader = CreateShip(\"AISpawnLeader\", null); waveLeader.AddTargetTag(\"Capital\"); waveLeader.AddTargetTag(\"Player\");");
                
        for(int i = 0; i < (numPlayers * 3) + (timeExtraFactor * numPlayers); i++)
        {
            builder2.Append("var subLeader" + i + " = CreateShip(\"EnemyNormal\", waveLeader);");
            builder2.Append("var child" + i + "1 = CreateShip(\"EnemyNormal\", subLeader" + i + ");");
            builder2.Append("var child" + i + "2 = CreateShip(\"EnemyNormal\", subLeader" + i + ");");
        
            //scripts[1] += "var subLeader" + i + " = CreateShip(\"EnemyNormal\", waveLeader);";
            //scripts[1] += "var child" + i + "1 = CreateShip(\"EnemyNormal\", subLeader" + i + ");";
            //scripts[1] += "var child" + i + "2 = CreateShip(\"EnemyNormal\", subLeader" + i + ");";
        }
        
        builder2.Append("SetWave(\"EnemyNormal\", waveLeader);");
        scripts[1] = builder2.ToString();
        //scripts[1] += "SetWave(\"EnemyNormal\", waveLeader);";
        
        // Large enemies = (P)groups of 3
        /*scripts[2] =    "var waveLeader = CreateShip(\"AISpawnLeader\", null);" +
                        "waveLeader.AddTargetTag(\"Capital\");" +
                        "waveLeader.AddTargetTag(\"Player\");";*/
        System.Text.StringBuilder builder3 = new System.Text.StringBuilder();
        builder3.Append("var waveLeader = CreateShip(\"AISpawnLeader\", null); waveLeader.AddTargetTag(\"Capital\"); waveLeader.AddTargetTag(\"Player\");");
                        
        for(int i = 0; i < (numPlayers) + (timeExtraFactor * numPlayers); i++)
        {
            builder3.Append("var subLeader" + i + " = CreateShip(\"EnemyHeavy\", waveLeader);");
            builder3.Append("var child" + i + "1 = CreateShip(\"EnemyHeavy\", subLeader" + i + ");");
            builder3.Append("var child" + i + "2 = CreateShip(\"EnemyHeavy\", subLeader" + i + ");");
        
            //scripts[2] += "var subLeader" + i + " = CreateShip(\"EnemyHeavy\", waveLeader);";
            //scripts[2] += "var child" + i + "1 = CreateShip(\"EnemyHeavy\", subLeader" + i + ");";
            //scripts[2] += "var child" + i + "2 = CreateShip(\"EnemyHeavy\", subLeader" + i + ");";
        }
        
        builder3.Append("SetWave(\"EnemyHeavy\", waveLeader);");
        scripts[2] = builder3.ToString();
        //scripts[2] += "SetWave(\"EnemyHeavy\", waveLeader);";
        
        // Mixed
        /*scripts[3] =    "var waveLeader = CreateShip(\"AISpawnLeader\", null);" +
                        "waveLeader.AddTargetTag(\"Capital\");" +
                        "waveLeader.AddTargetTag(\"Player\");";
                        
        for(int i = 0; i < numPlayers; i++)
        {
            scripts[3] += "var subLeader" + i + " = CreateShip(\"EnemyHeavy\", waveLeader);";
            scripts[3] += "var child" + i + "1 = CreateShip(\"EnemyNormal\", subLeader);";
            scripts[3] += "var child" + i + "2 = CreateShip(\"EnemyNormal\", subLeader);";
            scripts[3] += "var child" + i + "3 = CreateShip(\"EnemyFast\", subLeader" + i + ");";
            scripts[3] += "var child" + i + "4 = CreateShip(\"EnemyFast\", subLeader" + i + ");";
            scripts[3] += "var child" + i + "5 = CreateShip(\"EnemyFast\", subLeader" + i + ");";
        }
        
        scripts[3] += "SetWave(\"EnemyMixed\", waveLeader);";*/
        
        
                /*string[] scripts = {
                               // Light enemy wave 3x5

                                // leader of wave
                                "var waveLeader = CreateShip(\"AISpawnLeader\", null);" +
                                "waveLeader.AddTargetTag(\"Capital\");" +
                                "waveLeader.AddTargetTag(\"Player\");" +

                                // group 1
                                "var subLeader1 = CreateShip(\"EnemyFast\", waveLeader);" +
                                "var child11 = CreateShip(\"EnemyFast\", subLeader1);" +
                                "var child12 = CreateShip(\"EnemyFast\", subLeader1);" +
                                "var child13 = CreateShip(\"EnemyFast\", subLeader1);" +
                                "var child14 = CreateShip(\"EnemyFast\", subLeader1);" +

                                // group 2
                                "var subLeader2 = CreateShip(\"EnemyFast\", waveLeader);" +
                                "var child21 = CreateShip(\"EnemyFast\", subLeader2);" +
                                "var child22 = CreateShip(\"EnemyFast\", subLeader2);" +
                                "var child23 = CreateShip(\"EnemyFast\", subLeader2);" +
                                "var child24 = CreateShip(\"EnemyFast\", subLeader2);" +

                                // group 3
                                "var subLeader3 = CreateShip(\"EnemyFast\", waveLeader);" +
                                "var child31 = CreateShip(\"EnemyFast\", subLeader3);" +
                                "var child32 = CreateShip(\"EnemyFast\", subLeader3);" +
                                "var child33 = CreateShip(\"EnemyFast\", subLeader3);" +
                                "var child34 = CreateShip(\"EnemyFast\", subLeader3);" +

                                "SetWave(\"EnemyFast\", waveLeader);",

                                //Normal enemy wave 3x3

                                // leader of wave
                                "var waveLeader = CreateShip(\"AISpawnLeader\", null);" +
                                "waveLeader.AddTargetTag(\"Capital\");" +
                                "waveLeader.AddTargetTag(\"Player\");" +

                                // group 1
                                "var subLeader1 = CreateShip(\"EnemyNormal\", waveLeader);" +
                                "var child11 = CreateShip(\"EnemyNormal\", subLeader1);" +
                                "var child12 = CreateShip(\"EnemyNormal\", subLeader1);" +

                                // group 2
                                "var subLeader2 = CreateShip(\"EnemyNormal\", waveLeader);" +
                                "var child21 = CreateShip(\"EnemyNormal\", subLeader2);" +
                                "var child22 = CreateShip(\"EnemyNormal\", subLeader2);" +

                                // group 3
                                "var subLeader3 = CreateShip(\"EnemyNormal\", waveLeader);" +
                                "var child31 = CreateShip(\"EnemyNormal\", subLeader3);" +
                                "var child32 = CreateShip(\"EnemyNormal\", subLeader3);" +

                                "SetWave(\"EnemyNormal\", waveLeader);",
                                
                                //Heavy enemy wave 1x3
                                
                                //leader
                                "var waveLeader = CreateShip(\"AISpawnLeader\", null);" +
                                "waveLeader.AddTargetTag(\"Capital\");" +
                                "waveLeader.AddTargetTag(\"Player\");" +
                                
                                // group 1
                                "var subLeader1 = CreateShip(\"EnemyHeavy\", waveLeader);" +
                                "var child11 = CreateShip(\"EnemyHeavy\", subLeader1);" +
                                "var child12 = CreateShip(\"EnemyHeavy\", subLeader1);" +
                                
                                "SetWave(\"EnemyHeavy\", waveLeader);"
                           };*/

        foreach (string haxescript in scripts)
        {
            Debug.Log("Loading = " + haxescript);
            Scripter script = new Scripter();
            script.LoadFromString(haxescript);

            AddFunctionsToWaveScripter(script);

            script.Run();
        }

        Debug.Log ("Waves were initialised.");
    }

    void AddFunctionsToWaveScripter(Scripter scripter_)
    {
        scripter_.AddFunction2<string, IEntity, IEntity>("CreateShip", CreateShip);
        scripter_.AddFunction2<string, Component, bool>("SetWave", SetWave);

        scripter_.AddAction1<string>("trace", Debug.Log);
    }
}

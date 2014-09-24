using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum PlanetType
{
    Volcanic = 1,
    Desert = 2,
    Terran = 3,
    Barren = 4,
    GasGiant = 5,
    Ice = 6
}

public class ProceduralLevelGenerator : MonoBehaviour 
{
    /* Serializable Members */
    [SerializeField]        GameObject[]        m_starPrefabs;
    [SerializeField]        GameObject[]        m_planetsPrefabs;
    [SerializeField]        GameObject[]        m_ringObjects;
    [SerializeField]        GameObject          m_asteroidManager;
    [SerializeField]        GameObject          m_shop;
    [SerializeField]        GameObject          m_shipyard;
    [SerializeField]        GameObject          m_levelBoundary;
    [SerializeField]        GameObject          m_startMarker;
    [SerializeField]        GameObject          m_endMarker;
    [SerializeField]        GameObject          m_spawnManager;
    [SerializeField]        GameObject          m_spawnPoint;
    [SerializeField]        int                 m_seed =            0;
    [SerializeField]        bool                m_tempDestroyScene = false;
    [SerializeField]        bool                m_tempGenerateScene = false;

    /* Internal Members */
    
    float furthestExtent = 0.0f;
    
    #region SpawnedObjects
    List<GameObject>    m_spawnedPlanets;
    List<float>         m_spawnPlanetsDistances;
    List<GameObject>    m_planetsUsedByShops;
    List<GameObject>    m_spawnedShops;
    #endregion

    /* Unity Functions */
    void Awake()
    {
        //Remove nulls from arrays
        m_starPrefabs = m_starPrefabs.NoNulls();
        m_planetsPrefabs = m_planetsPrefabs.NoNulls();
        m_ringObjects = m_ringObjects.NoNulls();
    }

	void Start () 
    {
        //if(m_seed == 0)
            //ResetSeed();
            
        //RequestGenerateLevel(false);
	}
    
    void Update()
    {
        if(m_tempDestroyScene)
        {
            m_tempDestroyScene = false;
            DestroyCurrentLevel();
        }
        
        if(m_tempGenerateScene)
        {
            m_tempGenerateScene = false;
            RequestGenerateLevel(true);
        }
    }
    
    /* Custom Functions */
    void GenerateLevel()
    {
        //Randomise this later
        System.Random rand = new System.Random(m_seed);
        //System.Random rand = new System.Random();
        Random.seed = m_seed;
        Debug.Log ("Beginning procedural system generation, with seed value of: " + m_seed);
        
        // Cached system variables
        float effectStarpowerCache = -1f;
        float starRelativeSizeScale = 1.0f;
        
        int orbitObjectNum = 0;
        float expectedDistance = 0.0f;
        
        #region Star
        int star = rand.Next(0, m_starPrefabs.Length);
        Debug.Log ("Spawning star #" + star + "...");
        float randomZ = Random.Range(0, 360);
        //NOTE: Change the z position if stars become 3D
        Network.Instantiate(m_starPrefabs[star], new Vector3(0f, 0f, 15.0f), Quaternion.Euler(0, 0, randomZ), 0);
        effectStarpowerCache = 0;
        GameObject[] stars = GameObject.FindGameObjectsWithTag("Star");
        float overallScale = 0;
        float totalScale = 0;
        float totalMass = 0;
        float oldestAge = 0;
        foreach(GameObject starGO in stars)
        {
            effectStarpowerCache += starGO.GetComponent<StarScript>().GetStarTemperature();
            totalScale += starGO.transform.localScale.x;
            
            totalMass += starGO.GetComponent<StarScript>().GetStarMass();
            
            float age = starGO.GetComponent<StarScript>().GetStarAge();
            if(age > oldestAge)
                oldestAge = age;
        }

        //Set caches
        overallScale = totalScale / stars.Length;
        starRelativeSizeScale = totalScale / 110.0f;
        effectStarpowerCache /= overallScale;
        
        orbitObjectNum = 4 + Mathf.RoundToInt((13.0f - oldestAge) * 0.6f);
        expectedDistance = 560.0f + (totalMass * 2.0f);
        Debug.Log ("System is expected to hold approximately " + orbitObjectNum + " natural objects, within a projected distance of " + expectedDistance + ".");
        #endregion
        
        
        #region First Pass
        
        // Generic calculations here    
        int numPlanets = Mathf.RoundToInt(orbitObjectNum * Random.Range(0.45f, 0.75f));
        int numMoons = Mathf.RoundToInt((orbitObjectNum - 4) * Random.Range (0.6f, 0.85f));
        int numBelts = Mathf.RoundToInt((orbitObjectNum - 4) * Random.Range (0.35f, 0.5f));
        int numFields = Mathf.RoundToInt((orbitObjectNum - 6) * Random.Range(0.33f, 0.425f));
        
        float planetSizeModLower = (0.28f + (4.5f / orbitObjectNum));
        float planetSizeModUpper = (0.08f + (8.75f / orbitObjectNum));
            
        
        #region Planets
        int planetRingCounter = 0;
        int spawnedMoonCounter = 0;
        
        Debug.Log("Spawning " + numPlanets + " planets...");
        for(int i = 0; i < numPlanets; i++)
        {
            int ringLevel = rand.Next(0, 2) + i;
            if(ringLevel <= planetRingCounter)
                ringLevel = planetRingCounter + 1;
            planetRingCounter = ringLevel;
            float distance = 35 * starRelativeSizeScale;
            for(int j = 0; j< ringLevel; j++)
            {
                distance += (50 * starRelativeSizeScale * Random.Range (0.9f, 1.1f));
            }
            
            //Get what type of planet this one should be
            PlanetType typeToSpawn = GetPlanetTypeFromDistanceToSunpower(distance, effectStarpowerCache);
            int planet = (int)(typeToSpawn) - 1;
            
            //Direction
            Vector2 direction = Random.insideUnitCircle;
            direction.Normalize();
            
            //Position
            Vector2 tempPos = Vector2.zero + (direction * distance);
            float scale = Random.Range(planetSizeModLower, planetSizeModUpper);
            Debug.Log ("Planet " + i + ") Spawning planet #" + planet + " with direction: " + direction + " at distance: " + distance + ", and scale: " + scale);
            
            GameObject planetObject = Network.Instantiate(m_planetsPrefabs[planet], new Vector3(tempPos.x, tempPos.y, 100.0f), Random.rotation, 0) as GameObject;
            m_spawnedPlanets.Add(planetObject);
            m_spawnPlanetsDistances.Add(distance);
            
            //Do type/size modifiers
            if(typeToSpawn == PlanetType.GasGiant)
                scale *= Random.Range(1.05f, 1.4f);
            else if(typeToSpawn == PlanetType.Volcanic || typeToSpawn == PlanetType.Ice)
                scale *= Random.Range(0.75f, 0.95f);
            
            float checkDist = Vector2.Distance(new Vector2(tempPos.x, tempPos.y), Vector2.zero);
            Debug.Log("Testing new extent of: " + checkDist);
            if(checkDist > furthestExtent)
                furthestExtent = checkDist;
            
            //Scale
            planetObject.GetComponent<OrbitingObject>().UpdateObjectScale(new Vector3(scale, scale, scale));
            
            //See if we should spawn a ring
            float baseRingChance = Random.Range(3.0f, 4.5f);
            baseRingChance += scale;
            
            if(typeToSpawn == PlanetType.GasGiant)
                baseRingChance += 1.0f;
                
            if(baseRingChance >= 6.2f)
            {
                int ringType = Random.Range(0, m_ringObjects.Length);
                Debug.Log ("Planet " + i + ") Spawning a ring of type " + ringType + "...");
                GameObject ring = Network.Instantiate(m_ringObjects[ringType], tempPos, Quaternion.identity, 0) as GameObject;
                
                ring.GetComponent<PlanetRing>().SetParentAndScale(planetObject.transform, new Vector3(50.0f, 50.0f, 50.0f));
                /*ring.transform.parent = planetObject.transform;
                ring.transform.localPosition = Vector3.zero;
                ring.transform.localScale = new Vector3(50.0f, 50.0f, 50.0f);*/
                
                //Work out a suitable rotation for the ring
                
                float randX = Random.Range(-85.0f, 85.0f);
                float randY = Random.Range(-85.0f, 85.0f);
                
                Quaternion ringRot = Quaternion.Euler(randX, randY, 0);
                ring.transform.rotation = ringRot;
            }
            
            //Moons
            if(spawnedMoonCounter < numMoons)
            {
                float moonChance = Random.Range (-1.5f, 3.0f);
                int numMoonsToSpawn = (int)(scale - moonChance);
                
                Debug.Log ("Planet " + i + ") Spawning " + numMoonsToSpawn + " moons...");
                for(int j = 0; j < numMoonsToSpawn; j++)
                {
                    ++spawnedMoonCounter;
                    
                    //Work out dist, then planet as normal
                    float moonDist = 45.0f * scale;
                    for(int ring = 0; ring < j; ring++)
                    {
                        moonDist += (15.0f * Random.Range (0.9f, 1.1f)) * scale;
                    }
                    
                    PlanetType moonTypeToSpawn = GetMoonTypeFromDistanceToSunPowerAndParentType(distance, effectStarpowerCache, typeToSpawn);
                    int moonType = (int)(moonTypeToSpawn) - 1;
                    
                    //Direction
                    Vector2 moonDirection = Random.insideUnitCircle;
                    direction.Normalize();
                    
                    //Position
                    Vector2 moonTempPos = tempPos + (moonDirection * moonDist);
                    float moonScale = Random.Range(0.2f * scale, 0.3f * scale);
                    Debug.Log ("Planet " + i + ") Moon " + j + ") Spawning moon #" + moonType + " with direction: " + direction + " at distance: " + distance + ", and scale: " + moonScale);
                    
                    GameObject moonObject = Network.Instantiate(m_planetsPrefabs[moonType], new Vector3(moonTempPos.x, moonTempPos.y, 100.0f), Random.rotation, 0) as GameObject;
                    m_spawnedPlanets.Add(moonObject);
                    
                    //moonObject.transform.parent = planetObject.transform;
                    
                    //Do type/size modifiers
                    if(moonTypeToSpawn == PlanetType.GasGiant)
                        moonScale *= Random.Range(1.05f, 1.4f);
                    else if(moonTypeToSpawn == PlanetType.Volcanic || typeToSpawn == PlanetType.Ice)
                        moonScale *= Random.Range(0.75f, 0.95f);
                    
                    float checkMoonDist = Vector2.Distance(new Vector2(moonTempPos.x, moonTempPos.y), Vector2.zero);
                    Debug.Log("Testing new extent of: " + checkMoonDist);
                    if(checkMoonDist > furthestExtent)
                        furthestExtent = checkMoonDist;
                    
                    //Scale
                    //moonObject.transform.localScale = new Vector3(moonScale, moonScale, moonScale);
                    moonObject.GetComponent<OrbitingObject>().UpdateObjectParentScale(new Vector3(moonScale, moonScale, moonScale), planetObject.transform);
                }
            }
        }
        
        //Find all shadows and make sure they're correctly aligned
        networkView.RPC ("UpdateShadowRots", RPCMode.All);
        #endregion
        
        
        #region Asteroids
        int asteroidRingCounter = 0;
        
        Debug.Log ("Spawning " + numBelts + " asteroid belts...");
        for(int i = 0; i < numBelts; i++)
        {
            GameObject asteroidMan = Network.Instantiate(m_asteroidManager, Vector3.zero, Quaternion.identity, 0) as GameObject;
            AsteroidManager asManSc = asteroidMan.GetComponent<AsteroidManager>();
            
            //Set range
            int ringLevel = rand.Next(1, 3) + i;
            if(ringLevel <= asteroidRingCounter)
                ringLevel = asteroidRingCounter + 1;
            
            float range = 60 * starRelativeSizeScale;
            for(int j = 0; j < ringLevel; j++)
            {
                range += (50 * starRelativeSizeScale * Random.Range(0.9f, 1.1f));
            }
            asteroidRingCounter = ringLevel;
            asManSc.SetRange(range);
            
            //Check extent
            if(range > furthestExtent)
                furthestExtent = range;
            
            //Thickness
            float thickness = Random.Range(3.0f, 10.0f);
            asManSc.SetThickness(thickness);
            
            //Number
            int numAster = (int)(range * 0.5f); 
            asManSc.SetAsteroidNum(numAster);
            
            //Ensure ring
            asManSc.SetIsRing(true);
            
            //Test
            Debug.Log ("Spawning asteroid belt with a range of " + range + " (ring level: " + ringLevel + ")....");
            //asManSc.ForceSpawnAsteroidsTestSP();
            asManSc.SpawnAsteroids();
        }
        
        for(int i = 0; i < numFields; i++)
        {
            GameObject asteroidMan = Network.Instantiate(m_asteroidManager, Vector3.zero, Quaternion.identity, 0) as GameObject;
            AsteroidManager asManSc = asteroidMan.GetComponent<AsteroidManager>();
            
            //Generate a size for the field
            float range = Random.Range (50.0f, 150.0f);
            
            //Find a suitable position
            Vector3 fieldPos = GetFreeFloatingRandomPosition(range);
            
            //Ensure field
            asManSc.SetIsRing(false);
            
            //Set field vars
            asManSc.SetRange(range);
            asteroidMan.transform.position = fieldPos;
            float asteroidDensity = Random.Range (1.25f, 2.0f);
            int numAster = (int)(range * asteroidDensity);
            asManSc.SetAsteroidNum(numAster);
            
            //Test
            Debug.Log ("Spawning asteroid field with a range of " + range + ", centered on position " + fieldPos + ", with " + numAster + " asteroids.");
            asManSc.SpawnAsteroids();
        }
        #endregion
        
        
        #region Shops/Shipyards
        int numShops = rand.Next(3, 7);
        
        Debug.Log ("Spawning " + numShops + " shops...");
        for(int i = 0;i < numShops; i++)
        {
            //Type
            bool isShipyard = (rand.Next(0, 4) == 3);
            bool isOrbital = (rand.Next(0, 2) == 0);
            
            if(isOrbital)
            {
                //Get a parent planet
                GameObject targetPlanet = null;
                bool isUseable = false;
                int counter = 0;
                while(!isUseable)
                {
                    counter++;
                    int spPlID = rand.Next(0, m_spawnedPlanets.Count);
                    
                    if(!m_planetsUsedByShops.Contains(m_spawnedPlanets[spPlID]))
                    {
                        targetPlanet = m_spawnedPlanets[spPlID];
                        isUseable = true;
                    }
                    else if(counter > 6)
                    {
                        isUseable = true;
                        isOrbital = false;
                    }
                }
                
                if(isOrbital)
                {
                    //Get a direction
                    Vector2 shopDir = Random.insideUnitCircle;
                    shopDir.Normalize();
                    Vector3 shopTrueDir = new Vector3(shopDir.x, shopDir.y, 15.0f);
                    
                    //Get a relative distance
                    float shopDist = targetPlanet.transform.localScale.x * 20.0f;
                    
                    //Calculate the position
                    Vector3 shopPos = targetPlanet.transform.position + (shopTrueDir * shopDist);
                    
                    string shopName = isShipyard ? "shipyard" : "shop";
                    Debug.Log ("Spawning " + shopName + " orbiting planet: " + targetPlanet.name + ".");
                    GameObject shop = null;
                    if(isShipyard)
                        shop = Network.Instantiate(m_shipyard, shopPos, Quaternion.identity, 0) as GameObject;
                    else
                        shop = Network.Instantiate(m_shop, shopPos, Quaternion.identity, 0) as GameObject;
                    
                    shop.transform.position = new Vector3(shop.transform.position.x, shop.transform.position.y, 11.0f);
                    
                    m_spawnedShops.Add (shop);
                    m_planetsUsedByShops.Add (targetPlanet);
                    
                    shop.transform.parent = targetPlanet.transform;
                    
                    //Check extent
                    float checkShopDist = Vector2.Distance(new Vector2(shopPos.x, shopPos.y), Vector2.zero);
                    if(checkShopDist > furthestExtent)
                        furthestExtent = checkShopDist;
                }
            }
            
            if(!isOrbital)
            {
                Vector3 randomPos = GetFreeFloatingRandomPosition(15.0f);
                
                string shopName = isShipyard ? "shipyard" : "shop";
                Debug.Log ("Spawning " + shopName + " at free-floating coords " + randomPos);
                GameObject shop = null;
                if(isShipyard)
                    shop = Network.Instantiate(m_shipyard, randomPos, Quaternion.identity, 0) as GameObject;
                else
                    shop = Network.Instantiate(m_shop, randomPos, Quaternion.identity, 0) as GameObject;
                m_spawnedShops.Add (shop);
            }
        }
        #endregion
        
        
        #endregion
        
        
        #region SecondPass
        // In this pass, we will sanity check what we have so far, and make sure everything we have makes at least theoretical sense
        
        // First, make sure nothing is inside the sun
        /*Collider[] objsInsideSun = Physics.OverlapSphere(new Vector3(0, 0, 50.0f), totalScale);
        Rigidbody[] uniqueObjsInSun = objsInsideSun.GetAttachedRigidbodies().GetUniqueOnly();
        
        Debug.Log ("Found " + uniqueObjsInSun.Length + " objects inside the sun's radius. Destroying...");
        for(int i = 0; i < uniqueObjsInSun.Length; i++)
        {
            Debug.Log ("Destroying " + uniqueObjsInSun[i] + "...");
            Destroy(uniqueObjsInSun[i].gameObject);
        }
        
        // Now check all planet ring levels, and 'circlecast' to destroy any asteroids that would impede their orbit
        CirclecastPlanetsToAbsorbAsteroids();*/
        #endregion

        #region ThirdPass
        //Check asteroids again, to compensate for bigger planets
        //CirclecastPlanetsToAbsorbAsteroids();
        
        // Unparent all shops
        networkView.RPC ("UnparentAllShops", RPCMode.All);
        #endregion

        //Truncate level boundary
        #region LevelBoundary, Spawns and Exit

        // Boundary 
        GameObject levelBound = Network.Instantiate(m_levelBoundary, Vector3.zero, Quaternion.identity, 0) as GameObject;
        LevelBoundary lbSc = levelBound.GetComponent<LevelBoundary>();
        
        float boundaryDist = (furthestExtent + 50.0f);
        lbSc.SetBoundaryScale(new Vector3(boundaryDist, boundaryDist, boundaryDist));
        
        // Enemy spawn points + manager
        Network.Instantiate(m_spawnPoint, new Vector3(0, -furthestExtent, 10.5f), Quaternion.identity, 0);
        Network.Instantiate(m_spawnPoint, new Vector3(0, furthestExtent, 10.5f), Quaternion.Euler(0, 0, 180), 0);
        Network.Instantiate(m_spawnPoint, new Vector3(-furthestExtent, 0, 10.5f), Quaternion.Euler(0, 0, -90), 0);
        Network.Instantiate(m_spawnPoint, new Vector3(furthestExtent, 0, 10.5f), Quaternion.Euler(0, 0, 90), 0);
        
        //Diagonals
        float testX1 = furthestExtent * Mathf.Cos(45.0f * Mathf.PI / 180f);
        float testY1 = furthestExtent * Mathf.Sin(45.0f * Mathf.PI / 180f);
        Network.Instantiate(m_spawnPoint, new Vector3(testX1, testY1, 10.5f), Quaternion.Euler(0, 0, 135), 0);
        
        float testX2 = furthestExtent * Mathf.Cos(135.0f * Mathf.PI / 180f);
        float testY2 = furthestExtent * Mathf.Sin(135.0f * Mathf.PI / 180f);
        Network.Instantiate(m_spawnPoint, new Vector3(testX2, testY2, 10.5f), Quaternion.Euler(0, 0, 225), 0);
        
        float testX3 = furthestExtent * Mathf.Cos(225.0f * Mathf.PI / 180f);
        float testY3 = furthestExtent * Mathf.Sin(225.0f * Mathf.PI / 180f);
        Network.Instantiate(m_spawnPoint, new Vector3(testX3, testY3, 10.5f), Quaternion.Euler(0, 0, 315), 0);
        
        float testX4 = furthestExtent * Mathf.Cos(315.0f * Mathf.PI / 180f);
        float testY4 = furthestExtent * Mathf.Sin(315.0f * Mathf.PI / 180f);
        Network.Instantiate(m_spawnPoint, new Vector3(testX4, testY4, 10.5f), Quaternion.Euler(0, 0, 45), 0);
        
        GameObject spawnMan = Network.Instantiate(m_spawnManager, Vector3.zero, Quaternion.identity, 0) as GameObject;
        spawnMan.GetComponent<EnemySpawnManagerScript>().InitSpawnPoints();
        
        GameStateController.Instance().UpdateAttachedSpawnManager(spawnMan);
        GameObject.FindGameObjectWithTag("GUIManager").GetComponent<GUIInGameMaster>().SetFurthestExtent(furthestExtent);
        
        //Spawn + Exit points
        Network.Instantiate(m_startMarker, new Vector3(-(furthestExtent + 50.0f), 0, 10.5f), Quaternion.Euler(0, 0, -90), 0);
        /*GameObject start = new GameObject("Capital Start Point");
        start.transform.position = new Vector3(-(furthestExtent + 50.0f), 0, 10.5f);
        start.transform.rotation = Quaternion.Euler(0, 0, -90);
        start.tag = "CSStart";*/
        
        Network.Instantiate(m_endMarker, new Vector3((furthestExtent + 50.0f), 0, 10.5f), Quaternion.Euler(0, 0, 90), 0);
        /*GameObject end = new GameObject("Capital End Point");
        end.transform.position = new Vector3((furthestExtent + 50.0f), 0, 10.5f);
        end.transform.rotation = Quaternion.Euler(0, 0, 90);
        end.AddComponent<SphereCollider>();
        end.GetComponent<SphereCollider>().radius = 5.0f;
        end.GetComponent<SphereCollider>().isTrigger = true;
        end.AddComponent<CapitalShipTarget>();
        end.tag = "CSTarget";
        end.layer = Layers.objective;*/
        
        #endregion
    }
    
    #region HelperFuncs
    void CirclecastPlanetsToAbsorbAsteroids()
    {
        for(int i = 0; i < m_spawnPlanetsDistances.Count; i++)
        {
            float width = m_spawnedPlanets[i].transform.localScale.x * m_spawnedPlanets[i].transform.localScale.x * 45.0f;
            Debug.Log ("Width value: " + width);
            float minDist = m_spawnPlanetsDistances[i] - width;
            float maxDist = m_spawnPlanetsDistances[i] + width;
            
            Rigidbody[] lowerThanMaxs = Physics.OverlapSphere(new Vector3(0, 0, 50.0f), maxDist, 1 << Layers.asteroid).GetAttachedRigidbodies().GetUniqueOnly();
            Rigidbody[] lowerThanMins = Physics.OverlapSphere(new Vector3(0, 0, 50.0f), minDist, 1 << Layers.asteroid).GetAttachedRigidbodies().GetUniqueOnly();
            
            // Destroy objects within range
            int asteroidsMulched = DestroyObjectsWithinRange(lowerThanMins, lowerThanMaxs);
            
            // Increase the size of the colliding planet, to simulate mass collection from collision
            // TODO: add mass here
            float oldScale = m_spawnedPlanets[i].transform.localScale.x;
            oldScale += (asteroidsMulched * 0.001f);
            m_spawnedPlanets[i].transform.localScale = new Vector3(oldScale, oldScale, oldScale);
        }
    }
    
    public int ResetSeed()
    {
        m_seed = Random.Range(0, int.MaxValue);
        return m_seed;
    }
    public void SetSeed(int seed_)
    {
        m_seed = seed_;
    }
    
    /// <summary>
    /// Destroys the objects within range of a planet.
    /// </summary>
    /// <returns> Returns the number of objects destroyed in this path. </returns>
    /// <param name="objectsLowerThanMin">Objects lower than minimum extent.</param>
    /// <param name="objectsLowerThanMax">Objects lower than maximum extent.</param>
    int DestroyObjectsWithinRange(Rigidbody[] objectsLowerThanMin, Rigidbody[] objectsLowerThanMax)
    {
        List<GameObject> toDestroy = new List<GameObject>();
        
        for(int i = 0; i < objectsLowerThanMax.Length; i++)
        {
            //If object is within the max, but not within the min, it means it's in the DANGER ZONE
            if(!objectsLowerThanMin.Contains(objectsLowerThanMax[i]))
            {
                toDestroy.Add(objectsLowerThanMax[i].gameObject);
            }
        }
        
        int numDestroyed = toDestroy.Count;
        Debug.Log ("Found " + numDestroyed + " objects in a planet's orbital path. Destroying...");
        for(int i = 0; i < toDestroy.Count; i++)
        {
            Debug.Log ("Destroyed: " + toDestroy[i].name);
            Network.Destroy(toDestroy[i]);
        }
        
        return numDestroyed;
    }
    
    PlanetType GetMoonTypeFromDistanceToSunPowerAndParentType(float distance, float sunPower, PlanetType parentType)
    {
        float effectiveTempRating = distance / sunPower;
        
        switch(parentType)
        {
            case PlanetType.Volcanic:
            {
                effectiveTempRating -= 0.05f;
                break;
            }
            case PlanetType.Desert:
            {
                
                break;
            }
            case PlanetType.Terran:
            {
                
                break;
            }
            case PlanetType.Barren:
            {
                
                break;
            }
            case PlanetType.GasGiant:
            {
                effectiveTempRating -= 0.1f;
                break;
            }
            case PlanetType.Ice:
            {
                
                break;
            }
        }
        
        //Now find the appropriate moontype
        if(effectiveTempRating <= 0.3f)
        {
            return PlanetType.Volcanic;
        }
        else if(effectiveTempRating <= 0.5f)
        {
            return PlanetType.Terran;
        }
        else if(effectiveTempRating <= 0.9f)
        {
            return PlanetType.Barren;
        }
        else
        {
            return PlanetType.Ice;
        }
    }
    PlanetType GetPlanetTypeFromDistanceToSunpower(float distance, float sunPower)
    {
        float effectiveTemperatureRating = distance / sunPower;
        
        if(effectiveTemperatureRating <= 0.27f)
        {
            return PlanetType.Volcanic;
        }
        else if(effectiveTemperatureRating <= 0.3f)
        {
            int random = Random.Range (0, 2);
            if(random == 0)
                return PlanetType.Volcanic;
            else
                return PlanetType.Desert;
        }
        else if(effectiveTemperatureRating <= 0.45f)
        {
            return PlanetType.Desert;
        }
        else if(effectiveTemperatureRating <= 0.5f)
        {
            int random = Random.Range (0, 2);
            if(random == 0)
                return PlanetType.Desert;
            else
                return PlanetType.Terran;
        }
        else if(effectiveTemperatureRating <= 0.64f)
        {
            return PlanetType.Terran;
        }
        else if(effectiveTemperatureRating <= 0.68f)
        {
            int random = Random.Range (0, 2);
            if(random == 0)
                return PlanetType.Terran;
            else
                return PlanetType.Barren;
        }
        else if(effectiveTemperatureRating <= 0.9f)
        {
            return PlanetType.Barren;
        }
        else if(effectiveTemperatureRating <= 1.2f)
        {
            return PlanetType.GasGiant;
        }
        else
        {
            return PlanetType.Ice;
        }
    }
    
    Vector3 GetFreeFloatingRandomPosition(float requiredRange)
    {
        Debug.Log ("Detected farthest extent as " + furthestExtent);
        
        bool completed = false;
        Vector3 testPos = Vector3.zero;
        while(!completed)
        {
            Vector2 dir2 = Random.insideUnitCircle;
            Vector3 dir = new Vector3(dir2.x, dir2.y, 0.0f);
            dir.Normalize();
            float range = Random.Range(0, furthestExtent);
            testPos = Vector3.zero + (dir * range);
            testPos.z = 15.0f;
            
            Collider[] colliders = Physics.OverlapSphere(testPos, requiredRange);
            Debug.Log ("Found " + colliders.Length + " colliders at tested position: " + testPos);
            
            if(colliders.Length == 0)
            {
                Debug.Log ("Returning position " + testPos);
                completed = true;
            }
        }
        
        return testPos;
    }
    #endregion
    
    #region ExternalCalls
    public void RequestGenerateLevel(bool resetSeed)
    {
        Debug.Log ("Receieved level generation request...");
        float beginTimer = Time.realtimeSinceStartup;
    
        m_spawnedShops = new List<GameObject>();
        m_spawnedPlanets = new List<GameObject>();
        m_spawnPlanetsDistances = new List<float>();
        m_planetsUsedByShops = new List<GameObject>();
        if(resetSeed)
            ResetSeed();
        GenerateLevel();
        
        float timeTaken = (Time.realtimeSinceStartup - beginTimer);
        Debug.Log ("Completed level generation request. Time taken: " + timeTaken);
            
        Debug.Log ("Sending new references to GSC...");
        GameStateController.Instance().UpdateShopReferences();
    }
    
    public void StartGenerateCoroutine()
    {
        StartCoroutine(GenerateNewLevelCoroutine());
    }
    IEnumerator GenerateNewLevelCoroutine()
    {
        System.Random rand = new System.Random(m_seed);
        Random.seed = m_seed;
        Debug.Log ("Beginning procedural system generation, with seed value of: " + m_seed);
        GameStateController.Instance().DebugToHost("Client gen seed: " + m_seed);
        
        // Cached system variables
        float effectStarpowerCache = -1f;
        float starRelativeSizeScale = 1.0f;
        
        int orbitObjectNum = 0;
        float expectedDistance = 0.0f;
        
        #region Star
        int star = rand.Next(0, m_starPrefabs.Length);
        Debug.Log ("Spawning star #" + star + "...");
        float randomZ = Random.Range(0, 360);
        //NOTE: Change the z position if stars become 3D
        Network.Instantiate(m_starPrefabs[star], new Vector3(0f, 0f, 15.0f), Quaternion.Euler(0, 0, randomZ), 0);
        effectStarpowerCache = 0;
        GameObject[] stars = GameObject.FindGameObjectsWithTag("Star");
        float overallScale = 0;
        float totalScale = 0;
        float totalMass = 0;
        float oldestAge = 0;
        foreach(GameObject starGO in stars)
        {
            effectStarpowerCache += starGO.GetComponent<StarScript>().GetStarTemperature();
            totalScale += starGO.transform.localScale.x;
            
            totalMass += starGO.GetComponent<StarScript>().GetStarMass();
            
            float age = starGO.GetComponent<StarScript>().GetStarAge();
            if(age > oldestAge)
                oldestAge = age;
        }
        
        yield return new WaitForSeconds(0.1f);
        
        //Set caches
        overallScale = totalScale / stars.Length;
        starRelativeSizeScale = totalScale / 110.0f;
        effectStarpowerCache /= overallScale;
        
        orbitObjectNum = 4 + Mathf.RoundToInt((13.0f - oldestAge) * 0.6f);
        expectedDistance = 560.0f + (totalMass * 2.0f);
        Debug.Log ("System is expected to hold approximately " + orbitObjectNum + " natural objects, within a projected distance of " + expectedDistance + ".");
        #endregion
        
        
        #region First Pass
        
        // Generic calculations here    
        int numPlanets = Mathf.RoundToInt(orbitObjectNum * Random.Range(0.45f, 0.75f));
        int numMoons = Mathf.RoundToInt((orbitObjectNum - 4) * Random.Range (0.6f, 0.85f));
        int numBelts = Mathf.RoundToInt((orbitObjectNum - 4) * Random.Range (0.35f, 0.5f));
        int numFields = Mathf.RoundToInt((orbitObjectNum - 6) * Random.Range(0.33f, 0.425f));
        
        float planetSizeModLower = (0.28f + (4.5f / orbitObjectNum));
        float planetSizeModUpper = (0.08f + (8.75f / orbitObjectNum));
        
        
        #region Planets
        int planetRingCounter = 0;
        int spawnedMoonCounter = 0;
        
        Debug.Log("Spawning " + numPlanets + " planets...");
        for(int i = 0; i < numPlanets; i++)
        {
            int ringLevel = rand.Next(0, 2) + i;
            if(ringLevel <= planetRingCounter)
                ringLevel = planetRingCounter + 1;
            planetRingCounter = ringLevel;
            float distance = 35 * starRelativeSizeScale;
            for(int j = 0; j< ringLevel; j++)
            {
                distance += (50 * starRelativeSizeScale * Random.Range (0.9f, 1.1f));
            }
            
            //Get what type of planet this one should be
            PlanetType typeToSpawn = GetPlanetTypeFromDistanceToSunpower(distance, effectStarpowerCache);
            int planet = (int)(typeToSpawn) - 1;
            
            //Direction
            Vector2 direction = Random.insideUnitCircle;
            direction.Normalize();
            
            //Position
            Vector2 tempPos = Vector2.zero + (direction * distance);
            float scale = Random.Range(planetSizeModLower, planetSizeModUpper);
            Debug.Log ("Planet " + i + ") Spawning planet #" + planet + " with direction: " + direction + " at distance: " + distance + ", and scale: " + scale);
            
            GameObject planetObject = Network.Instantiate(m_planetsPrefabs[planet], new Vector3(tempPos.x, tempPos.y, 100.0f), Random.rotation, 0) as GameObject;
            m_spawnedPlanets.Add(planetObject);
            m_spawnPlanetsDistances.Add(distance);
            
            yield return new WaitForSeconds(0.1f);
            
            //Do type/size modifiers
            if(typeToSpawn == PlanetType.GasGiant)
                scale *= Random.Range(1.05f, 1.4f);
            else if(typeToSpawn == PlanetType.Volcanic || typeToSpawn == PlanetType.Ice)
                scale *= Random.Range(0.75f, 0.95f);
            
            float checkDist = Vector2.Distance(new Vector2(tempPos.x, tempPos.y), Vector2.zero);
            Debug.Log("Testing new extent of: " + checkDist);
            if(checkDist > furthestExtent)
                furthestExtent = checkDist;
            
            //Scale
            //planetObject.transform.localScale = new Vector3(scale, scale, scale);
            planetObject.GetComponent<OrbitingObject>().UpdateObjectScale(new Vector3(scale, scale, scale));
            
            //See if we should spawn a ring
            float baseRingChance = Random.Range(3.0f, 4.5f);
            baseRingChance += scale;
            
            if(typeToSpawn == PlanetType.GasGiant)
                baseRingChance += 1.0f;
            
            if(baseRingChance >= 6.2f)
            {
                int ringType = Random.Range(0, m_ringObjects.Length);
                Debug.Log ("Planet " + i + ") Spawning a ring of type " + ringType + "...");
                GameObject ring = Network.Instantiate(m_ringObjects[ringType], tempPos, Quaternion.identity, 0) as GameObject;
                
                ring.GetComponent<PlanetRing>().SetParentAndScale(planetObject.transform, new Vector3(50.0f, 50.0f, 50.0f));
                /*ring.transform.parent = planetObject.transform;
                ring.transform.localPosition = Vector3.zero;
                ring.transform.localScale = new Vector3(50.0f, 50.0f, 50.0f);*/
                
                //Work out a suitable rotation for the ring
                
                float randX = Random.Range(-85.0f, 85.0f);
                float randY = Random.Range(-85.0f, 85.0f);
                
                Quaternion ringRot = Quaternion.Euler(randX, randY, 0);
                ring.transform.rotation = ringRot;
            }
            
            //Moons
            if(spawnedMoonCounter < numMoons)
            {
                float moonChance = Random.Range (-1.5f, 3.0f);
                int numMoonsToSpawn = (int)(scale - moonChance);
                
                Debug.Log ("Planet " + i + ") Spawning " + numMoonsToSpawn + " moons...");
                for(int j = 0; j < numMoonsToSpawn; j++)
                {
                    ++spawnedMoonCounter;
                    
                    //Work out dist, then planet as normal
                    float moonDist = 45.0f * scale;
                    for(int ring = 0; ring < j; ring++)
                    {
                        moonDist += (15.0f * Random.Range (0.9f, 1.1f)) * scale;
                    }
                    
                    PlanetType moonTypeToSpawn = GetMoonTypeFromDistanceToSunPowerAndParentType(distance, effectStarpowerCache, typeToSpawn);
                    int moonType = (int)(moonTypeToSpawn) - 1;
                    
                    //Direction
                    Vector2 moonDirection = Random.insideUnitCircle;
                    direction.Normalize();
                    
                    //Position
                    Vector2 moonTempPos = tempPos + (moonDirection * moonDist);
                    float moonScale = Random.Range(0.2f * scale, 0.3f * scale);
                    Debug.Log ("Planet " + i + ") Moon " + j + ") Spawning moon #" + moonType + " with direction: " + direction + " at distance: " + distance + ", and scale: " + moonScale);
                    
                    GameObject moonObject = Network.Instantiate(m_planetsPrefabs[moonType], new Vector3(moonTempPos.x, moonTempPos.y, 100.0f), Random.rotation, 0) as GameObject;
                    m_spawnedPlanets.Add(moonObject);
                    
                    yield return new WaitForSeconds(0.1f);
                    //moonObject.transform.parent = planetObject.transform;
                    
                    //Do type/size modifiers
                    if(moonTypeToSpawn == PlanetType.GasGiant)
                        moonScale *= Random.Range(1.05f, 1.4f);
                    else if(moonTypeToSpawn == PlanetType.Volcanic || typeToSpawn == PlanetType.Ice)
                        moonScale *= Random.Range(0.75f, 0.95f);
                    
                    float checkMoonDist = Vector2.Distance(new Vector2(moonTempPos.x, moonTempPos.y), Vector2.zero);
                    Debug.Log("Testing new extent of: " + checkMoonDist);
                    if(checkMoonDist > furthestExtent)
                        furthestExtent = checkMoonDist;
                    
                    //Scale
                    //moonObject.transform.localScale = new Vector3(moonScale, moonScale, moonScale);
                    moonObject.GetComponent<OrbitingObject>().UpdateObjectParentScale(new Vector3(scale, scale, scale), planetObject.transform);
                }
            }
        }
        
        //Find all shadows and make sure they're correctly aligned
        networkView.RPC ("UpdateShadowRots", RPCMode.All);
        yield return new WaitForSeconds(0.1f);
        #endregion
        
        
        #region Asteroids
        int asteroidRingCounter = 0;
        
        Debug.Log ("Spawning " + numBelts + " asteroid belts...");
        for(int i = 0; i < numBelts; i++)
        {
            GameObject asteroidMan = Network.Instantiate(m_asteroidManager, Vector3.zero, Quaternion.identity, 0) as GameObject;
            AsteroidManager asManSc = asteroidMan.GetComponent<AsteroidManager>();
            
            //Set range
            int ringLevel = rand.Next(1, 3) + i;
            if(ringLevel <= asteroidRingCounter)
                ringLevel = asteroidRingCounter + 1;
            
            float range = 60 * starRelativeSizeScale;
            for(int j = 0; j < ringLevel; j++)
            {
                range += (50 * starRelativeSizeScale * Random.Range(0.9f, 1.1f));
            }
            asteroidRingCounter = ringLevel;
            asManSc.SetRange(range);
            
            //Check extent
            if(range > furthestExtent)
                furthestExtent = range;
            
            //Thickness
            float thickness = Random.Range(3.0f, 10.0f);
            asManSc.SetThickness(thickness);
            
            //Number
            int numAster = (int)(range * 0.5f); 
            asManSc.SetAsteroidNum(numAster);
            
            //Ensure ring
            asManSc.SetIsRing(true);
            
            //Test
            Debug.Log ("Spawning asteroid belt with a range of " + range + " (ring level: " + ringLevel + ")....");
            asManSc.SpawnAsteroids();
            yield return new WaitForSeconds(0.1f);
        }
        
        for(int i = 0; i < numFields; i++)
        {
            GameObject asteroidMan = Network.Instantiate(m_asteroidManager, Vector3.zero, Quaternion.identity, 0) as GameObject;
            AsteroidManager asManSc = asteroidMan.GetComponent<AsteroidManager>();
            
            //Generate a size for the field
            float range = Random.Range (50.0f, 150.0f);
            
            //Find a suitable position
            Vector3 fieldPos = GetFreeFloatingRandomPosition(range);
            
            //Ensure field
            asManSc.SetIsRing(false);
            
            //Set field vars
            asManSc.SetRange(range);
            asteroidMan.transform.position = fieldPos;
            float asteroidDensity = Random.Range (1.25f, 2.0f);
            int numAster = (int)(range * asteroidDensity);
            asManSc.SetAsteroidNum(numAster);
            
            //Test
            Debug.Log ("Spawning asteroid field with a range of " + range + ", centered on position " + fieldPos + ", with " + numAster + " asteroids.");
            asManSc.SpawnAsteroids();
            yield return new WaitForSeconds(0.1f);
        }
        #endregion
        
        
        #region Shops/Shipyards
        int numShops = rand.Next(3, 7);
        
        Debug.Log ("Spawning " + numShops + " shops...");
        for(int i = 0;i < numShops; i++)
        {
            //Type
            bool isShipyard = (rand.Next(0, 4) == 3);
            bool isOrbital = (rand.Next(0, 2) == 0);
            
            if(isOrbital)
            {
                //Get a parent planet
                GameObject targetPlanet = null;
                bool isUseable = false;
                int counter = 0;
                while(!isUseable)
                {
                    counter++;
                    int spPlID = rand.Next(0, m_spawnedPlanets.Count);
                    
                    if(!m_planetsUsedByShops.Contains(m_spawnedPlanets[spPlID]))
                    {
                        targetPlanet = m_spawnedPlanets[spPlID];
                        isUseable = true;
                    }
                    else if(counter > 6)
                    {
                        isUseable = true;
                        isOrbital = false;
                    }
                }
                
                if(isOrbital)
                {
                    //Get a direction
                    Vector2 shopDir = Random.insideUnitCircle;
                    shopDir.Normalize();
                    Vector3 shopTrueDir = new Vector3(shopDir.x, shopDir.y, 15.0f);
                    
                    //Get a relative distance
                    float shopDist = targetPlanet.transform.localScale.x * 20.0f;
                    
                    //Calculate the position
                    Vector3 shopPos = targetPlanet.transform.position + (shopTrueDir * shopDist);
                    
                    string shopName = isShipyard ? "shipyard" : "shop";
                    Debug.Log ("Spawning " + shopName + " orbiting planet: " + targetPlanet.name + ".");
                    GameObject shop = null;
                    if(isShipyard)
                        shop = Network.Instantiate(m_shipyard, shopPos, Quaternion.identity, 0) as GameObject;
                    else
                        shop = Network.Instantiate(m_shop, shopPos, Quaternion.identity, 0) as GameObject;
                        
                    yield return new WaitForSeconds(0.1f);
                    
                    shop.transform.position = new Vector3(shop.transform.position.x, shop.transform.position.y, 11.0f);
                    
                    m_spawnedShops.Add (shop);
                    m_planetsUsedByShops.Add (targetPlanet);
                    
                    shop.transform.parent = targetPlanet.transform;
                    
                    //Check extent
                    float checkShopDist = Vector2.Distance(new Vector2(shopPos.x, shopPos.y), Vector2.zero);
                    if(checkShopDist > furthestExtent)
                        furthestExtent = checkShopDist;
                }
            }
            
            if(!isOrbital)
            {
                Vector3 randomPos = GetFreeFloatingRandomPosition(15.0f);
                
                string shopName = isShipyard ? "shipyard" : "shop";
                Debug.Log ("Spawning " + shopName + " at free-floating coords " + randomPos);
                GameObject shop = null;
                if(isShipyard)
                    shop = Network.Instantiate(m_shipyard, randomPos, Quaternion.identity, 0) as GameObject;
                else
                    shop = Network.Instantiate(m_shop, randomPos, Quaternion.identity, 0) as GameObject;
                    
                yield return new WaitForSeconds(0.1f);
                m_spawnedShops.Add (shop);
            }
        }
        #endregion
        
        
        #endregion
        
        
        #region SecondPass
        // In this pass, we will sanity check what we have so far, and make sure everything we have makes at least theoretical sense
        
        // First, make sure nothing is inside the sun
        /*Collider[] objsInsideSun = Physics.OverlapSphere(new Vector3(0, 0, 50.0f), totalScale);
        Rigidbody[] uniqueObjsInSun = objsInsideSun.GetAttachedRigidbodies().GetUniqueOnly();
        
        Debug.Log ("Found " + uniqueObjsInSun.Length + " objects inside the sun's radius. Destroying...");
        for(int i = 0; i < uniqueObjsInSun.Length; i++)
        {
            Debug.Log ("Destroying " + uniqueObjsInSun[i] + "...");
            Destroy(uniqueObjsInSun[i].gameObject);
        }
        
        // Now check all planet ring levels, and 'circlecast' to destroy any asteroids that would impede their orbit
        CirclecastPlanetsToAbsorbAsteroids();*/
        #endregion
        
        #region ThirdPass
        //Check asteroids again, to compensate for bigger planets
        //CirclecastPlanetsToAbsorbAsteroids();
        
        // Unparent all shops
        networkView.RPC ("UnparentAllShops", RPCMode.All);
        #endregion
        
        //Truncate level boundary
        #region LevelBoundary, Spawns and Exit
        
        // Boundary 
        GameObject levelBound = Network.Instantiate(m_levelBoundary, Vector3.zero, Quaternion.identity, 0) as GameObject;
        LevelBoundary lbSc = levelBound.GetComponent<LevelBoundary>();
        
        float boundaryDist = (furthestExtent + 50.0f);
        lbSc.SetBoundaryScale(new Vector3(boundaryDist, boundaryDist, boundaryDist));
        
        // Enemy spawn points + manager
        Network.Instantiate(m_spawnPoint, new Vector3(0, -furthestExtent, 10.5f), Quaternion.identity, 0);
        Network.Instantiate(m_spawnPoint, new Vector3(0, furthestExtent, 10.5f), Quaternion.Euler(0, 0, 180), 0);
        Network.Instantiate(m_spawnPoint, new Vector3(-furthestExtent, 0, 10.5f), Quaternion.Euler(0, 0, -90), 0);
        Network.Instantiate(m_spawnPoint, new Vector3(furthestExtent, 0, 10.5f), Quaternion.Euler(0, 0, 90), 0);
        
        //Diagonals
        float testX1 = furthestExtent * Mathf.Cos(45.0f * Mathf.PI / 180f);
        float testY1 = furthestExtent * Mathf.Sin(45.0f * Mathf.PI / 180f);
        Network.Instantiate(m_spawnPoint, new Vector3(testX1, testY1, 10.5f), Quaternion.Euler(0, 0, 135), 0);
        
        float testX2 = furthestExtent * Mathf.Cos(135.0f * Mathf.PI / 180f);
        float testY2 = furthestExtent * Mathf.Sin(135.0f * Mathf.PI / 180f);
        Network.Instantiate(m_spawnPoint, new Vector3(testX2, testY2, 10.5f), Quaternion.Euler(0, 0, 225), 0);
        
        float testX3 = furthestExtent * Mathf.Cos(225.0f * Mathf.PI / 180f);
        float testY3 = furthestExtent * Mathf.Sin(225.0f * Mathf.PI / 180f);
        Network.Instantiate(m_spawnPoint, new Vector3(testX3, testY3, 10.5f), Quaternion.Euler(0, 0, 315), 0);
        
        float testX4 = furthestExtent * Mathf.Cos(315.0f * Mathf.PI / 180f);
        float testY4 = furthestExtent * Mathf.Sin(315.0f * Mathf.PI / 180f);
        Network.Instantiate(m_spawnPoint, new Vector3(testX4, testY4, 10.5f), Quaternion.Euler(0, 0, 45), 0);
        
        //Spawn + Exit points
        Network.Instantiate(m_startMarker, new Vector3(-(furthestExtent + 50.0f), 0, 10.5f), Quaternion.Euler(0, 0, -90), 0);
        
        Network.Instantiate(m_endMarker, new Vector3((furthestExtent + 50.0f), 0, 10.5f), Quaternion.Euler(0, 0, 90), 0);
        
        #endregion
        
        // Alert GUI it needs to await input
        GameObject.FindGameObjectWithTag("GUIManager").GetComponent<GUIInGameMaster>().AlertTransitionNeedsInput(true);
        GameObject.FindGameObjectWithTag("GUIManager").GetComponent<GUIInGameMaster>().SetFurthestExtent(furthestExtent);
        //GameStateController.Instance().EndCShipJumpTransition();
    }
    public void StartDestroyCoroutine()
    {
        StartCoroutine(DestroyCurrentLevelCoroutine());
    }
    IEnumerator DestroyCurrentLevelCoroutine()
    {
        Debug.Log ("Destroying current level...");
        
        Debug.Log ("Destroying planet...");
        for(int i = 0; i < m_spawnedPlanets.Count; i++)
        {
            Network.Destroy(m_spawnedPlanets[i]);
            yield return new WaitForSeconds(0.05f);
        }
        
        Debug.Log ("Done. Destroying shops...");
        for(int i = 0; i < m_spawnedShops.Count; i++)
        {
            Network.Destroy(m_spawnedShops[i]);
            yield return new WaitForSeconds(0.05f);
        }
        
        Debug.Log ("Done. Destroying asteroid managers...");
        GameObject[] managers = GameObject.FindGameObjectsWithTag("AsteroidManager");
        if(managers != null)
        {
            for(int i = 0; i < managers.Length; i++)
            {
                Network.Destroy(managers[i]);
                yield return new WaitForSeconds(0.05f);
            }
        }
        
        Debug.Log ("Done. Destroying asteroids...");
        Network.Destroy (GameObject.FindGameObjectWithTag("AsteroidParent"));
        yield return new WaitForSeconds(0.05f);
        
        Debug.Log ("Done. Destroying star...");
        GameObject[] stars = GameObject.FindGameObjectsWithTag("Star");
        for(int i = 0; i < stars.Length; i++)
        {
            if(stars[i] != null)
            {
                Network.Destroy(stars[i].transform.root.gameObject);
                yield return new WaitForSeconds(0.05f);
            }
        }
        
        Debug.Log ("Destroying level boundary...");
        Network.Destroy (GameObject.FindGameObjectWithTag("LevelBoundary"));
        
        Debug.Log ("Destroying CShip start/end points...");
        Network.Destroy (GameObject.FindGameObjectWithTag("CSStart"));
        Network.Destroy (GameObject.FindGameObjectWithTag("CSTarget"));
        
        Debug.Log ("Destroying spawn points and spawnmanager...");
        Network.Destroy (GameObject.FindGameObjectWithTag("SpawnManager"));
        GameObject[] points = GameObject.FindGameObjectsWithTag("SpawnPoint");
        for(int i = 0; i < points.Length; i++)
        {
            Destroy(points[i]);
        }
        
        Debug.Log ("Done. Scene should now be empty!");
        
        Debug.Log ("Resetting holders and seed...");
        m_seed = 0;
        m_spawnPlanetsDistances = new List<float>();
        m_spawnedPlanets = new List<GameObject>();
        m_planetsUsedByShops = new List<GameObject>();
        m_spawnedShops = new List<GameObject>();
        
        GameStateController.Instance().BeginGenerate();
    }
    public void DestroyCurrentLevel()
    {
        Debug.Log ("Destroying current level...");
        
        Debug.Log ("Destroying planets...");
        for(int i = 0; i < m_spawnedPlanets.Count; i++)
        {
            Network.Destroy(m_spawnedPlanets[i]);
        }
        
        Debug.Log ("Done. Destroying shops...");
        for(int i = 0; i < m_spawnedShops.Count; i++)
        {
            Network.Destroy(m_spawnedShops[i]);
        }
        
        Debug.Log ("Done. Destroying asteroid managers...");
        GameObject[] managers = GameObject.FindGameObjectsWithTag("AsteroidManager");
        if(managers != null)
        {
            for(int i = 0; i < managers.Length; i++)
            {
                Network.Destroy(managers[i]);
            }
        }
        
        Debug.Log ("Done. Destroying asteroids...");
        Network.Destroy (GameObject.FindGameObjectWithTag("AsteroidParent"));
        
        Debug.Log ("Done. Destroying star...");
        GameObject[] stars = GameObject.FindGameObjectsWithTag("Star");
        for(int i = 0; i < stars.Length; i++)
        {
            if(stars[i] != null)
            {
                Network.Destroy(stars[i].transform.root.gameObject);
            }
        }
        
        Debug.Log ("Destroying level boundary...");
        Network.Destroy (GameObject.FindGameObjectWithTag("LevelBoundary"));
        
        Debug.Log ("Destroying CShip start/end points...");
        Network.Destroy (GameObject.FindGameObjectWithTag("CSStart"));
        Network.Destroy (GameObject.FindGameObjectWithTag("CSTarget"));
        
        Network.Destroy (GameObject.FindGameObjectWithTag("SpawnManager"));
        GameObject[] points = GameObject.FindGameObjectsWithTag("SpawnPoint");
        for(int i = 0; i < points.Length; i++)
        {
            Destroy(points[i]);
        }
        
        Debug.Log ("Done. Scene should now be empty!");
        
        Debug.Log ("Resetting holders and seed...");
        m_seed = 0;
        m_spawnPlanetsDistances = new List<float>();
        m_spawnedPlanets = new List<GameObject>();
        m_planetsUsedByShops = new List<GameObject>();
        m_spawnedShops = new List<GameObject>();
    }
    #endregion
    
    [RPC] void UpdateShadowRots()
    {
        GameObject[] shadows = GameObject.FindGameObjectsWithTag("Shadow");
        for(int i = 0; i < shadows.Length; i++)
        {
            Quaternion target = Quaternion.LookRotation(new Vector3(0, 0, shadows[i].transform.position.z) - shadows[i].transform.position) * Quaternion.FromToRotation(Vector3.forward, Vector3.up);
            shadows[i].transform.rotation = target;
        }
    }
    
    [RPC] void UnparentAllShops()
    {
        GameObject[] shops = GameObject.FindGameObjectsWithTag("Shop");
        for(int i = 0; i < shops.Length; i++)
        {
            shops[i].transform.parent = null;
        }
    }
}

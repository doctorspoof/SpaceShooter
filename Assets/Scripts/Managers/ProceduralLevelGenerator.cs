using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ProceduralLevelGenerator : MonoBehaviour 
{
    /* Serializable Members */
    [SerializeField]        GameObject[]        m_starPrefabs;
    [SerializeField]        GameObject[]        m_planetsPrefabs;
    [SerializeField]        GameObject[]        m_moonPrefabs;
    [SerializeField]        GameObject          m_asteroidManager;
    [SerializeField]        GameObject          m_shop;
    [SerializeField]        GameObject          m_shipyard;
    [SerializeField]        int                 m_seed =            0;
    [SerializeField]        bool                m_tempDestroyScene = false;
    [SerializeField]        bool                m_tempGenerateScene = false;

    /* Internal Members */
    
    float furthestExtent = 0.0f;
    
    #region SpawnedObjects
    List<GameObject> m_spawnedPlanets;
    List<GameObject> m_planetsUsedByShops;
    List<GameObject> m_spawnedShops;
    #endregion

    /* Unity Functions */
    void Awake()
    {
        //Remove nulls from arrays
        m_starPrefabs = m_starPrefabs.NoNulls();
        m_planetsPrefabs = m_planetsPrefabs.NoNulls();
        m_moonPrefabs = m_moonPrefabs.NoNulls();
    }

	void Start () 
    {
        if(m_seed == 0)
            ResetSeed();
            
        RequestGenerateLevel();
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
            RequestGenerateLevel();
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
        
        #region Star
        int star = rand.Next(0, m_starPrefabs.Length);
        Debug.Log ("Spawning star #" + star + "...");
        float randomZ = Random.Range(0, 360);
        Instantiate(m_starPrefabs[star], new Vector3(0f, 0f, 15.0f), Quaternion.Euler(0, 0, randomZ));
        #endregion
        
        #region Planets
        int numPlanets = rand.Next(2, 7);
        int planetRingCounter = 0;
        Debug.Log ("Spawning " + numPlanets + " planets...");
        for(int i = 0; i < numPlanets; i++)
        {
            //Planet type should be affected by distance to star (hotter planets when closer)
            int ringLevel = rand.Next(0, 2) + i;
            if(ringLevel <= planetRingCounter)
                ringLevel = planetRingCounter + 1;
            float distance = 200;
            for(int j = 0; j < ringLevel; j++)
            {
                distance += ringLevel * (100 * Random.Range(0.9f, 1.1f));
            }
            planetRingCounter = ringLevel;
            int heatEffect = Mathf.RoundToInt((distance * Random.Range(0.9f, 1.1f)) / 205);
            int planet = rand.Next(0, m_planetsPrefabs.Length);
            
            if(heatEffect > planet)
                planet++;
            else if(heatEffect < planet)
                planet--;
                
            if(planet >= m_planetsPrefabs.Length)
                planet = m_planetsPrefabs.Length - 1;
            else if(planet < 0)
                planet = 0;
            
            //Direction
            Vector2 direction = Random.insideUnitCircle;
            direction.Normalize();
            
            //Position
            Vector2 tempPos = Vector2.zero + (direction * distance);
            float scale = Random.Range(0.9f, 4.2f);
            Debug.Log ("Planet " + i + ") Spawning planet #" + planet + " with direction: " + direction + " at distance: " + distance + ", and scale: " + scale);
            
            GameObject planetObject = Instantiate(m_planetsPrefabs[planet], new Vector3(tempPos.x, tempPos.y, 15.0f), Random.rotation) as GameObject;
            m_spawnedPlanets.Add(planetObject);
            
            //Check for furthest extent
            if(Mathf.Abs(tempPos.x) > furthestExtent)
                furthestExtent = Mathf.Abs(tempPos.x);
            else if(Mathf.Abs(tempPos.y) > furthestExtent)
                furthestExtent = Mathf.Abs(tempPos.y);
            
            //Scale
            planetObject.transform.localScale = new Vector3(scale, scale, scale);
            
            //If the planet is above a certain size, consider giving it a belt of it's own
            if(scale > 2.8f)
            {
                int decider = rand.Next(0, 10);
                if(decider == 6)
                {
                    //Do one ring
                    GameObject asteroidMan = Instantiate(m_asteroidManager, Vector3.zero, Quaternion.identity) as GameObject;
                    AsteroidManager asManSc = asteroidMan.GetComponent<AsteroidManager>();
                    asteroidMan.transform.position = planetObject.transform.position;
                    
                    //Set range
                    float range = 10 + (30 * Random.Range(0.85f, 1.15f));
                    asManSc.SetRange(range);
                    
                    //Check extent
                    if(range > furthestExtent)
                        furthestExtent = range;
                    
                    //Thickness
                    float thickness = Random.Range(2.5f, 6.5f);
                    asManSc.SetThickness(thickness);
                    
                    //Number
                    int numAster = (int)(range * 3.0f);
                    asManSc.SetAsteroidNum(numAster);
                    
                    //Ensure ring
                    asManSc.SetIsRing(true);
                    
                    //Test
                    Debug.LogWarning ("Planet " + i + "): Spawning asteroid belt around planet '" + planetObject + "', with range of " + range + ", consisting of " + numAster + " asteroids.");
                    asManSc.SetTestSpawns(true);
                }
            }
            
            //Decide if the planet should have a moon
            int numMoons = rand.Next(0, 3);
            Debug.Log ("Planet " + i + "): Spawning " + numMoons + " moons...");
            for(int j = 0; j < numMoons; j++)
            {
                //Decide on the type of moon to spawn
                int moon = rand.Next(0, m_moonPrefabs.Length);
                
                //Decide on direction from parent planet
                //Vector2 moonDirection = Random.insideUnitCircle;
                Vector3 moonDirection = Random.onUnitSphere;
                //direction.Normalize();
                
                //Decide on distance
                float ringFactor = scale * 30.0f; //should make rings for moons approximately accurate
                float moonDistance = ringFactor + (i * (ringFactor * 0.5f * Random.Range(0.8f, 1.2f)));
                
                //Position
                Vector3 tempMoonPos = new Vector3(tempPos.x, tempPos.y, 15.0f) + (moonDirection * moonDistance);
                float moonScale = Random.Range(0.8f, 1.2f);
                
                //Instantiate
                Debug.Log ("Planet " + i + ") Moon " + j + "] Spawning moon #" + moon + " with direction: " + moonDirection + " at distance: " + moonDistance + ", and scale factor " + moonScale);
                //GameObject moonObject = Instantiate(m_moonPrefabs[moon], new Vector3(tempMoonPos.x, tempMoonPos.y, 15.0f), Random.rotation) as GameObject;
                GameObject moonObject = Instantiate(m_moonPrefabs[moon], tempMoonPos, Random.rotation) as GameObject;
                m_spawnedPlanets.Add(moonObject);
                
                //Check extent
                if(Mathf.Abs(tempMoonPos.x) > furthestExtent)
                    furthestExtent = Mathf.Abs(tempMoonPos.x);
                else if(Mathf.Abs(tempMoonPos.y) > furthestExtent)
                    furthestExtent = Mathf.Abs(tempMoonPos.y);
                
                //Scale
                float newScale = moonObject.transform.localScale.x * moonScale;
                moonObject.transform.localScale = new Vector3(newScale, newScale , newScale);
                
                //Parent
                moonObject.transform.parent = planetObject.transform;
            }
        }
        
        //Find all shadows and make sure they're correctly aligned
        GameObject[] shadows = GameObject.FindGameObjectsWithTag("Shadow");
        for(int i = 0; i < shadows.Length; i++)
        {
            Quaternion target = Quaternion.LookRotation(Vector3.zero - shadows[i].transform.position) * Quaternion.FromToRotation(Vector3.forward, Vector3.up);
            shadows[i].transform.rotation = target;
        }
        #endregion
        
        #region AsteroidManagers
        int numAsteroidBelts = rand.Next(1,5);
        int asteroidRingCounter = 0;
        
        Debug.Log ("Spawning " + numAsteroidBelts + " asteroid belts...");
        for(int i = 0; i < numAsteroidBelts; i++)
        {
            GameObject asteroidMan = Instantiate(m_asteroidManager, Vector3.zero, Quaternion.identity) as GameObject;
            AsteroidManager asManSc = asteroidMan.GetComponent<AsteroidManager>();
            
            //Set range
            int ringLevel = rand.Next(1, 3) + i;
            if(ringLevel <= asteroidRingCounter)
                ringLevel = asteroidRingCounter + 1;
            //float range = 150 + (ringLevel * (100 * Random.Range(0.9f, 1.1f)));
            float range = 150;
            for(int j = 0; j < ringLevel; j++)
            {
                range += (100 * Random.Range(0.9f, 1.1f));
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
            int numAster = (int)(range * 2.25f);
            asManSc.SetAsteroidNum(numAster);
            
            //Ensure ring
            asManSc.SetIsRing(true);
            
            //Test
            Debug.Log ("Spawning asteroid belt with a range of " + range + " (ring level: " + ringLevel + ")....");
            asManSc.SetTestSpawns(true);
        }
        
        int numAsteroidFields = rand.Next(0,3);
        for(int i = 0; i < numAsteroidFields; i++)
        {
            GameObject asteroidMan = Instantiate(m_asteroidManager, Vector3.zero, Quaternion.identity) as GameObject;
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
            float asteroidDensity = Random.Range (2.5f, 6.0f);
            int numAster = (int)(range * asteroidDensity);
            asManSc.SetAsteroidNum(numAster);
            
            //Test
            Debug.Log ("Spawning asteroid field with a range of " + range + ", centered on position " + fieldPos + ", with " + numAster + " asteroids.");
            asManSc.SetTestSpawns(true);
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
                while(!isUseable)
                {
                    int spPlID = rand.Next(0, m_spawnedPlanets.Count);
                    
                    if(!m_planetsUsedByShops.Contains(m_spawnedPlanets[spPlID]))
                    {
                        targetPlanet = m_spawnedPlanets[spPlID];
                        isUseable = true;
                    }
                }
                
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
                    shop = Instantiate(m_shipyard, shopPos, Quaternion.identity) as GameObject;
                else
                    shop = Instantiate(m_shop, shopPos, Quaternion.identity) as GameObject;
                    
                shop.transform.position = new Vector3(shop.transform.position.x, shop.transform.position.y, 11.0f);
                
                m_spawnedShops.Add (shop);
                m_planetsUsedByShops.Add (targetPlanet);
                
                //Check extent
                if(Mathf.Abs(shopPos.x) > furthestExtent)
                    furthestExtent = Mathf.Abs(shopPos.x);
                else if(Mathf.Abs(shopPos.y) > furthestExtent)
                    furthestExtent = Mathf.Abs(shopPos.y);

            }
            else
            {
                Vector3 randomPos = GetFreeFloatingRandomPosition(15.0f);
                
                string shopName = isShipyard ? "shipyard" : "shop";
                Debug.Log ("Spawning " + shopName + " at free-floating coords " + randomPos);
                GameObject shop = null;
                if(isShipyard)
                    shop = Instantiate(m_shipyard, randomPos, Quaternion.identity) as GameObject;
                else
                    shop = Instantiate(m_shop, randomPos, Quaternion.identity) as GameObject;
                m_spawnedShops.Add (shop);
            }
        }
        #endregion

        //Truncate level boundary
        
    }
    
    #region HelperFuncs
    void ResetSeed()
    {
        m_seed = Random.Range(0, int.MaxValue);
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
    public void RequestGenerateLevel()
    {
        Debug.Log ("Receieved level generation request...");
        float beginTimer = Time.realtimeSinceStartup;
    
        m_spawnedShops = new List<GameObject>();
        m_spawnedPlanets = new List<GameObject>();
        m_planetsUsedByShops = new List<GameObject>();
        GenerateLevel();
        
        float timeTaken = (Time.realtimeSinceStartup - beginTimer);
        Debug.Log ("Completed level generation request. Time taken: " + timeTaken);
    }
    
    public void DestroyCurrentLevel()
    {
        Debug.Log ("Destroying current level...");
        
        Debug.Log ("Destroying planets...");
        for(int i = 0; i < m_spawnedPlanets.Count; i++)
        {
            Destroy(m_spawnedPlanets[i]);
        }
        
        Debug.Log ("Done. Destroying shops...");
        for(int i = 0; i < m_spawnedShops.Count; i++)
        {
            Destroy(m_spawnedShops[i]);
        }
        
        Debug.Log ("Done. Destroying asteroid managers...");
        GameObject[] managers = GameObject.FindGameObjectsWithTag("AsteroidManager");
        if(managers != null)
        {
            for(int i = 0; i < managers.Length; i++)
            {
                Destroy(managers[i]);
            }
        }
        
        Debug.Log ("Done. Destroying asteroids...");
        Destroy (GameObject.FindGameObjectWithTag("AsteroidParent"));
        
        Debug.Log ("Done. Destroying star...");
        GameObject[] stars = GameObject.FindGameObjectsWithTag("Star");
        for(int i = 0; i < stars.Length; i++)
        {
            Destroy (stars[i]);
        }
        
        Debug.Log ("Done. Scene should now be empty!");
        
        Debug.Log ("Resetting holders and seed...");
        m_seed = 0;
        m_spawnedPlanets = new List<GameObject>();
        m_spawnedShops = new List<GameObject>();
    }
    #endregion
}

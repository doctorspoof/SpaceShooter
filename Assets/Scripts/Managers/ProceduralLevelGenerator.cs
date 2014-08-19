using UnityEngine;
using System.Collections;

public class ProceduralLevelGenerator : MonoBehaviour 
{
    /* Serializable Members */
    [SerializeField]        GameObject[]        m_starPrefabs;
    [SerializeField]        GameObject[]        m_planetsPrefabs;
    [SerializeField]        GameObject[]        m_moonPrefabs;
    
    

    /* Internal Members */
    int m_seed = 1337;

	void Start () 
    {
        //Remove this later
        GenerateLevel();
	}
	
	void Update () 
    {
	
	}
    
    /* Custom Functions */
    void GenerateLevel()
    {
        //Randomise this late
        System.Random rand = new System.Random();
        //Random.seed = m_seed;
        Debug.Log ("Beginning procedural system generation, with seed value of: " + m_seed);
        
        //Decide on a star
        int star = rand.Next(0, m_starPrefabs.Length);
        Debug.Log ("Spawning star #" + star + "...");
        float randomZ = Random.Range(0, 360);
        Instantiate(m_starPrefabs[star], new Vector3(0f, 0f, 15.0f), Quaternion.Euler(0, 0, randomZ));
        
        //Decide on the number of planets to spawn
        int numPlanets = rand.Next(1, 7);
        Debug.Log ("Spawning " + numPlanets + " planets...");
        for(int i = 0; i < numPlanets; i++)
        {
            //Planet type should be affected by distance to star (hotter planets when closer)
            float distance = 125 + (i * (100 * Random.Range(0.8f, 1.2f)));
            int heatEffect = Mathf.RoundToInt((distance * Random.Range(0.9f, 1.1f)) / 205);
            int planet = rand.Next(0, m_planetsPrefabs.Length);
            
            if(heatEffect > planet)
                planet++;
            else if(heatEffect < planet)
                planet--;
                
            if(planet >= m_planetsPrefabs.Length)
                planet = m_planetsPrefabs.Length;
            else if(planet < 0)
                planet = 0;
            
            //Direction
            Vector2 direction = Random.insideUnitCircle;
            direction.Normalize();
            
            //Position
            Vector2 tempPos = Vector2.zero + (direction * distance);
            float scale = Random.Range(0.9f, 2.4f);
            Debug.Log ("Planet " + i + ") Spawning planet #" + planet + " with direction: " + direction + " at distance: " + distance + ", and scale: " + scale);
            
            GameObject planetObject = Instantiate(m_planetsPrefabs[planet], new Vector3(tempPos.x, tempPos.y, 15.0f), Random.rotation) as GameObject;
            
            //Scale
            planetObject.transform.localScale = new Vector3(scale, scale, scale);
            
            //Decide if the planet should have a moon
            int numMoons = rand.Next(0, 3);
            Debug.Log ("Planet " + i + "): Spawning " + numMoons + " moons...");
            for(int j = 0; j < numMoons; j++)
            {
                //Decide on the type of moon to spawn
                int moon = rand.Next(0, m_moonPrefabs.Length);
                
                //Decide on direction from parent planet
                Vector2 moonDirection = Random.insideUnitCircle;
                direction.Normalize();
                
                //Decide on distance
                float ringFactor = scale * 35.0f; //should make rings for moons approximately accurate
                float moonDistance = ringFactor + (i * (ringFactor * 0.5f * Random.Range(0.8f, 1.2f)));
                
                //Position
                Vector2 tempMoonPos = tempPos + (moonDirection * moonDistance);
                float moonScale = Random.Range(0.8f, 1.2f);
                
                //Instantiate
                Debug.Log ("Planet " + i + ") Moon " + j + "] Spawning moon #" + moon + " with direction: " + moonDirection + " at distance: " + moonDistance + ", and scale factor " + moonScale);
                GameObject moonObject = Instantiate(m_moonPrefabs[moon], new Vector3(tempMoonPos.x, tempMoonPos.y, 15.0f), Random.rotation) as GameObject;
                
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
        
        
    }
}

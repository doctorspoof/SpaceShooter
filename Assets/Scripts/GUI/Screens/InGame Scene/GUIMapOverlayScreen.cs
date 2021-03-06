using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GUIMapOverlayScreen : BaseGUIScreen 
{
    /* Serializable Members */
    [SerializeField]    Texture     m_mapOverlay;
    [SerializeField]    Texture     m_selfPBlob;
    [SerializeField]    Texture     m_otherPBlob;
    [SerializeField]    Texture     m_cShipBlob;
    [SerializeField]    Texture     m_enemyBlob;
    [SerializeField]    Texture     m_specEnemyBlob;
    
    /* Internal Members */
    List<GameObject> m_drawables = null;
    GameObject[] m_playerShips = null;
    GameObject[] m_pingedEnemies = null;
    GameObject[] m_pingedMissiles = null;
    GameObject[] m_shops = null;
    GameObject m_exit = null;
    bool m_isBigMap = false;
    bool m_isFollowMap = false;
    
    // Map size info
    float m_furthestExtent = 0.0f;
    
    #region Setters
    public void AlertUpdatePlanetReferences()
    {
        //Find all planets, belts, stars and fields
        GameObject[] planets = GameObject.FindGameObjectsWithTag("Planet");
        GameObject[] stars = GameObject.FindGameObjectsWithTag("Star");
        GameObject[] asteroids = GameObject.FindGameObjectsWithTag("AsteroidManager");
        
        m_drawables = new List<GameObject>();
        for(int i = 0; i < planets.Length; i++)
        {
            m_drawables.Add(planets[i]);
            
            if(i < stars.Length)
                m_drawables.Add(stars[i]);
                
            if(i < asteroids.Length)
                m_drawables.Add(asteroids[i]);
        }
    }
    public void SetFurthestExtent(float extent)
    {
        m_furthestExtent = extent;
    }
    public bool ToggleBigMap()
    {
        m_isBigMap = !m_isBigMap;
        return m_isBigMap;
    }
    public void ToggleSmallMap()
    {
        m_isFollowMap = !m_isFollowMap;
    }
    public void ToggleMapsTogether()
    {
        if(m_isBigMap)
        {
            m_isBigMap = false;
            m_isFollowMap = true;
        }
        else
        {
            if(m_isFollowMap)
            {
                m_isBigMap = false;
                m_isFollowMap = false;
            }
            else
            {
                m_isBigMap = true;
            }
        }
    }
    public void ResetPlayerList()
    {
        m_playerShips = GameObject.FindGameObjectsWithTag("Player");
    }
    public void UpdateShopList()
    {
        m_shops = GameObject.FindGameObjectsWithTag("Shop");
        m_exit = GameObject.FindGameObjectWithTag("CSTarget");
    }
    #endregion
    
    float m_blobSize;
    
    /* Cached Members */
    PlayerControlScript m_playerCache = null;
    CapitalShipScript m_cshipCache = null;
    GameStateController m_gscCache = null;
    
    #region Setters
    public void SetCShipReference(GameObject ship)
    {
        m_cshipCache = ship.GetComponent<CapitalShipScript>();
    }
    
    public void SetPlayerReference(GameObject ship)
    {
        m_playerCache = ship.GetComponent<PlayerControlScript>();
    }
    #endregion
    
    /* Unity Functions */
    void Start () 
    {
        m_priorityValue = 2;
        m_blobSize = Screen.height * 0.015f;
        
        m_drawables = new List<GameObject>();
    }
    
    /* Custom Functions */
    #region Draw Functions
    public override void ManualGUICall (bool shouldRecieveInput)
    {
        if (m_isBigMap)
            DrawMap();
        else
        {
            if (m_isFollowMap)
                DrawSmallFollowMap();
            else
            {
                DrawSmallMap();
            }
        }
    }
    
    void DrawMap()
    {
        //Store gui matrix, restore to identity to remove scaling
        Matrix4x4 oldGUIMat = GUI.matrix;
        GUI.matrix = Matrix4x4.identity;
        
        //Map should be screen.height * screen.height, center on 1/2 screen.width
        GUI.DrawTexture(new Rect((Screen.width * 0.5f) - Screen.height * 0.5f, 0, Screen.height, Screen.height), m_mapOverlay);
        
        //Exit portal
        Vector2 exitPos = WorldToMapPos(m_exit.transform.position);
        GUI.DrawTexture(new Rect(exitPos.x - (m_blobSize * 0.5f), exitPos.y - (m_blobSize * 0.5f), m_blobSize, m_blobSize), m_otherPBlob);
        
        //Draw the 'drawables'
        for(int i = 0; i < m_drawables.Count; i++)
        {
            Vector2 drawableSpotPos = WorldToMapPos(m_drawables[i].transform.position);
            
            Texture drawableBlob = null;
            float blobSize = 0;
            if(m_drawables[i].GetComponent<OrbitingObject>())
            {
                drawableBlob = m_drawables[i].GetComponent<OrbitingObject>().GetPlanetMinimapBlip();
                blobSize = m_drawables[i].transform.localScale.x * 15;
            }
            else if(m_drawables[i].GetComponent<StarScript>())
            {
                drawableBlob = m_drawables[i].GetComponent<StarScript>().GetMinimapBlip();
                blobSize = m_drawables[i].transform.localScale.x * 0.25f;
            }
            else
            {
                AsteroidManager asmansc = m_drawables[i].GetComponent<AsteroidManager>();
                drawableBlob = asmansc.GetMinimapBlip();
                
                if(asmansc.GetIsRing())
                    blobSize = asmansc.GetRange() / m_furthestExtent * (Screen.height * 0.8f);
                else
                    blobSize = asmansc.GetRange() / m_furthestExtent * (Screen.height * 0.8f);
            }
            
            
            GUI.DrawTexture(new Rect(drawableSpotPos.x - (blobSize * 0.5f), drawableSpotPos.y - (blobSize * 0.5f), blobSize, blobSize), drawableBlob);
        }
        
        //Now draw blobs
        
        //Player - self
        if (m_playerCache != null)
        {
            Vector2 playerSpotPos = WorldToMapPos(m_playerCache.transform.position);
            GUI.DrawTexture(new Rect(playerSpotPos.x - (m_blobSize * 0.5f), playerSpotPos.y - (m_blobSize * 0.5f), m_blobSize, m_blobSize), m_selfPBlob);
        }
        else
        {
            Vector2 playerSpotPos = WorldToMapPos(Camera.main.transform.position);
            GUI.DrawTexture(new Rect(playerSpotPos.x - (m_blobSize * 0.5f), playerSpotPos.y - (m_blobSize * 0.5f), m_blobSize, m_blobSize), m_selfPBlob);
        }
        
        //Player - others
        if (m_playerShips != null)
        {
            foreach (GameObject player in m_playerShips)
            {
                //if(player != null && player != thisPlayerHP.gameObject)
                if (player && (m_playerCache && player != m_playerCache.gameObject))
                {
                    Vector2 playPos = WorldToMapPos(player.transform.position);
                    GUI.DrawTexture(new Rect(playPos.x - (m_blobSize * 0.5f), playPos.y - (m_blobSize * 0.5f), m_blobSize, m_blobSize), m_otherPBlob);
                    Debug.Log ("Trying to draw player: " + player.name);
                    GUI.Label(new Rect(playPos.x - (m_blobSize * 1.5f), playPos.y + (m_blobSize * 0.5f), 75, 40),
                              m_gscCache.GetNameFromNetworkPlayer(player.GetComponent<PlayerControlScript>().GetOwner()));
                }
            }
        }
        else
        {
            m_playerShips = GameObject.FindGameObjectsWithTag("Player");
        }
        
        //CShip
        if (m_cshipCache != null)
        {
            Vector2 cshipPos = WorldToMapPos(m_cshipCache.transform.position);
            GUI.DrawTexture(new Rect(cshipPos.x - (m_blobSize * 0.5f), cshipPos.y - (m_blobSize * 0.5f), m_blobSize, m_blobSize), m_cShipBlob);
            
            // Now, draw the CShip's waypoints on the map
            List<Vector2> waypoints = m_cshipCache.GetWaypoints();
            
            for(int i = 0; i < waypoints.Count; i++)
            {
                Vector2 mapPos = WorldToMapPos(new Vector3(waypoints[i].x, waypoints[i].y, 0));
                GUI.DrawTexture(new Rect(mapPos.x - (m_blobSize * 0.25f), mapPos.y - (m_blobSize * 0.25f), m_blobSize * 0.5f, m_blobSize * 0.5f), m_cShipBlob);
            }
            
            // Listen for input, check for waypoint requests
            if (Event.current.type == EventType.MouseDown)
            {
                Rect mapArea = new Rect((Screen.width * 0.5f) - Screen.height * 0.5f, 0, Screen.height, Screen.height);
                if(mapArea.Contains(Event.current.mousePosition))
                {
                    Vector2 worldPos = MapToWorldPos(Event.current.mousePosition);
                    
                    if(!Input.GetKey(KeyCode.LeftControl))
                        m_cshipCache.ClearMoveWaypoints();
                    
                    
                    m_cshipCache.AddMoveWaypoint(worldPos);
                }
            }
        }
        else
        {
            m_cshipCache = GameObject.FindGameObjectWithTag("Capital").GetComponent<CapitalShipScript>();
        }
        
        //Enemies?
        if (m_pingedEnemies != null)
        {
            foreach (GameObject enemy in m_pingedEnemies)
            {
                if (enemy != null)// && )IsEnemyInViewableRange(enemy.transform.position))
                {
                    Vector2 pingPos = WorldToMapPos(enemy.transform.position);
                    if (enemy.GetComponent<ShipEnemy>().IsSpecial())
                    {
                        GUI.DrawTexture(new Rect(pingPos.x - (m_blobSize * 0.5f), pingPos.y - (m_blobSize * 0.5f), m_blobSize, m_blobSize), m_specEnemyBlob);
                    }
                    else
                    {
                        GUI.DrawTexture(new Rect(pingPos.x - (m_blobSize * 0.5f), pingPos.y - (m_blobSize * 0.5f), m_blobSize, m_blobSize), m_enemyBlob);
                    }
                }
            }
        }
        
        //Missiles
        if(m_pingedMissiles != null && m_pingedMissiles.Length != 0)
        {
            foreach(GameObject bullet in m_pingedMissiles)
            {
                if(bullet != null)
                {
                    Vector2 pingPos = WorldToMapPos(bullet.transform.position);
                    GUI.DrawTexture(new Rect(pingPos.x - (m_blobSize * 0.2f), pingPos.y - (m_blobSize * 0.2f), m_blobSize, m_blobSize), m_specEnemyBlob);
                }
            }
        }
        
        //Shops!
        for (int i = 0; i < m_shops.Length; i++)
        {
            Vector2 shopPos = WorldToMapPos(m_shops[i].transform.position);
            GUI.Label(new Rect(shopPos.x - 10, shopPos.y - 10, 20, 20), "$", "Label");
        }
        
        //When we're done, reset the gui matrix
        GUI.matrix = oldGUIMat;
    }
    
    void DrawSmallFollowMap()
    {
        //Store gui matrix, restore to identity to remove scaling
        Matrix4x4 oldGUIMat = GUI.matrix;
        GUI.matrix = Matrix4x4.identity;
        float pixelGapPercent = (53.0f) / (Screen.height * 0.5f);
        //float mapSize = 280.0f;
        float mapSize = m_furthestExtent;
        
        //If map is bottom left:
        //Map should be screen.height/5 * screen.height/5, centered on (screen.height/5, (screen.height/5)*4)
        //GUI.DrawTexture(new Rect((Screen.width * 0.5f) - Screen.height * 0.5f, 0, Screen.height, Screen.height), m_mapOverlay);
        
        //Step one: Get 'imagepos' from playerPos
        Vector2 imagePos = Vector2.zero;
        if (m_playerCache != null)
            imagePos = new Vector2(m_playerCache.transform.position.x / mapSize, m_playerCache.transform.position.y / mapSize);
        else
            imagePos = new Vector2(Camera.main.transform.position.x / mapSize, Camera.main.transform.position.y / mapSize);
        Vector2 playerPos = imagePos;
        imagePos *= (1.0f - pixelGapPercent);
        imagePos.x /= 2;
        imagePos.x += 0.5f;
        imagePos.y /= 2;
        imagePos.y += 0.5f;
        
        //Step two: draw map around this area
        Rect drawRect = new Rect(0, (Screen.height / 4.0f) * 3.0f, Screen.height / 4.0f, Screen.height / 4.0f);
        float texDrawArea = 0.25f;
        GUI.DrawTextureWithTexCoords(drawRect,
                                     m_mapOverlay,
                                     new Rect((imagePos.x - (texDrawArea / 2)), (imagePos.y - (texDrawArea / 2)), texDrawArea, texDrawArea));
        
        GUI.DrawTexture(new Rect((Screen.height * 0.125f) - (m_blobSize * 0.5f), ((Screen.height * 0.125f) * 7.0f) - (m_blobSize * 0.5f), m_blobSize, m_blobSize), m_selfPBlob);
        
        //Now draw planets and stuff
        for(int i = 0; i < m_drawables.Count; i++)
        {
            //Vector2 drawableSpotPos = WorldToSmallMapPos(m_drawables[i].transform.position);
            Vector2 drawableSpotPos = new Vector2(m_drawables[i].transform.position.x / mapSize, m_drawables[i].transform.position.y / mapSize);
            
            Texture drawableBlob = null;
            float blobSize = 0;
            if(m_drawables[i].GetComponent<OrbitingObject>())
            {
                drawableBlob = m_drawables[i].GetComponent<OrbitingObject>().GetPlanetMinimapBlip();
                blobSize = m_drawables[i].transform.localScale.x * 15;
            }
            else if(m_drawables[i].GetComponent<StarScript>())
            {
                drawableBlob = m_drawables[i].GetComponent<StarScript>().GetMinimapBlip();
                blobSize = m_drawables[i].transform.localScale.x * 0.25f;
            }
            else
            {
                AsteroidManager asmansc = m_drawables[i].GetComponent<AsteroidManager>();
                drawableBlob = asmansc.GetMinimapBlip();
                
                if(asmansc.GetIsRing())
                    blobSize = asmansc.GetRange() * 0.65f * (m_furthestExtent / 280.0f);
                else
                    blobSize = asmansc.GetRange() * 0.45f * (m_furthestExtent / 280.0f);
            }
            
            drawableSpotPos.x -= playerPos.x;
            drawableSpotPos.y -= playerPos.y;
            
            Vector2 drawPos = Vector2.zero;
            drawPos.x = drawableSpotPos.x * (Screen.height * (0.5f - (pixelGapPercent * 0.5f)));
            drawPos.y = drawableSpotPos.y * (Screen.height * (0.5f - (pixelGapPercent * 0.5f)));
            
            Vector2 finalDrawPos = new Vector2((Screen.height * 0.125f) + drawPos.x,
                                               ((Screen.height * 0.125f) * 7.0f) - drawPos.y);
            
            if(drawRect.Contains(finalDrawPos))
            {
                GUI.DrawTexture(new Rect(finalDrawPos.x - (blobSize * 0.5f), finalDrawPos.y - (blobSize * 0.5f), blobSize, blobSize), drawableBlob);
            }
        }
        
        //Step three: draw CShip blob
        if (m_cshipCache != null)
        {
            Vector2 cshipRelMapPos = new Vector2(m_cshipCache.transform.position.x / mapSize, m_cshipCache.transform.position.y / mapSize);
            
            //Relativise cship -> player
            cshipRelMapPos.x -= playerPos.x;
            cshipRelMapPos.y -= playerPos.y;
            
            Vector2 drawPos = Vector2.zero;
            drawPos.x = cshipRelMapPos.x * (Screen.height * (0.5f - (pixelGapPercent * 0.5f)));
            drawPos.y = cshipRelMapPos.y * (Screen.height * (0.5f - (pixelGapPercent * 0.5f)));
            
            Vector2 finalDrawPos = new Vector2((Screen.height * 0.125f) + drawPos.x,
                                               ((Screen.height * 0.125f) * 7.0f) - drawPos.y);
            if (drawRect.Contains(finalDrawPos))
            {
                GUI.DrawTexture(new Rect(finalDrawPos.x - (m_blobSize * 0.5f),
                                         finalDrawPos.y - (m_blobSize * 0.5f),
                                         m_blobSize, m_blobSize), m_cShipBlob);
            }
        }
        
        //Draw other players:
        if (m_playerShips != null)
        {
            foreach (GameObject player in m_playerShips)
            {
                //if(player != null && player != thisPlayerHP.gameObject)
                if (player && (!m_playerCache || player != m_playerCache.gameObject))
                {
                    Vector2 playerMapPos = new Vector2(player.transform.position.x / mapSize, player.transform.position.y / mapSize);
                    
                    playerMapPos.x -= playerPos.x;
                    playerMapPos.y -= playerPos.y;
                    
                    Vector3 playerDrawPos = Vector2.zero;
                    playerDrawPos.x = playerMapPos.x * (Screen.height * (0.5f));
                    playerDrawPos.y = playerMapPos.y * (Screen.height * (0.5f));
                    
                    Vector2 finalDrawPos = new Vector2((Screen.height * 0.125f) + playerDrawPos.x,
                                                       ((Screen.height * 0.125f) * 7.0f) - playerDrawPos.y);
                    
                    if (drawRect.Contains(finalDrawPos))
                    {
                        GUI.DrawTexture(new Rect(finalDrawPos.x - (m_blobSize * 0.5f),
                                                 finalDrawPos.y - (m_blobSize * 0.5f),
                                                 m_blobSize, m_blobSize), m_otherPBlob);
                    }
                }
            }
        }
        else
        {
            m_playerShips = GameObject.FindGameObjectsWithTag("Player");
        }
        
        //Enemies
        if (m_pingedEnemies != null)
        {
            foreach (GameObject enemy in m_pingedEnemies)
            {
                if (enemy != null)
                {
                    Vector2 enemyMapPos = new Vector2(enemy.transform.position.x / mapSize, enemy.transform.position.y / mapSize);
                    enemyMapPos.x -= playerPos.x;
                    enemyMapPos.y -= playerPos.y;
                    
                    Vector3 enemyDrawPos = Vector2.zero;
                    enemyDrawPos.x = enemyMapPos.x * (Screen.height * (0.5f));
                    enemyDrawPos.y = enemyMapPos.y * (Screen.height * (0.5f));
                    
                    Vector2 finalDrawPos = new Vector2((Screen.height * 0.125f) + enemyDrawPos.x,
                                                       ((Screen.height * 0.125f) * 7.0f) - enemyDrawPos.y);
                    
                    if (drawRect.Contains(finalDrawPos))
                    {
                        if (enemy.GetComponent<ShipEnemy>().IsSpecial())
                        {
                            GUI.DrawTexture(new Rect(finalDrawPos.x - (m_blobSize * 0.5f),
                                                     finalDrawPos.y - (m_blobSize * 0.5f),
                                                     m_blobSize, m_blobSize), m_specEnemyBlob);
                        }
                        else
                        {
                            GUI.DrawTexture(new Rect(finalDrawPos.x - (m_blobSize * 0.5f),
                                                     finalDrawPos.y - (m_blobSize * 0.5f),
                                                     m_blobSize, m_blobSize), m_enemyBlob);
                        }
                    }
                }
            }
        }
        
        //Missiles
        if(m_pingedMissiles != null && m_pingedMissiles.Length != 0)
        {
            foreach(GameObject bullet in m_pingedMissiles)
            {
                if(bullet != null)
                {
                    Vector2 enemyMapPos = new Vector2(bullet.transform.position.x / mapSize, bullet.transform.position.y / mapSize);
                    enemyMapPos.x -= playerPos.x;
                    enemyMapPos.y -= playerPos.y;
                    
                    Vector3 enemyDrawPos = Vector2.zero;
                    enemyDrawPos.x = enemyMapPos.x * (Screen.height * (0.5f));
                    enemyDrawPos.y = enemyMapPos.y * (Screen.height * (0.5f));
                    
                    Vector2 finalDrawPos = new Vector2((Screen.height * 0.125f) + enemyDrawPos.x,
                                                       ((Screen.height * 0.125f) * 7.0f) - enemyDrawPos.y);
                    
                    if (drawRect.Contains(finalDrawPos))
                    {
                        GUI.DrawTexture(new Rect(finalDrawPos.x - (m_blobSize * 0.2f), finalDrawPos.y - (m_blobSize * 0.2f), m_blobSize, m_blobSize), m_specEnemyBlob);
                    }
                }
            }
        }
        
        //Always reset the gui!
        GUI.matrix = oldGUIMat;
    }
    
    void DrawSmallMap()
    {
        Matrix4x4 oldGUIMat = GUI.matrix;
        GUI.matrix = Matrix4x4.identity;
        
        GUI.DrawTexture(new Rect(0, (Screen.height * 0.25f) * 3.0f, Screen.height * 0.25f, Screen.height * 0.25f), m_mapOverlay);
        
        //Draw map
        for(int i = 0; i < m_drawables.Count; i++)
        {
            Vector2 drawableSpotPos = WorldToSmallMapPos(m_drawables[i].transform.position);
            
            Texture drawableBlob = null;
            float blobSize = 0;
            if(m_drawables[i].GetComponent<OrbitingObject>())
            {
                drawableBlob = m_drawables[i].GetComponent<OrbitingObject>().GetPlanetMinimapBlip();
                blobSize = m_drawables[i].transform.localScale.x * 7.5f;
            }
            else if(m_drawables[i].GetComponent<StarScript>())
            {
                drawableBlob = m_drawables[i].GetComponent<StarScript>().GetMinimapBlip();
                blobSize = m_drawables[i].transform.localScale.x * 0.125f;
            }
            else
            {
                AsteroidManager asmansc = m_drawables[i].GetComponent<AsteroidManager>();
                drawableBlob = asmansc.GetMinimapBlip();
                
                if(asmansc.GetIsRing())
                    //blobSize = asmansc.GetRange() * 0.26f * (m_furthestExtent / 300.0f) * (m_furthestExtent / 300.0f);
                    blobSize = asmansc.GetRange() / m_furthestExtent * (Screen.height * 0.2f);
                else
                    blobSize = asmansc.GetRange() / m_furthestExtent * (Screen.height * 0.2f);
            }
            
            
            GUI.DrawTexture(new Rect(drawableSpotPos.x - (blobSize * 0.5f), drawableSpotPos.y - (blobSize * 0.5f), blobSize, blobSize), drawableBlob);
        }
        
        //Draw Self
        if (m_playerCache != null)
        {
            Vector2 playerSpotPos = WorldToSmallMapPos(m_playerCache.transform.position);
            GUI.DrawTexture(new Rect(playerSpotPos.x - (m_blobSize * 0.25f), playerSpotPos.y - (m_blobSize * 0.25f), m_blobSize * 0.5f, m_blobSize * 0.5f), m_selfPBlob);
        }
        else
        {
            Vector2 playerSpotPos = WorldToSmallMapPos(Camera.main.transform.position);
            GUI.DrawTexture(new Rect(playerSpotPos.x - (m_blobSize * 0.25f), playerSpotPos.y - (m_blobSize * 0.25f), m_blobSize * 0.5f, m_blobSize * 0.5f), m_selfPBlob);
        }
        
        //Draw CShip
        if (m_cshipCache != null)
        {
            Vector2 cshipPos = WorldToSmallMapPos(m_cshipCache.transform.position);
            GUI.DrawTexture(new Rect(cshipPos.x - (m_blobSize * 0.25f), cshipPos.y - (m_blobSize * 0.25f), m_blobSize * 0.5f, m_blobSize * 0.5f), m_cShipBlob);
        }
        else
            m_cshipCache = GameObject.FindGameObjectWithTag("Capital").GetComponent<CapitalShipScript>();
        
        //Draw others
        if (m_playerShips != null)
        {
            foreach (GameObject player in m_playerShips)
            {
                //if(player != null && player != thisPlayerHP.gameObject)
                if (player && (!m_playerCache || player != m_playerCache.gameObject))
                {
                    Vector2 playPos = WorldToSmallMapPos(player.transform.position);
                    GUI.DrawTexture(new Rect(playPos.x - (m_blobSize * 0.25f), playPos.y - (m_blobSize * 0.25f), m_blobSize * 0.5f, m_blobSize * 0.5f), m_otherPBlob);
                }
            }
        }
        else
        {
            m_playerShips = GameObject.FindGameObjectsWithTag("Player");
        }
        
        //Draw enemies
        if (m_pingedEnemies != null)
        {
            foreach (GameObject enemy in m_pingedEnemies)
            {
                if (enemy != null)
                {
                    //Check if enemy is in viewable range
                    //if(IsEnemyInViewableRange(enemy.transform.position))
                    //{
                    Vector2 pingPos = WorldToSmallMapPos(enemy.transform.position);
                    if(enemy.GetComponent<ShipEnemy>().IsSpecial())
                    {
                        GUI.DrawTexture(new Rect(pingPos.x - (m_blobSize * 0.25f), pingPos.y - (m_blobSize * 0.25f), m_blobSize * 0.5f, m_blobSize * 0.5f), m_specEnemyBlob);
                    }
                    else
                    {
                        GUI.DrawTexture(new Rect(pingPos.x - (m_blobSize * 0.25f), pingPos.y - (m_blobSize * 0.25f), m_blobSize * 0.5f, m_blobSize * 0.5f), m_enemyBlob);
                    }
                    //}
                }
            }
        }
        
        //Missiles
        if(m_pingedMissiles != null && m_pingedMissiles.Length != 0)
        {
            foreach(GameObject bullet in m_pingedMissiles)
            {
                if(bullet != null)
                {
                    Vector2 pingPos = WorldToSmallMapPos(bullet.transform.position);
                    GUI.DrawTexture(new Rect(pingPos.x - (m_blobSize * 0.2f), pingPos.y - (m_blobSize * 0.2f), m_blobSize, m_blobSize), m_specEnemyBlob);
                }
            }
        }
        
        GUI.matrix = oldGUIMat;
    }
    #endregion
    
    #region MapCoordFuncs
    Vector2 MapToWorldPos(Vector2 mapPos)
    {
        float pixelGapPercent = 53.0f / (Screen.height);
        Vector2 output = Vector2.zero;
        mapPos.y = -mapPos.y;
        
        //output.x = (((mapPos.x - (Screen.height)) / (Screen.height)) * (m_furthestExtent));
        //output.y = (((mapPos.y + (Screen.height * 0.5f)) / (Screen.height)) * (m_furthestExtent * 0.5f));
        
        output.x = (((mapPos.x) - (Screen.width * 0.5f)) / (Screen.height * (0.5f - pixelGapPercent))) * (m_furthestExtent );
        output.y = (((mapPos.y) + (Screen.height * 0.5f)) / (Screen.height * (0.5f - pixelGapPercent))) * (m_furthestExtent );
        
        return output;
    }
    Vector2 WorldToMapPos(Vector3 worldPos)
    {
        //gap is 24px, this as a percentage of the screen changes dependant upon screen size
        //if the screen is 900 high, the gap of 24 as a percentage is 2.666666667% or 0.0266666666667
        float pixelGapPercent = 53.0f / (Screen.height);
        
        Vector2 output = Vector2.zero;
        //World is still -275 -> 275
        //Map is now:
        //X:        (Screen.width * 0.5f) - (Screen.height * (0.5f - pixelGapPercent)) -> (Screen.width * 0.5f + (Screen.height * (0.5f - pixelGapPercent))
        //Y:        (pixelGapPercent * Screen.height) -> (Screen.height - (pixelGapPercent * Screen.height))
        
        //x
        //output.x = (Screen.width * 0.5f) + ((worldPos.x / 275.0f) * (Screen.height * (0.5f - pixelGapPercent)));
        output.x = (Screen.width * 0.5f) + ((worldPos.x / m_furthestExtent) * (Screen.height * (0.5f - pixelGapPercent)));
        
        //y
        //output.y = (Screen.height * 0.5f) - ((worldPos.y / 275.0f) * (Screen.height * (0.5f - pixelGapPercent)));
        output.y = (Screen.height * 0.5f) - ((worldPos.y / m_furthestExtent) * (Screen.height * (0.5f - pixelGapPercent)));
        return output;
    }
    Vector2 WorldToSmallMapPos(Vector3 worldPos)
    {
        float pixelGapPercent = (53.0f * 0.25f) / (Screen.height);
        //World is still -275 -> 275
        //Map is now:
        //X:        0 -> (Screen.height * 0.25f)
        //Y:        (Screen.height * 0.25f) * 3.0f -> Screen.height
        Vector2 output = Vector2.zero;
        
        //X:
        //output.x = (worldPos.x / 275.0f) * (Screen.height * 0.25f);
        //output.x = (Screen.height * 0.125f) + ((worldPos.x / 275.0f) * (Screen.height * (0.125f - pixelGapPercent)));
        output.x = (Screen.height * 0.125f) + ((worldPos.x / m_furthestExtent) * (Screen.height * (0.125f - pixelGapPercent)));
        
        //Y:
        //output.y = ((Screen.height * 0.25f) * 3.0f) + ((worldPos.y / 275.0f) * Screen.height);
        //output.y = ((Screen.height * 0.125f) * 7.0f) - ((worldPos.y / 275.0f) * (Screen.height * (0.125f - pixelGapPercent)));
        output.y = ((Screen.height * 0.125f) * 7.0f) - ((worldPos.y / m_furthestExtent) * (Screen.height * (0.125f - pixelGapPercent)));
        
        return output;
    }
    
    bool IsEnemyInViewableRange(Vector3 position)
    {
        float maxDist = 65.0f;
        //If the enemy is within distance X of any player of CShip, then return true
        
        if (m_playerCache != null)
        {
            float distToPlayer = Vector3.Distance(position, m_playerCache.transform.position);
            if (distToPlayer <= maxDist)
                return true;
        }
        else
        {
            float distToCam = Vector2.Distance(new Vector2(position.x, position.y), new Vector2(Camera.main.transform.position.x, Camera.main.transform.position.y));
            if (distToCam <= maxDist)
                return true;
        }
        
        if (m_cshipCache != null)
        {
            float distToCShip = Vector3.Distance(position, m_cshipCache.transform.position);
            if (distToCShip <= maxDist)
                return true;
        }
        
        if (m_playerShips != null)
        {
            for (int i = 0; i < m_playerShips.Length; i++)
            {
                float dist = Vector3.Distance(position, m_playerShips[i].transform.position);
                if (dist <= maxDist)
                    return true;
            }
        }
        
        return false;
    }
    #endregion
}

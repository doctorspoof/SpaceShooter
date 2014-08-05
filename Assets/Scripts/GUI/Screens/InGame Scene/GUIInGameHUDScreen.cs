using UnityEngine;
using System.Collections;

public class GUIInGameHUDScreen : BaseGUIScreen 
{
    /* Serializable Members */
    [SerializeField]        Texture m_barEnd;
    [SerializeField]        Texture m_barMid;
    [SerializeField]        Texture m_healthBackground;
    [SerializeField]        Texture m_healthBar;
    [SerializeField]        Texture m_shieldBar;
    [SerializeField]        Texture m_iconBorder;
    [SerializeField]        Texture m_playerIcon;
    [SerializeField]        Texture m_cShipIcon;
    [SerializeField]        Texture m_menuBackground;
    [SerializeField]        Texture m_cursor;
    [SerializeField]        Texture m_cursorLocking;
    [SerializeField]        Texture m_cursorLocked;
    [SerializeField]        Texture m_reloadBackground;
    [SerializeField]        Texture m_reloadBar;
    [SerializeField]        Texture m_lockedTarget;
    
    // Internal members
    bool m_shouldShowCShipUnderFire = false;
    bool m_playerHasDied = false;
    bool m_isOnCShipDeathSequence = false;
    bool m_noRespawnCash = false;
    bool m_isOutOfBounds = false;
    float m_cShipAttackTimer = 0.0f;
    
    //Locking vars
    bool m_isLockedOn = false;
    bool m_isLockingOn = false;
    GameObject m_targetGO = null;
    
    #region Setters
    public void SetPlayerDead(bool state)
    {
        m_playerHasDied = state;
    }
    public void SetNoRespawnCash(bool state)
    {
        m_noRespawnCash = state;
    }
    public void SetCShipUnderFire()
    {
        m_shouldShowCShipUnderFire = true;
        m_cShipAttackTimer = 0.0f;
    }
    public void SetIsOutOfBounds(bool state)
    {
        m_isOutOfBounds = state; 
    }
    #endregion
    
    // Cached members
    GameStateController m_gscCache;
    HealthScript m_playerHPCache;
    HealthScript m_cShipHPCache;
    
    #region
    public void SetCShipReference(GameObject cship)
    {
        m_cShipHPCache = cship.GetComponent<HealthScript>();
    }
    public void SetPlayerReference(GameObject ship)
    {
        m_playerHPCache = ship.GetComponent<HealthScript>();
    }
    #endregion
    
    /* Unity Functions */
    void Start () 
    {
        m_priorityValue = 1;
        m_gscCache = GameStateController.Instance();
    }
    
    void Update ()
    {
        if(m_shouldShowCShipUnderFire)
        {
            if(m_cShipAttackTimer < 3.5f)
                m_cShipAttackTimer += Time.deltaTime;
            else
                m_shouldShowCShipUnderFire = false;
        }
    }
    
    /* Custom Functions */
    public override void ManualGUICall (bool shouldRecieveInput)
    {
        //Show gametime
        GUI.DrawTexture(new Rect(690, 5, 10, 50), m_barEnd);
        GUI.DrawTexture(new Rect(700, 5, 200, 50), m_barMid);
        GUI.DrawTexture(new Rect(910, 5, -10, 50), m_barEnd);
        int seconds2 = (int)m_gscCache.GetGameTimer();
        string displayedTime = string.Format("{0:00}:{1:00}", (seconds2 / 60) % 60, seconds2 % 60);
        GUI.Label(new Rect(710, 10, 180, 40), displayedTime, "No Box");
        
        //Now, finally, draw the hp/shield areas
        if (m_playerHPCache != null)
        {
            //New New HP Bar
            float healthPercent = m_playerHPCache.GetHPPercentage();
            healthPercent = Mathf.Max(0, healthPercent);
            float shieldPercent = m_playerHPCache.GetShieldPercentage();
            shieldPercent = Mathf.Max(0, shieldPercent);
            
            GUI.DrawTexture(new Rect(0, 0, 150, 150), m_iconBorder);
            GUI.DrawTexture(new Rect(0, 0, 150, 150), m_playerIcon);
            
            GUI.DrawTexture(new Rect(150, 0, 350, 50), m_healthBackground);
            GUI.DrawTextureWithTexCoords(new Rect(150, 0, 350 * healthPercent, 50), m_healthBar, new Rect(0, 0, healthPercent, 1));
            GUI.DrawTextureWithTexCoords(new Rect(150, 0, 350 * shieldPercent, 50), m_shieldBar, new Rect(0, 0, shieldPercent, 1));
            
            //Show spacebux
            GUI.DrawTexture(new Rect(175, 80, 10, 50), m_barEnd);
            GUI.DrawTexture(new Rect(185, 80, 200, 50), m_barMid);
            GUI.DrawTexture(new Rect(395, 80, -10, 50), m_barEnd);
            GUI.Label(new Rect(195, 85, 180, 44), "$ " + m_playerHPCache.gameObject.GetComponent<PlayerControlScript>().GetCash(), "No Box");
        }
        else
        {
            GUI.DrawTexture(new Rect(0, 0, 150, 150), m_iconBorder);
            GUI.DrawTexture(new Rect(0, 0, 150, 150), m_playerIcon);
            
            GUI.DrawTexture(new Rect(150, 0, 350, 50), m_healthBackground);
            
            GUI.DrawTexture(new Rect(175, 80, 10, 50), m_barEnd);
            GUI.DrawTexture(new Rect(185, 80, 200, 50), m_barMid);
            GUI.DrawTexture(new Rect(395, 80, -10, 50), m_barEnd);
            GUI.Label(new Rect(195, 85, 180, 44), "--", "No Box");
        }
        
        //Now show CShip HP
        if (m_cShipHPCache == null)
        {
            GUI.DrawTexture(new Rect(1450, 0, 150, 150), m_iconBorder);
            GUI.DrawTexture(new Rect(1450, 0, 150, 150), m_cShipIcon);
            
            GUI.DrawTexture(new Rect(1100, 0, 350, 50), m_healthBackground);
            GUI.DrawTexture(new Rect(1205, 80, 10, 50), m_barEnd);
            GUI.DrawTexture(new Rect(1215, 80, 200, 50), m_barMid);
            GUI.DrawTexture(new Rect(1425, 80, -10, 50), m_barEnd);
        }
        else
        {
            //New New CShip Health
            float healthPercent = m_cShipHPCache.GetHPPercentage();
            healthPercent = Mathf.Max(0, healthPercent);
            float shieldPercent = m_cShipHPCache.GetShieldPercentage();
            shieldPercent = Mathf.Max(0, shieldPercent);
            
            GUI.DrawTexture(new Rect(1450, 0, 150, 150), m_iconBorder);
            GUI.DrawTexture(new Rect(1450, 0, 150, 150), m_cShipIcon);
            
            GUI.DrawTexture(new Rect(1100, 0, 350, 50), m_healthBackground);
            GUI.DrawTextureWithTexCoords(new Rect(1450, 0, -350 * healthPercent, 50), m_healthBar, new Rect(0, 0, healthPercent, 1));
            GUI.DrawTextureWithTexCoords(new Rect(1450, 0, -350 * shieldPercent, 50), m_shieldBar, new Rect(0, 0, shieldPercent, 1));
            
            //Show CShip moolah
            GUI.DrawTexture(new Rect(1205, 80, 10, 50), m_barEnd);
            GUI.DrawTexture(new Rect(1215, 80, 200, 50), m_barMid);
            GUI.DrawTexture(new Rect(1425, 80, -10, 50), m_barEnd);
            GUI.Label(new Rect(1225, 85, 180, 44), "$ " + m_cShipHPCache.GetComponent<CapitalShipScript>().GetBankedCash(), "No Box");
        }
        
        if(m_shouldShowCShipUnderFire)
        {
            GUI.Label (new Rect(1205, 130, 220, 44), "Capital ship under attack!", "Shared");
        }
        
        if (m_playerHasDied && !m_isOnCShipDeathSequence)
        {
            GUI.DrawTexture(new Rect(650, 100, 300, 250), m_menuBackground);
            GUI.Label(new Rect(700, 130, 200, 80), "You have been destroyed", "Big No Box");
            
            if (m_noRespawnCash)
            {
                GUI.Label(new Rect(675, 200, 250, 80), "Not enough banked cash to respawn! You need $500.", "No Box");
            }
            else
            {
                GUI.Label(new Rect(700, 200, 200, 80), "Respawning...", "No Box");
            }
        }
    }
    
    void DrawCursor()
    {
        if (m_playerHPCache != null)
        {
            //Cursor
            Matrix4x4 oldMat = GUI.matrix;
            GUI.matrix = Matrix4x4.identity;
            Vector3 mousePos = Input.mousePosition;
            
            
        }
    }
}
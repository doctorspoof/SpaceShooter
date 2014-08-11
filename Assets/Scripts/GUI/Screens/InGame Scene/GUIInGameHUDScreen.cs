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
    
    // Internal members
    
    
    // Cached members
    GameStateController m_gscCache;
    HealthScript m_playerHPCache;
    HealthScript m_cShipHPCache;
    
    /* Unity Functions */
    void Start () 
    {
        m_priorityValue = 1;
        m_gscCache = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameStateController>();
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
    }
}

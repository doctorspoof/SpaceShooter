using UnityEngine;
using System.Collections;

public class GUILossSplashScreen : BaseGUIScreen 
{
    /* Serializable Members */
    [SerializeField]    Texture     m_menuBackground;
    
    /* Internal Members */
    
    
    /* Unity Functions */
    void Start () 
    {
        m_priorityValue = 5;
    }
    
    public override void ManualGUICall (bool shouldRecieveInput)
    {
        GUI.DrawTexture(new Rect(600, 100, 400, 400), m_menuBackground);
        GUI.Label(new Rect(700, 130, 200, 80), "Defeat!", "Big No Box");
        GUI.Label(new Rect(700, 200, 200, 80), "The capital ship was destroyed", "No Box");
        
        Time.timeScale = 0.0f;
        //TODO: Uncomment this when split
        //int seconds = (int)m_gameTimer;
        //string displayedTime2 = string.Format("{0:00}:{1:00}", (seconds / 60) % 60, seconds % 60);
        //GUI.Label(new Rect(700, 300, 200, 80), "Final time: " + displayedTime2, m_nonBoxStyle);
        
        if (GUI.Button(new Rect(750, 400, 100, 80), "Restart", "Shared") && shouldRecieveInput)
        {
            Time.timeScale = 1.0f;
            Network.Disconnect();
            Application.LoadLevel(0);
        }
    }
}

using UnityEngine;
using System.Collections;
using System.Text.RegularExpressions;

public class GUIMainMenuScreen : BaseGUIScreen 
{
    [SerializeField]
    Texture m_menuBackground;

    string m_username;
    
    // Cached stuff
    GameStateController m_gscCache;

    /* Unity Functions */
    void Start ()
    {
        m_priorityValue = 1;
 
        m_username = PlayerPrefs.GetString("LastUsername", "Name");       
        m_gscCache = GameStateController.Instance();
    }
    
    /* Custom Functions */
    public override void ManualGUICall(bool shouldRecieveInput)
    {
        GUI.DrawTexture(new Rect(222, 130, 290, 620), m_menuBackground);
        GUI.Label(new Rect(225, 131, 285, 100), "Please enter a name and select an option:", GUI.skin.GetStyle("Shared"));
        
        if(shouldRecieveInput)
        {
            m_username = GUI.TextField(new Rect(225, 228, 285, 50), m_username, 19, "Shared");
            m_username = Regex.Replace(m_username, @"[^a-zA-Z0-9 ]", "");
        }
        else
            GUI.Label (new Rect(225, 228, 285, 50), m_username, "Shared");
            
        if(shouldRecieveInput)
        {
            if (GUI.Button(new Rect(225, 400, 285, 50), "HOST", "Shared"))
            {
                HostButtonActivate();
            }
        }
        else
        {
            GUI.Label (new Rect(225, 400, 285, 50), "HOST", "Shared");
        }
        
        if(shouldRecieveInput)
        {
            if (GUI.Button(new Rect(225, 450, 285, 50), "JOIN", "Shared"))
            {
                JoinButtonActivate();
            }
        }
        else
        {
            GUI.Label (new Rect(225, 450, 285, 50), "JOIN", "Shared");
        }
        
        if(shouldRecieveInput)
        {
            if (GUI.Button(new Rect(225, 500, 285, 50), "OPTIONS", "Shared"))
            {
                m_gscCache.SwitchToOptions();
            }
        }
        else
        {
            GUI.Label (new Rect(225, 500, 285, 50), "OPTIONS", "Shared");
        }
        
        if(shouldRecieveInput)
        {
            if (GUI.Button(new Rect(225, 698, 285, 50), "QUIT", "Shared"))
            {
                Application.Quit();
            }
        }
        else
        {
            GUI.Label (new Rect(225, 698, 285, 50), "QUIT", "Shared");
        }
    }
    
    // Response functions
    void HostButtonActivate()
    {
        if (m_username != "Name" && m_username != "")
        {
            m_gscCache.PlayerRequestsToHostGame(m_username);
            Time.timeScale = 1.0f;
            PlayerPrefs.SetString("LastUsername", m_username);
        }
    }
    void JoinButtonActivate()
    {
        if (m_username != "Name" && m_username != "")
        {
            m_gscCache.SwitchToIPInput();
            GetComponent<GUIMainMenuMaster>().SetUsername(m_username);
            PlayerPrefs.SetString("LastUsername", m_username);
        }
    }
}

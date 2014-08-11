using UnityEngine;
using System.Collections;

public class GUIIPScreen : BaseGUIScreen 
{
    /* Serialized Textures */
    [SerializeField]        Texture m_menuBackground;
    
    /* Internal Members */
    string m_IPField;
    string m_username;

    /* Cached stuff */
    GameStateController m_gscCache;

	/* Unity Functions */
	void Start () 
    {
	    m_priorityValue = 1;
        m_IPField = PlayerPrefs.GetString("LastIP");
        m_username = PlayerPrefs.GetString("LastUsername");
        m_gscCache = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameStateController>();
	}
    
    /* Custom Functions */
    public override void ManualGUICall (bool shouldRecieveInput)
    {
        GUI.DrawTexture(new Rect(222, 130, 290, 620), m_menuBackground);
        
        GUI.Label(new Rect(222, 331, 290, 50), "IP Address:", "Big No Box");
        m_IPField = GUI.TextField(new Rect(222, 380, 290, 50), m_IPField, "Shared");
        
        if ((GUI.Button(new Rect(222, 600, 290, 100), "CONNECT", "Shared") || Event.current.keyCode == KeyCode.KeypadEnter || Event.current.keyCode == KeyCode.Return) && shouldRecieveInput)
        {
            //if(username != "Name")
            ClientConnectJoinActivate();
        }
        
        if(shouldRecieveInput)
        {
            if (GUI.Button(new Rect(222, 698, 290, 50), "BACK", "Shared"))
            {
                ClientConnectBackActivate();
            }
        }
        else
        {
            GUI.Label (new Rect(222, 698, 290, 50), "BACK", "Shared");
        }
    }
    
    void ClientConnectJoinActivate()
    {
        m_gscCache.GetComponent<GameStateController>().PlayerRequestsToJoinGame(m_IPField, m_username, 6677);
        PlayerPrefs.SetString("LastIP", m_IPField);
        Time.timeScale = 1.0f;
    }
    void ClientConnectBackActivate()
    {
        m_gscCache.GetComponent<GameStateController>().BackToMenu();
    }
}

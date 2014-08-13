using UnityEngine;
using System.Collections;

public class GUIHostConnectedScreen : BaseGUIScreen 
{
    /* Serializable Members */
    [SerializeField]        Texture m_menuBackground;

    // Internal members
    bool m_shouldStartSpec = false;

    // Cached members
    GameStateController m_gscCache;
    
    /* Unity Functions */
    void Start () 
    {
        m_priorityValue = 1;
        m_gscCache = GameStateController.Instance();
    }
    
    /* Custom Functions */
    public override void ManualGUICall (bool shouldRecieveInput)
    {
        GUI.DrawTexture(new Rect(222, 130, 290, 620), m_menuBackground);
        
        GUI.Label(new Rect(225, 131, 285, 50), "Connected Players:", "Big No Box");
        System.Collections.Generic.List<Player> players = m_gscCache.GetConnectedPlayers();
        for (int i = 0; i < players.Count; i++)
        {
            GUI.Label(new Rect(225, 188 + (i * 40), 285, 40), players[i].m_name, "No Box");
        }
        
        if (GUI.Button(new Rect(225, 600, 285, 100), "START", "Shared"))
        {
            HostMenuStartButtonActivate();
        }
        
        if (!m_shouldStartSpec)
        {
            if (GUI.Button(new Rect(510, 600, 140, 100), "Spectator mode", "Shared"))
            {
                m_shouldStartSpec = true;
            }
        }
        else
        {
            if (GUI.Button(new Rect(510, 600, 140, 100), "Spectator mode", "Highlight"))
            {
                m_shouldStartSpec = false;
            }
        }
        
        if (GUI.Button(new Rect(225, 698, 285, 50), "BACK", "Shared"))
        {
            HostMenuBackActivate();
        }
    }
    
    void HostMenuStartButtonActivate()
    {
        m_gscCache.StartGameFromMenu(m_shouldStartSpec);
    }
    void HostMenuBackActivate()
    {
        m_gscCache.SwitchToMainMenu();
        m_gscCache.WipeConnectionInfo();
        Network.Disconnect();
        Debug.Log("Closed Server.");
    }
}

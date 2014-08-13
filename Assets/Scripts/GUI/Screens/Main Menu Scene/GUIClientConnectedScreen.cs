using UnityEngine;
using System.Collections;

public class GUIClientConnectedScreen : BaseGUIScreen 
{
    /* Serializable Members */
    [SerializeField]        Texture m_menuBackground;

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
        
        if (GUI.Button(new Rect(225, 698, 285, 50), "BACK", "Shared"))
        {
            ClientConnectingBackActivate();
        }
    }
    
    void ClientConnectingBackActivate()
    {
        m_gscCache.SwitchToMainMenu();
        m_gscCache.WipeConnectionInfo();
        Network.Disconnect();
    }
}

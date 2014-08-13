using UnityEngine;
using System.Collections;

public class GUIConnFailedScreen : BaseGUIScreen 
{
    /* Serialized Textures */
    [SerializeField] Texture m_menuBackground;
    
    // Cached
    GameStateController m_gscCache;

	/* Unity Functions */
	void Start () 
    {
        m_priorityValue = 2;
        m_gscCache = GameStateController.Instance();
	}
    
    /* Custom Functions */
    public override void ManualGUICall (bool shouldRecieveInput)
    {
        GUI.DrawTexture(new Rect(222, 130, 290, 620), m_menuBackground);
        
        GUI.Label(new Rect(222, 331, 290, 50), "Nickname mismatch!", "Big No Box");
        
        if (GUI.Button(new Rect(225, 698, 285, 50), "BACK", "Shared"))
        {
            m_gscCache.SwitchToMainMenu();
        }
    }
}

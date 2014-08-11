using UnityEngine;
using System.Collections;

public class GUIEscapeMenuScreen : BaseGUIScreen 
{
    /* Serializable Members */
    [SerializeField]    Texture     m_menuBackground;
    
    /* Internal Members */
    
    /* Cached Members */
    GameStateController m_gscCache = null;

	/* Unity Functions */
	void Start () 
    {
	    m_priorityValue = 4;
	}
    
    /* Custom Functions */
    public override void ManualGUICall (bool shouldRecieveInput)
    {
        GUI.DrawTexture(new Rect(305, 130, 290, 620), m_menuBackground);
        
        if (GUI.Button(new Rect(308, 430, 284, 130), "QUIT TO MENU", "Shared"))
        {
            m_gscCache.WipeConnectionInfo();
            Application.LoadLevel(0);
        }
        
        if (GUI.Button(new Rect(308, 560, 284, 130), "QUIT TO DESKTOP", "Shared"))
        {
            Application.Quit();
        }
        
        if (GUI.Button(new Rect(308, 300, 284, 130), "CONTINUE", "Shared"))
        {
            //TODO: Change this to alert GSC to close menu
            //ToggleMenuState();
        }
    }
}

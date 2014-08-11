using UnityEngine;
using System.Collections;

public class GUIConnectingScreen : BaseGUIScreen 
{

    // Cached members
    GameStateController m_gscCache;

	/* Unity Functions */
	void Start () 
    {
        m_priorityValue = 2;
        m_gscCache = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameStateController>();
	}
    
    /* Custom Functions */
    public override void ManualGUICall (bool shouldRecieveInput)
    {
        GUI.Label(new Rect(700, 300, 200, 80), "Connecting...", "Shared");
        
        if (GUI.Button(new Rect(222, 698, 290, 50), "", "label") && shouldRecieveInput)
        {
            Debug.Log ("Activating cancel button");
            m_gscCache.GetComponent<GameStateController>().BackToMenu();
        }
    }
}

using UnityEngine;
using System.Collections;

public class GUIDisconnectedScreen : BaseGUIScreen 
{
    /* Serializable Members */
    [SerializeField]    Texture     m_menuBackground;
    
    /* Internal Members */
    
    
    /* Unity Functions */
    void Start () 
    {
        m_priorityValue = 5;
    }
    
    /* Custom Functions */
    public override void ManualGUICall (bool shouldRecieveInput)
    {
        GUI.DrawTexture(new Rect(600, 350, 400, 200), m_menuBackground);
        GUI.Label(new Rect(650, 400, 300, 50), "The host has disconnected.", "No Box");
        if (GUI.Button(new Rect(750, 500, 100, 50), "Return to menu", "Shared"))
        {
            Application.LoadLevel(0);
        }
    }
}

using UnityEngine;
using System.Collections;

public class GUILoadingScreen : BaseGUIScreen 
{
    /* Serialized Textures */
    [SerializeField]        Texture m_loadingScreenTexture;
    
    /* Internal Members */
    
    /* Cached stuff */
    GameStateController m_gscCache;
    
    /* Unity Functions */
    void Start () 
    {
        m_priorityValue = 3;
        m_gscCache = GameStateController.Instance();
    }
    
    /* Custom Functions */
    public override void ManualGUICall (bool shouldRecieveInput)
    {
        GUI.DrawTexture(new Rect(0, 0, 1600, 900), m_loadingScreenTexture);
    }
}

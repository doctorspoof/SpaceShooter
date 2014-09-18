using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GUICShipDockTemp : BaseGUIScreen 
{
    // Caches
    PlayerControlScript m_playerCache;
    GameStateController m_gscCache;
    
	/* Unity Functions */
	void Start () 
    {
	    m_priorityValue = 3;
        m_gscCache = GameStateController.Instance();
	}
	
	/* Custom Functions */
    #region DrawFunctions
    public override void ManualGUICall (bool shouldRecieveInput)
    {
        //Leave button
        if (shouldRecieveInput && GUI.Button(new Rect(512, 687, 176, 110), "", "label"))
        {
            //This shouldn't be used anymore, instead GSC should be told when the player is docked or not docked, and that info passed back to the GUI
            m_gscCache.SwitchToInGame();
            
            Screen.showCursor = false;
            m_playerCache.TellPlayerStopDocking();
        }
    }
    #endregion
    
}

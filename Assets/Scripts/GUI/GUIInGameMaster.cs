using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GUIInGameMaster : GUIBaseMaster 
{
    // Serializable Members
    [SerializeField]        GUISkin m_mainMenuSkin;

	/* Unity Functions */
	void Start () 
    {
        m_listOfScreensToDraw = new List<BaseGUIScreen>();
        List<BaseGUIScreen> temp = new List<BaseGUIScreen>();
        temp.Add(GetComponent<GUIInGameHUDScreen>());
        UpdateScreensToDraw(temp);
	}
    
    void OnGUI ()
    {
        float rx = Screen.width / m_nativeWidth;
        float ry = Screen.height / m_nativeHeight;
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(rx, ry, 1));
        
        if(GUI.skin != m_mainMenuSkin)
            GUI.skin = m_mainMenuSkin;
        
        //Draw all the screens currently in the list
        for(int i = 0; i < m_listOfScreensToDraw.Count; i++)
        {
            bool isHighest = (m_listOfScreensToDraw[i].m_priorityValue == m_highestPriority);
            m_listOfScreensToDraw[i].ManualGUICall(isHighest);
        }
    }
    
    // Custom Functions
    public override void ChangeGameState (GameState newState)
    {
        switch (newState)
        {
            case GameState.InGame:
            {
                List<BaseGUIScreen> temp = new List<BaseGUIScreen>();
                temp.Add(GetComponent<GUIInGameHUDScreen>());
                temp.Add(GetComponent<GUIMapOverlayScreen>());
                UpdateScreensToDraw(temp);
                break;
            }
            case GameState.InGameConnectionLost:
            {
                List<BaseGUIScreen> temp = new List<BaseGUIScreen>(m_listOfScreensToDraw);
                temp.Add(GetComponent<GUIDisconnectedScreen>());
                UpdateScreensToDraw(temp);
                break;
            }
            case GameState.InGameCShipDock:
            {
                List<BaseGUIScreen> temp = new List<BaseGUIScreen>();
                temp.Add(GetComponent<GUIInGameHUDScreen>());
                temp.Add(GetComponent<GUICShipDockScreen>());
                UpdateScreensToDraw(temp);
                break;
            }
            case GameState.InGameShopDock:
            {
                List<BaseGUIScreen> temp = new List<BaseGUIScreen>();
                temp.Add(GetComponent<GUIInGameHUDScreen>());
                temp.Add(GetComponent<GUIShopDockScreen>());
                UpdateScreensToDraw(temp);
                break;
            }
            default:
            {
                Debug.Log ("Passed game state is not handled by this GUIMaster!");
                break;
            }
        }
    }
}

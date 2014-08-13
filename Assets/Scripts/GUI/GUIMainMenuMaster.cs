using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GUIMainMenuMaster : GUIBaseMaster 
{
    /* Serialized Members */
    [SerializeField]        GUISkin m_mainMenuSkin;

	/* Unity Functions */
	void Start () 
    {
	    m_listOfScreensToDraw = new List<BaseGUIScreen>();
        List<BaseGUIScreen> temp = new List<BaseGUIScreen>();
        temp.Add(GetComponent<GUIMainMenuScreen>());
        UpdateScreensToDraw(temp);
	}
    
    void OnGUI()
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
    
    /* Custom Functions */
    public override void ChangeGameState(GameState newState)
    {
        switch (newState)
        {
            case GameState.MainMenu:
            {
                m_listOfScreensToDraw.Clear();
                m_listOfScreensToDraw.Add(GetComponent<GUIMainMenuScreen>());
                m_highestPriority = 1;
                break;
            }   
            case GameState.OptionMenu:
            {
                List<BaseGUIScreen> temp = new List<BaseGUIScreen>(m_listOfScreensToDraw);
                temp.Add(GetComponent<GUIOptionsScreen>());
                UpdateScreensToDraw(temp);
                break;
            }
            case GameState.ClientInputIP:
            {
                m_listOfScreensToDraw.Clear();
                m_listOfScreensToDraw.Add(GetComponent<GUIIPScreen>());
                m_highestPriority = 1;
                break;
            }
            case GameState.AttemptingConnect:
            {
                List<BaseGUIScreen> temp = new List<BaseGUIScreen>(m_listOfScreensToDraw);
                temp.Add(GetComponent<GUIConnectingScreen>());
                UpdateScreensToDraw(temp);
                break;
            }
            case GameState.FailedConnectName:
            {
                List<BaseGUIScreen> temp = new List<BaseGUIScreen>(m_listOfScreensToDraw);
                temp.Remove(GetComponent<GUIConnectingScreen>());
                temp.Add(GetComponent<GUIConnFailedScreen>());
                UpdateScreensToDraw(temp);
                break;
            }
            case GameState.ClientMenu:
            {
                m_listOfScreensToDraw.Clear();
                m_listOfScreensToDraw.Add(GetComponent<GUIClientConnectedScreen>());
                m_highestPriority = 1;
                break;
            }
            case GameState.HostMenu:
            {
                m_listOfScreensToDraw.Clear();
                m_listOfScreensToDraw.Add(GetComponent<GUIHostConnectedScreen>());
                m_highestPriority = 1;
                break;
            }
            case GameState.LoadingScreen:
            {
                m_listOfScreensToDraw.Clear();
                m_listOfScreensToDraw.Add (GetComponent<GUILoadingScreen>());
                m_highestPriority = 3;
                break;
            }
            default:
            {
                Debug.Log ("Passed game state is not handled by this GUIMaster!");
                break;
            }
        }
    }
    
    /* Specific Calls/PassThrough */
    public void SetUsername(string name)
    {
        GetComponent<GUIIPScreen>().SetUsername(name);
    }
}

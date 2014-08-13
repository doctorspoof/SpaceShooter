using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum CShipScreen
{
    PlayerPanel = 1,
    StatusPanel = 2,
    ObjectivePanel = 3,
    DualPanel = 5,
    LeftPanelActive = 6,
    RightPanelActive = 7,
    PanelsAnimating = 8
}

enum ItemOwner
{
    PlayerInventory = 1,
    NetworkInventory = 2,
    PlayerEquipment = 3,
    CShipEquipment = 4
}

public abstract class GUIBaseMaster : MonoBehaviour 
{
    // Internal Members
    [HideInInspector]       public List<BaseGUIScreen> m_listOfScreensToDraw;
    [HideInInspector]       public int m_highestPriority = -1;
    [HideInInspector]       public float m_nativeWidth = 1600;
    [HideInInspector]       public float m_nativeHeight = 900;

	// Custom Functions
    public abstract void ChangeGameState(GameState newState);
    
    public void UpdateScreensToDraw(List<BaseGUIScreen> unorderedScreens)
    {
        //We need to order the list by priority value
        List<BaseGUIScreen> temp = new List<BaseGUIScreen>(unorderedScreens);
        m_listOfScreensToDraw.Clear();
        
        BaseGUIScreen lowestScreen = null;
        int lowestI = -1;
        
        for(int j = 0; j < unorderedScreens.Count; j++)
        {
            for(int i = 0; i < temp.Count; i++)
            {
                if(lowestScreen == null || temp[i].m_priorityValue < lowestScreen.m_priorityValue)
                {
                    lowestScreen = temp[i];
                    lowestI = i;
                }
            }
            
            //Should now have current lowest screen
            temp.RemoveAt(lowestI);
            m_listOfScreensToDraw.Add(lowestScreen);
            
            lowestScreen = null;
            lowestI = -1;
        }
        
        //Get highest value for posterity
        int highestPri = -1;
        foreach(BaseGUIScreen screen in m_listOfScreensToDraw)
        {
            if(screen.m_priorityValue > highestPri)
                highestPri = screen.m_priorityValue;
        }
        m_highestPriority = highestPri;
    }
}

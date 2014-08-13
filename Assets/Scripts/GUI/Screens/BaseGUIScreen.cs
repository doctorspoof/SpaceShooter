using UnityEngine;
using System.Collections;

public abstract class BaseGUIScreen : MonoBehaviour 
{
    [HideInInspector]
    public int m_priorityValue;
    
    public abstract void ManualGUICall(bool shouldRecieveInput);
}

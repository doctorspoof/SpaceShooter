using UnityEngine;
using System.Collections;

public class OrbitingObject : MonoBehaviour 
{
    [SerializeField]    Texture     m_miniMapBlip;

    #region Getters/Setters
    public Texture GetPlanetMinimapBlip()
    {
        return m_miniMapBlip;
    }
    #endregion

	// Use this for initialization
	void Start () 
    {
	
	}
	
	// Update is called once per frame
	void Update () 
    {
	
	}
}

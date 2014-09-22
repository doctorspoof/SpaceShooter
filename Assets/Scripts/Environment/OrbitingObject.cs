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
    
    public void UpdateObjectScale(Vector3 scale)
    {
        if(scale.x == scale.y && scale.x == scale.z)
        {
            networkView.RPC ("PropagateNewScaleF", RPCMode.Others, scale.x);
        }
        else
        {
            networkView.RPC ("PropagateNewScaleV", RPCMode.Others, scale);
        }
        
        transform.localScale = scale;
    }
    [RPC] void PropagateNewScaleV(Vector3 scale)
    {
        transform.localScale = scale;
    }
    [RPC] void PropagateNewScaleF(float scale)
    {
        transform.localScale = new Vector3(scale, scale, scale);
    }
    public void UpdateObjectParentScale(Vector3 scale, Transform parent)
    {
        if(scale.x == scale.y && scale.x == scale.z)
        {
            networkView.RPC ("PropagateNewParentScaleF", RPCMode.Others, scale.x, parent.networkView.viewID);
        }
        else
        {
            networkView.RPC ("PropagateNewParentScaleV", RPCMode.Others, scale, parent.networkView.viewID);
        }
        
        transform.localScale = scale;
        transform.parent = parent;
    }
    [RPC] void PropagateNewParentScaleV(Vector3 scale, NetworkViewID id)
    {
        transform.localScale = scale;
        transform.parent = NetworkView.Find(id).transform;
    }
    [RPC] void PropagateNewParentScaleF(float scale, NetworkViewID id)
    {
        transform.localScale = new Vector3(scale, scale, scale);
        transform.parent = NetworkView.Find(id).transform;
    }
}

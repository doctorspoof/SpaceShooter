using UnityEngine;
using System.Collections;

public class PlanetRing : MonoBehaviour {

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}
    
    public void SetParentAndScale(Transform parent, Vector3 scale)
    {
        NetworkViewID id = parent.networkView.viewID;
        
        if(scale.x == scale.y && scale.x == scale.z)
        {
            networkView.RPC("PropagateNewParentScaleF", RPCMode.All, scale.x, id);
        }
        else
        {
            networkView.RPC("PropagateNewParentScaleV", RPCMode.All, scale, id);
        }
    }
    
    [RPC] void PropagateNewParentScaleV(Vector3 scale, NetworkViewID id)
    {
        transform.parent = NetworkView.Find(id).transform;
        transform.localPosition = Vector3.zero;
        transform.localScale = scale;
    }
    [RPC] void PropagateNewParentScaleF(float scale, NetworkViewID id)
    {
        transform.parent = NetworkView.Find(id).transform;
        transform.localPosition = Vector3.zero;
        transform.localScale = new Vector3(scale, scale, scale);
    }
}

using UnityEngine;
using System.Collections;

public class CapitalShipFinalExplodeScript : MonoBehaviour {

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	void OnDestroy()
	{
		this.transform.root.GetComponent<CapitalShipScript>().FinalExplodeCompleted();
	}
}

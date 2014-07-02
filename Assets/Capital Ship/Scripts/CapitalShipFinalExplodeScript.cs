using UnityEngine;
using System.Collections;

public class CapitalShipFinalExplodeScript : MonoBehaviour {

	// Use this for initialization
	void Start () 
	{
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	void OnDestroy()
	{
		//Tell gui to dispaly loss popup
		GameObject.FindGameObjectWithTag("GUIManager").GetComponent<GUIManager>().ShowLossSplash();
		Destroy (this.gameObject);
	}
}

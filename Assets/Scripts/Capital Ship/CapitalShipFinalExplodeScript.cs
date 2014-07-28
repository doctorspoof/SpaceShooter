using UnityEngine;
using System.Collections;

public class CapitalShipFinalExplodeScript : MonoBehaviour 
{
	void OnDestroy()
	{
		//Tell gui to display loss popup
		GameObject.FindGameObjectWithTag("GUIManager").GetComponent<GUIManager>().ShowLossSplash();
		Destroy (this.gameObject);
	}
}

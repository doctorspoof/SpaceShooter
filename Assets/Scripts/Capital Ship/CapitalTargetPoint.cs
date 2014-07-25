using UnityEngine;
using System.Collections;

public class CapitalTargetPoint : MonoBehaviour 
{
	//[SerializeField]
	//LayerMask m_CapitalShipLayer;

	[SerializeField]
	GameObject m_GameController;

	// Use this for initialization
	void Start () 
	{
	
	}
	
	// Update is called once per frame
	void Update () 
	{

	}

	void OnTriggerEnter(Collider other)
	{
		if(Network.isServer)
		{
			if(other.tag == "Capital")
			{
				//TODO: Implement win state, activate it here
				Debug.Log ("Capital ship entered exit zone, game over!");
				m_GameController.GetComponent<GameStateController>().TellEveryoneCapitalShipArrivesAtVictoryPoint();
			}
		}
	}
}

using UnityEngine;
using System.Collections;

public class CapitalTargetPoint : MonoBehaviour 
{
	/* Serializable members */
	[SerializeField]    GameObject m_GameController;

    /* Unity functions */
	void OnTriggerEnter(Collider other)
	{
		if(Network.isServer)
		{
			if(other.tag == "Capital")
			{
				m_GameController.GetComponent<GameStateController>().TellEveryoneCapitalShipArrivesAtVictoryPoint();
			}
		}
	}
}

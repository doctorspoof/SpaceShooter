using UnityEngine;
using System.Collections;
    
public class CapitalShipTarget : MonoBehaviour 
{
	void OnTriggerEnter(Collider other)
    {
        if(other.tag == "Capital")
        {
            // Alert GSC that cship has reached the end
            GameStateController.Instance().TellEveryoneCapitalShipArrivesAtVictoryPoint();
        }
    }
}

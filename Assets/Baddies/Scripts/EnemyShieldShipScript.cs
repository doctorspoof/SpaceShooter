using UnityEngine;
using System.Collections;

public class EnemyShieldShipScript : MonoBehaviour 
{

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
		if(!this.GetComponent<EnemyScript>().GetShield().GetComponent<EnemySupportShieldScript>().OnTriggerEnter(other))
		{
			//Call trigger enter on enemyScript
		}
	}
}

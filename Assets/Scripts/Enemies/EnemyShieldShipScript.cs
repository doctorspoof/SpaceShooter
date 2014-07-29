using UnityEngine;
using System.Collections;

public class EnemyShieldShipScript : MonoBehaviour 
{
    EnemySupportShield supportShield;

    void Start()
    {
        supportShield = this.GetComponent<EnemyScript>().GetShield().GetComponent<EnemySupportShield>();
    }

	void OnTriggerEnter(Collider other)
	{
		if(other.gameObject.layer == Layers.playerBullet || other.gameObject.layer == Layers.capitalBullet)
		{
            if (!supportShield.OnTriggerEnter(other))
			{
				//Call trigger enter on enemyScript
			}
		}
	}
}

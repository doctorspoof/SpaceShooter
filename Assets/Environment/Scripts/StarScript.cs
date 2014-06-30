using UnityEngine;
using System.Collections;

public class StarScript : MonoBehaviour 
{
	[SerializeField] int m_damagePerTick = 1;
	
	[SerializeField] float m_tickTime = 0.05f;
	
	float m_damageDelay = 0.0f;
	
	
	void OnTriggerStay(Collider other)
	{
		switch (other.attachedRigidbody.gameObject.layer)
		{
			case Layers.capital:
			case Layers.enemy:
			case Layers.asteroid:
				m_damageDelay += Time.deltaTime;
				if(m_damageDelay >= m_tickTime)
				{
					other.gameObject.GetComponent<HealthScript>().DamageMobHullDirectly(m_damagePerTick);
					m_damageDelay = 0.0f;
				}
				
				break;
		}
	}
}

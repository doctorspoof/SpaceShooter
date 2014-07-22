using UnityEngine;
using System.Collections;

public class StarScript : MonoBehaviour 
{
	[SerializeField, Range (0, 100)] int m_damagePerTick = 1;
	
	[SerializeField, Range (0f, 100f)] float m_tickTime = 0.05f;
	
	float m_damageDelay = 0.0f;
	
	
	void OnTriggerStay(Collider other)
	{
		if (other.attachedRigidbody && other.attachedRigidbody.gameObject)
		{
			switch (other.attachedRigidbody.gameObject.layer)
			{
				case Layers.player:
				case Layers.capital:
				case Layers.enemy:
				case Layers.enemyCollide:
                case Layers.enemyDestructibleBullet:
				case Layers.asteroid:
					m_damageDelay += Time.deltaTime;
					if(m_damageDelay >= m_tickTime)
					{
						HealthScript script = other.attachedRigidbody.gameObject.GetComponent<HealthScript>();

						if (script)
						{
							script.DamageMobHullDirectly (m_damagePerTick);
						}

						else
						{
							Debug.LogError ("Unable to find HealthScript on " + other.attachedRigidbody.name);
						}

						m_damageDelay = 0.0f;
					}
					
					break;
			}
		}
	}
}

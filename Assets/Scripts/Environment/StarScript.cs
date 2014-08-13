using UnityEngine;


/// <summary>
/// Used to damage destroyable objects. In the future it should include additional functionality like Black Hole features.
/// </summary>
public sealed class StarScript : MonoBehaviour 
{
    //////////////////////////////////
    /// Unity modifiable variables ///
    //////////////////////////////////



    // Damage attributes
	[SerializeField, Range (0, 100)] int m_damagePerTick = 1;       // How much damage to apply per tick
	[SerializeField, Range (0f, 100f)] float m_tickTime = 0.05f;    // The time in seconds in which to apply damage
	


    /////////////////////
    /// Internal data ///
    /////////////////////



	float m_damageDelay = 0.0f; // The current time
	
	

    //////////////////////////
    /// Behavior functions ///
    //////////////////////////



	void OnTriggerStay(Collider other)
	{
		if (other.attachedRigidbody != null && other.attachedRigidbody.gameObject != null)
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

						if (script != null)
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

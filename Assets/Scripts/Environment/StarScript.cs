using UnityEngine;


/// <summary>
/// Used to damage destroyable objects. In the future it should include additional functionality like Black Hole features.
/// </summary>
public sealed class StarScript : MonoBehaviour 
{
    //////////////////////////////////
    /// Unity modifiable variables ///
    //////////////////////////////////

    // Procedural Generation Info
    [SerializeField, Range (3000f, 60000f)]     float   m_starTemperature = 8000f;  // The surface temperature of the star, in kelvin
    [SerializeField, Range (1.0f, 13.0f)]       float   m_starAge = 4.6f;           // Age of the star, in billions of years
    [SerializeField, Range (0.1f, 150.0f)]      float   m_starMass = 1.0f;          // Mass of the star, in solar masses

    // Damage attributes
	[SerializeField, Range (0, 100)]            int     m_damagePerTick = 1;        // How much damage to apply per tick
	[SerializeField, Range (0f, 100f)]          float   m_tickTime = 0.05f;         // The time in seconds in which to apply damage
	
    [SerializeField]                            Texture m_minimapBlip;

    /////////////////////
    /// Internal data ///
    /////////////////////



	float m_damageDelay = 0.0f; // The current time
	
    #region Getters/Setters
    public Texture GetMinimapBlip()
    {
        return m_minimapBlip;
    }
    public float GetStarTemperature()
    {
        return m_starTemperature;
    }
    public float GetStarAge()
    {
        return m_starAge;
    }
    public float GetStarMass()
    {
        return m_starMass;
    }
    #endregion

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

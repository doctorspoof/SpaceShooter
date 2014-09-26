using UnityEngine;



/// <summary>
/// LevelBoundary is the script used to destory objects which exit the trigger collider according to the conditions set in the editor. Currently only
/// asteroids are destroyed.
/// </summary>
[RequireComponent (typeof (Collider))]
public sealed class LevelBoundary : MonoBehaviour 
{
    #region Unity modifiable variables

    // Size
    [SerializeField] Vector3 m_boundaryScale = new Vector3 (1f, 1f, 1f);    // How large the localScale Vector3 should be for the boundary

    // Destructible objects
    [SerializeField] bool m_destroyAsteroids = true;                        // Whether asteroids should be destroyed
	[SerializeField] bool m_destroyPlayers = false;                         // Whether players should be destroyed
	[SerializeField] bool m_destroyEnemies = false;                         // Whether the enemies should be destroyed

    #endregion


    #region Getters & Setters

    public Vector3 GetBoundaryScale()
    {
        return m_boundaryScale;
    }


    public void SetBoundaryScale (Vector3 boundaryScale)
    {
        networkView.RPC ("PropagateBoundaryScale", RPCMode.All, boundaryScale);
    }
    [RPC] void PropagateBoundaryScale(Vector3 boundaryScale)
    {
        m_boundaryScale = boundaryScale;
        transform.localScale = m_boundaryScale * 2.0f;
    }

    #endregion


    #region Behavior functions

	// Set collider to trigger
	void Awake()
	{
		collider.isTrigger = true;
		transform.localScale = m_boundaryScale;
	}


	// Destroy exitting objects
	void OnTriggerExit (Collider other)
	{
		if (Network.isServer)
		{
			// Cache the tag for efficiency
			string tag = other.tag;
			
			// All Network.Destroy() targets should go here
			if (m_destroyAsteroids && tag == "Asteroid")
			{
				DestroyByNetwork (other.gameObject);
			}
			
			// All HealthScript destructions should go here
			else if ((m_destroyPlayers && tag == "Player") || (m_destroyEnemies && tag == "Enemy"))
			{
				DestroyByHealth (other.gameObject);
			}
		}
	}

    #endregion


    #region Destroy functions

	/// <summary>
    /// Destroy object using HealthScript.
    /// </summary>
    /// <param name="destroy">GameObject to destroy.</param>
	void DestroyByHealth (GameObject destroy)
	{
		if (destroy != null)
		{
			// Attempt to kill through the use of a HealthScript
			HealthScript health = destroy.GetComponent<HealthScript>();
			if (health != null)
			{
				health.DamageMobHullDirectly (health.GetCurrHP());
			}

			// Fall back to network destruction
			else
			{
				DestroyByNetwork (destroy);
			}
		}
	}


	/// <summary>
    /// Destroy objects using Network.Destroy()
    /// </summary>
    /// <param name="destroy">GameObject to destroy.</param>
	void DestroyByNetwork (GameObject destroy)
	{
		if (destroy != null)
		{
            Debug.Log ("Boundary destroyed: " + destroy.name);
			Network.Destroy (destroy);
		}
	}

    #endregion
}

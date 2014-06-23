using UnityEngine;


/// <summary>
/// LevelBoundary is the script used to destory objects which exit the trigger collider according to the conditions set in the editor. Currently only
/// asteroids are destroyed.
/// </summary>
[RequireComponent (typeof (Collider))]
public sealed class LevelBoundary : MonoBehaviour 
{
	// How large the localScale Vector3 should be for the boundary
	[SerializeField] private Vector3 m_boundaryScale = new Vector3 (1f, 1f, 1f);


	// What should be destroyed
	public bool destroyAsteroids = true;
	public bool destroyPlayers = false;
	public bool destroyEnemies = false;


	// The public property for m_boundaryScale
	public Vector3 boundaryScale
	{
		get { return m_boundaryScale; }
		set
		{
			m_boundaryScale = value;
			transform.localScale = m_boundaryScale;
		}
	}



	// Set collider to trigger
	private void Awake()
	{
		collider.isTrigger = true;
		transform.localScale = m_boundaryScale;
	}


	// Destroy exitting objects
	private void OnTriggerExit (Collider other)
	{
		if (Network.isServer)
		{
			// Cache the tag for efficiency
			string tag = other.tag;
			
			// All Network.Destroy() targets should go here
			if (destroyAsteroids && tag == "Asteroid")
			{
				DestroyByNetwork (other.gameObject);
			}
			
			// All HealthScript destructions should go here
			else if ((destroyPlayers && tag == "Player") || (destroyEnemies && tag == "Enemy"))
			{
				DestroyByHealth (other.gameObject);
			}
		}
	}


	// Destroy object using HealthScript
	private void DestroyByHealth (GameObject destroy)
	{
		if (destroy)
		{
			// Attempt to kill through the use of a HealthScript
			HealthScript health = destroy.GetComponent<HealthScript>();
			if (health)
			{
				health.DamageMobHullDirectly (health.GetCurrHP() + 1);
			}

			// Fall back to network destruction
			else
			{
				DestroyByNetwork (destroy);
			}
		}
	}


	// Destroy objects using Network.Destroy()
	private void DestroyByNetwork (GameObject destroy)
	{
		if (destroy)
		{
			Debug.Log ("Destroying " + destroy.name + " because it reached the level boundary");
			Network.Destroy (destroy);
		}
	}
}

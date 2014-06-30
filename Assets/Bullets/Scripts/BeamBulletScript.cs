﻿using UnityEngine;



public class BeamBulletScript : MonoBehaviour 
{
	/// Unity modifiable values
	// Damage
	[SerializeField] int m_beamDamage; 				// The beam damage dealt per second
	public float m_beamLength;						// How long the beam should be in localScale.y
	
	// Impact attributes
	[SerializeField] float m_impactForce = 2.5f;	// The amount of force to be applied when hitting a target
	
	
	/// Internal data
	float m_overflow = 0f;								// Simply keeps a reference of any damage overflow for the sake of accurate DPS
	
	int m_damageMask;									// A layermask used in raycasting against valid enemy targets
	
	Vector3 m_offset = Vector3.zero;					// The position offset used to make the beam reach from the firer to the target
	
	[HideInInspector] public GameObject firer = null;	// The parent object which fires the beam
	
	
	
	/// Properties, getters and setters
	// Adjusts the local offset
	public void SetOffset (Vector3 offset)
	{
		m_offset = offset;
		transform.localPosition = offset;
	}
	
	
	
	/// Functions
	// Set up the beam
	void Start () 
	{
		LayerMaskSetup();
	}
	
	
	// Update is called once per frame
	void Update() 
	{
		// Increase the overflow by deltaTime to ensure correct damage is being applied
		m_overflow += m_beamDamage * Time.deltaTime;
		
		// Check if the beam is hitting anything
		RaycastHit hit;
		if (Physics.Raycast (firer.transform.position, firer.transform.up, out hit, m_beamLength, m_damageMask))
		{
			// Reset the distance according to the RaycastHit
			ResetOffset (hit.distance);
			
			// Only the host should apply damage and only if damage can be dealt
			if (hit.collider && hit.collider.attachedRigidbody)
			{				
				DamageHit (hit);
			}
		}
		
		// Raycast found nothing
		else
		{
			ResetOffset ();
		}
		
	}
	
	
	// Sets the correct layer for damage according to the beams layer
	void LayerMaskSetup()
	{
		const int 	player = (1 << Layers.player), 
		capital = (1 << Layers.capital), 
		enemy = (1 << Layers.enemy), 
		asteroid = (1 << Layers.asteroid);
		
		switch (gameObject.layer)
		{
		case Layers.playerBullet:
		case Layers.capitalBullet:
			m_damageMask = enemy | asteroid;
			break;
			
		case Layers.enemyBullet:
			m_damageMask = player | capital | asteroid;
			break;
		}
	}
	
	
	// Calcuates the correct localPosition and localScale for the beam based on the distance given
	void ResetOffset (float distance = -1f)
	{
		// Calculate the scale modifier based on the parents scale
		float scaleModifier = firer.transform.localScale.x * firer.transform.localScale.y;
		
		// Avoid divide by zero errors
		if (scaleModifier == 0f)
		{
			scaleModifier = 1f;
		}
		
		float newPositionY = distance >= 0f && distance <= m_beamLength ? distance / scaleModifier : m_beamLength / scaleModifier;
		
		m_offset.y = newPositionY;
		transform.localPosition = m_offset;
		
		// The scale will always be double the position to centre the beam
		Vector3 newScale = new Vector3 (transform.localScale.x, newPositionY * 2f, transform.localScale.z);
		transform.localScale = newScale;
	}
	
	
	// Damage the mob, apply impact force and sync its position over the network if it's an asteroid
	void DamageHit (RaycastHit hit)
	{
		// Colliders may be part of a composite collider so we must use Collider.attachedRigidbody to get the HealthScript component
		Rigidbody mob = hit.collider.attachedRigidbody;
		
		// Push the enemy away from the force of the beam
		mob.AddForceAtPosition (transform.up * m_impactForce * Time.deltaTime, hit.point);

		// Only the host should cause damage
		if (Network.isServer)
		{
			HealthScript health = mob.GetComponent<HealthScript>();
			if (health)
			{
				if (m_overflow > 1f)
				{
					// Ensure the overflow is caught and stored for the next time damage will be dealt
					int damage = (int) m_overflow;
					m_overflow -= damage;
					
					health.DamageMob (damage, firer, gameObject);
				}
			}
			
			else
			{
				Debug.LogError ("Unable to find HealthScript on: " + hit.collider.attachedRigidbody.name);
			}
		}
	}
	
	
	// Used to ensure that the beam is properly parented across all clients
	[RPC] void ParentBeamToFirerOverNetwork(string playerName)
	{
		GameStateController gsc = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameStateController>();
		
		GameObject playerGO = gsc.GetPlayerFromNetworkPlayer(gsc.GetNetworkPlayerFromID(gsc.GetIDFromName(playerName)));
		Debug.Log ("Attaching beam: " + this.name + " to gameObject: " + playerGO.name + ".");
		this.transform.parent = playerGO.transform;
		firer = playerGO;
	}
	
	
	// Used to ensure that the beam is properly parented across all clients
	[RPC] void ParentBeamToCShipOverNetwork(int id)
	{
		GameObject cship = GameObject.FindGameObjectWithTag("Capital");
		GameObject turret = cship.GetComponent<CapitalShipScript>().GetCTurretHolderWithId(id);
		this.transform.parent = turret.transform.GetChild(0);
	}
	
	
	// Inform other clients to parent the beam to the corresponding player
	public void ParentBeamToFirer(string playerName)
	{
		networkView.RPC("ParentBeamToFirerOverNetwork", RPCMode.Others, playerName);
	}
	
	
	// Inform other clients to parent the beam to the corresponding CShip turret
	public void ParentBeamToCShipTower (int turretID)
	{
		networkView.RPC("ParentBeamToCShipOverNetwork", RPCMode.Others, turretID);
	}
}

using UnityEngine;


/// <summary>
/// The class used to create manage beam damage and length. It keeps a reference to whether the laser has hit anything and applies force to targets it hits.
/// </summary>
public sealed class BeamBulletScript : MonoBehaviour 
{
    ///////////////////////////////
	/// Unity modifiable values ///
    ///////////////////////////////



    // Damage
    [SerializeField] int m_beamDamage = 30; 			            // The beam damage dealt per second
    [SerializeField, Range (0f, 100f)] float m_beamLength = 5f;     // How long the beam should be in localScale.y
	[SerializeField] DamageType m_damageType = DamageType.Laser;	// The type of damage this object deals, used for shading


	// Impact attributes
	[SerializeField] float m_impactForce = 2.5f;		            // The amount of force to be applied when hitting a target
	
	

    /////////////////////
	/// Internal data ///
    /////////////////////


    
    int m_damageMask = 0;                       // A layermask used in raycasting against valid enemy targets
    float m_overflow = 0f;						// Simply keeps a reference of any damage overflow for the sake of accurate DPS
	
	Vector3 m_offset = Vector3.zero;			// The position offset used to make the beam reach from the firer to the target
	Vector3 m_raycastOffset = Vector3.zero;		// Used to work around the problem of Z values effecting the raycast

	RaycastHit m_beamHit = new RaycastHit();	// What the raycast has hit, updated every frame
	GameObject m_firer = null;				    // The parent object which fires the beam
	
	
	
    /////////////////////////
	/// Getters & setters ///
    /////////////////////////



	public float GetDamage()
	{
		return m_beamDamage;
	}

    
    public float GetBeamLength()
    {
        return m_beamLength;
    }
    

    public DamageType GetDamageType()
    {
        return m_damageType;
    }


    public void SetDamageType (DamageType damageType)
    {
        m_damageType = damageType;
    }


    public RaycastHit GetBeamHit()
    {
        return m_beamHit;
    }


    public GameObject GetFirer()
    {
        return m_firer;
    }


    public void SetFirer (GameObject firer)
    {
        m_firer = firer;
    }


	/// <summary>
    /// Adjusts the local offset of the beam so that it lines up correctly with the weapon.
    /// </summary>
    /// <param name="offset">The desired offset.</param>
	public void SetOffset (Vector3 offset)
	{
		m_offset = offset;
		m_raycastOffset = offset;
		m_raycastOffset.z = 0f;

		transform.localPosition = offset;
	}
	
	
	
    //////////////////////////
	/// Behavior functions ///
    //////////////////////////


   
	void Awake()
	{
		LayerMaskSetup();
	}
	
	
    /// <summary>
    /// Checks if the beam is hitting any targets, damages targets and adjusts the beam length to reflect the situation.
    /// </summary>
	void Update() 
	{
		if (m_firer != null)
		{
			// Check if the beam is hitting anything
			if (Physics.Raycast (m_firer.transform.position + m_raycastOffset, m_firer.transform.up, out m_beamHit, m_beamLength, m_damageMask))
            {
                // Increase the overflow by deltaTime to ensure correct damage is being applied
                m_overflow += m_beamDamage * Time.deltaTime;


				// Reset the distance according to the RaycastHit
				ResetOffset (m_beamHit.distance);


				// Only the host should apply damage and only if damage can be dealt
				if (m_beamHit.collider != null && m_beamHit.collider.attachedRigidbody != null)
				{
					DamageHit (m_beamHit);
				}
			}
			
			// Raycast found nothing
			else
            {
                // Reset the damage
                m_overflow = 0f;

				ResetOffset();
			}
		}
	}
	


    ///////////////////////////////
    /// Initial setup functions ///
    ///////////////////////////////



	/// <summary>
	/// Sets the correct layer for damage according to the beams layer.
    /// </summary>
	void LayerMaskSetup()
	{
		const int 	player = (1 << Layers.player), 
					capital = (1 << Layers.capital), 
					enemy = (1 << Layers.enemy), 
					enemySupportShield = (1 << Layers.enemySupportShield),
					enemyDestructibleBullet = (1 << Layers.enemyDestructibleBullet),
					enemyCollide = (1 << Layers.enemyCollide),
					asteroid = (1 << Layers.asteroid);
		
		switch (gameObject.layer)
		{
			case Layers.playerBullet:
			case Layers.capitalBullet:
				m_damageMask = enemy | enemyDestructibleBullet | asteroid | enemySupportShield | enemyCollide;
				break;
				
			case Layers.enemyBullet:
			case Layers.enemyDestructibleBullet:
				m_damageMask = player | capital | asteroid;
				break;
		}
	}



    ////////////////////////////
    /// Damage functionality ///
    ////////////////////////////
    

    
    /// <summary>
    /// Damage the mob and apply impact force.
    /// </summary>
    /// <param name="hit">The RaycastHit to apply damage to.</param>
    void DamageHit (RaycastHit hit)
    {
        // Colliders may be part of a composite collider so we must use Collider.attachedRigidbody to get the HealthScript component
        Rigidbody mob = hit.collider.attachedRigidbody;
        
        if (hit.collider.tag != "Shield")
        {
            // Push the enemy away from the force of the beam
            mob.AddForceAtPosition (transform.up * m_impactForce * Time.deltaTime, hit.point);
        }
        
        // Only the host should cause damage
        if (Network.isServer)
        {
            if (m_overflow > 1f)
            {
                int damage = (int) m_overflow;
                m_overflow -= damage;
                
                if (hit.collider.gameObject.layer == Layers.enemySupportShield)
                {
                    EnemySupportShield script = hit.collider.gameObject.GetComponent<EnemySupportShield>();
                    if (script != null)
                    {
                        script.DamageShield (damage);
                        script.BeginShaderCoroutine (hit.point);
                    }
                }
                
                else
                {
                    HealthScript health = mob.GetComponent<HealthScript>();
                    if (health != null)
                    {
                        health.DamageMob (damage, m_firer, gameObject);
                    }
                    
                    else
                    {
                        Debug.LogError ("Can't find HealthScript on " + mob.name);
                    }
                }
            }
        }
    }
    
    
    
    /////////////////////////////////
    /// Beam length functionality ///
    /////////////////////////////////



    /// <summary>
    /// Calcuates the correct localPosition and localScale for the beam based on the distance given. The scale will be converted to real world units.
    /// </summary>
    /// <param name="distance">How long the beam should be</param>
	void ResetOffset (float distance = -1f)
	{
		// Calculate the scale modifier based on the parents scale
		float scaleModifier = Mathf.Max (transform.root.localScale.y * transform.parent.localScale.y, 0.00001f);

		float newPositionY = distance >= 0f && distance <= m_beamLength ? distance * (1f / scaleModifier) : m_beamLength * (1f / scaleModifier);

		m_offset.y = newPositionY / 2f;
		transform.localPosition = m_offset;
		
		// The scale will always be double the position to centre the beam
		Vector3 newScale = new Vector3 (transform.localScale.x, newPositionY, transform.localScale.z);
		transform.localScale = newScale;
	}
	


    ///////////////////////////////
    /// Parenting functionality ///
    ///////////////////////////////



    /// <summary>
    /// Inform other clients to parent the beam to the corresponding player.
    /// </summary>
    /// <param name="playerName">The player name to search for with the GameStateController.</param>
    public void ParentBeamToFirer(string playerName)
    {
        networkView.RPC("ParentBeamToFirerOverNetwork", RPCMode.Others, playerName);
    }


	/// <summary>
    /// Used to ensure that the beam is properly parented across all clients.
    /// </summary>
    /// <param name="playerName">The player name to search for with the GameStateController.</param>
	[RPC] void ParentBeamToFirerOverNetwork (string playerName)
	{
		GameStateController gsc = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameStateController>();
		
		GameObject playerGO = gsc.GetPlayerFromNetworkPlayer(gsc.GetNetworkPlayerFromID(gsc.GetIDFromName(playerName)));
		Debug.Log ("Attaching beam: " + this.name + " to gameObject: " + playerGO.name + ".");

		this.transform.parent = playerGO.GetComponent<PlayerControlScript>().GetWeaponObject().transform;
		m_firer = playerGO;
    }
    
    
    /// <summary>
    /// Inform other clients to parent the beam to the corresponding CShip turret.
    /// </summary>
    /// <param name="turretID">The corresponding turret ID.</param>
    public void ParentBeamToCShipTower (int turretID)
    {
        networkView.RPC("ParentBeamToCShipOverNetwork", RPCMode.Others, turretID);
    }
	
	
	/// <summary>
    /// Used to ensure that the beam is properly parented across all clients.
    /// </summary>
    /// <param name="turretID">The corresponding turret ID.</param>
	[RPC] void ParentBeamToCShipOverNetwork (int turretID)
	{
		GameObject cship = GameObject.FindGameObjectWithTag("Capital");
		GameObject turret = cship.GetComponent<CapitalShipScript>().GetCTurretHolderWithId(turretID);
		this.transform.parent = turret.transform.GetChild(0);
	}
}

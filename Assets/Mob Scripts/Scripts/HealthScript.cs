using UnityEngine;
using System.Collections;

public class HealthScript : MonoBehaviour 
{
	//Only need to use this for the capital ship
	[SerializeField]
	GameObject m_GameStateController;
	
	[SerializeField]
	GameObject m_DeathObjectRef;
	
	public void SetGameStateController(GameObject controller)
	{
		m_GameStateController = controller;
	}
	
	[SerializeField]
	int m_maximumHealth = 100;
	[SerializeField]
	int m_currentHealth;
	
	[SerializeField]
	string m_pathToShieldObject = "Composite Collider/Shield";
	
	// Stops the script from repeating searching for the shield GameObject
	GameObject m_shieldCache;

    bool isDead = false;

	public void EquipNewPlating(int hullValue)
	{
		float percent = GetHPPercentage();
		m_maximumHealth = hullValue;
		
		//m_currentHealth = (int)(hullValue * percent);
		m_currentHealth = m_maximumHealth;
		
		if(Network.isServer)
		{
			//Debug.Log ("Sent health values: " + m_currentHealth + "/" + m_maximumHealth + ".");
			networkView.RPC ("PropagateDamageAndMaxs", RPCMode.Others, m_currentHealth, m_maximumHealth, m_currentShield, m_maximumShield);
		}
	}
	
	[SerializeField]
	int m_maximumShield = 100;
	[SerializeField]
	int m_currentShield;
	
	public void EquipNewShield(int capacity, int rechargeRate, float rechargeDelay)
	{
		m_maximumShield = capacity;
		m_currentShield = GetMaxShield();
		m_shieldRechargeRate = rechargeRate;
		m_timeToRechargeShield = rechargeDelay;
		
		if(Network.isServer)
		{
			networkView.RPC ("PropagateDamageAndMaxs", RPCMode.Others, m_currentHealth, m_maximumHealth, m_currentShield, m_maximumShield);
			networkView.RPC ("PropagateShieldRechargeStats", RPCMode.Others, rechargeRate, rechargeDelay);
		}
	}
	
	[SerializeField]
	int m_shieldRechargeRate = 6;
	[SerializeField]
	float m_timeToRechargeShield = 4.0f;
	float m_currentShieldDownTime = 0;
	bool isRegenerating = false;
	float regenFloatCatch = 0;
	
	public bool m_shouldStop = false;
	[HideInInspector]
	public bool m_isInvincible = false;
	
	// Use this for initialization
	void Start () 
	{
		m_currentHealth = m_maximumHealth;
        m_currentShield = GetMaxShield();
	}
	
	// Update is called once per frame
	void Update () 
	{
		if(m_currentShieldDownTime < m_timeToRechargeShield && !m_shouldStop)
		{
			m_currentShieldDownTime += Time.deltaTime;
			if(m_currentShieldDownTime >= m_timeToRechargeShield)
			{
				isRegenerating = true;
				ShieldOnOff (true);
			}
		}

        if (isRegenerating && m_currentShield < GetMaxShield() && !m_shouldStop)
		{
			regenFloatCatch += Time.deltaTime * m_shieldRechargeRate;
			if(Mathf.FloorToInt(regenFloatCatch) > 0)
			{
				int increase = Mathf.FloorToInt(regenFloatCatch);
				m_currentShield += increase;
				regenFloatCatch -= increase;
				//Debug.Log ("Regenned " + increase + " shield.");
			}
		}
	}
	void OnCollisionEnter(Collision collision)
	{
		//if(this.tag == "Asteroid")
		//Debug.Log ("Object '" + name + " collided with object: '" + collision.gameObject.name + "'.");
		//NOTE: This function should only apply damage to the other collider, since the function will be called on
		// 		both sides
        if (Network.isServer)
        {
            if(this.tag == "Enemy")
            {
                //If we're an enemy, check if the collider is the capital ship or the player
                if(collision.gameObject.tag == "Capital")
                {
                    //If it's a PC or the capital ship, work out how fast the collision was
                    //TODO: 
                    //Insert sounds here
                    Ship shipComponent = GetComponent<Ship>();
                    
                    float magnitude = collision.relativeVelocity.magnitude * collision.rigidbody.mass;
                    int PCdamage = (int)(magnitude * shipComponent.GetRamDam());
                    //Debug.Log("Applying " + PCdamage + " damage to PC.");
                    collision.gameObject.GetComponent<HealthScript>().DamageMob(PCdamage, this.gameObject);
                }
                else if(collision.gameObject.tag == "Player")
                {
                    Ship shipComponent = GetComponent<Ship>();

                    float magnitude = collision.relativeVelocity.magnitude * collision.rigidbody.mass;
                    int PCdamage = (int)(magnitude * shipComponent.GetRamDam() * 10);
                    Debug.Log("Applying " + PCdamage + " damage to PC.");
                    collision.gameObject.GetComponent<HealthScript>().DamageMob(PCdamage, this.gameObject);
                }
                else if(collision.gameObject.tag == "Asteroid")
                {
                    //If the other collider is an asteroid, don't try to apply damage to it, just deal damage to self instead
                    int magnitude = (int)(collision.relativeVelocity.magnitude * 2.0f);
                    //Debug.Log ("Asteroid hit enemy for " + magnitude + " damage.");
                    DamageMob(magnitude, collision.gameObject);
                }
            }
            else if(this.tag == "Capital" || this.tag == "Player")
            {
                //If we're the player or the capital ship, check if the collider is the enemy
                if(collision.gameObject.tag == "Enemy")
                {
                    //If it's an enemy, work out how fast the collision between the two is
                    float magnitude = collision.relativeVelocity.magnitude;
                    int NMdamage = 0;
                    if(this.GetComponent<PlayerControlScript>() != null)
                    {
                        NMdamage = (int)(magnitude * this.GetComponent<PlayerControlScript>().GetRamDam());
                    }
                    else
                    {
                        //TODO: Maybe parametise capital ship ram damage? (the 1.0f)
                        NMdamage = (int)(magnitude * 5.0f);
                    }
                    //Debug.Log ("Applying " + NMdamage + " damage to enemy.");
                    collision.gameObject.GetComponent<HealthScript>().DamageMob(NMdamage, this.gameObject);
                }
                else if(collision.gameObject.tag == "Asteroid")
                {
                    if(this.gameObject.tag == "Capital" && Network.isClient)
                    {
                        //Do nothing
                    }
                    else
                    {
                        //If the other collider is an asteroid, don't try to apply damage to it, just deal damage to self instead
                        int magnitude = (int)(collision.relativeVelocity.magnitude * 2.0f);
                        //Debug.Log ("Asteroid hit player for " + magnitude + " damage.");
                        DamageMob(magnitude, collision.gameObject);
                    }
                }
            }
        }
	}
	
	public void RepairHP(int amount)
	{
		m_currentHealth += amount;
		if(m_currentHealth > m_maximumHealth)
			m_currentHealth = m_maximumHealth;
		
		networkView.RPC ("PropagateDamage", RPCMode.Server, m_currentHealth, m_currentShield);
		Debug.Log ("Clienting sending new health value of " + m_currentHealth + " to host.");
	}
	public float GetHPPercentage()
	{
		float output = (float)m_currentHealth / (float)m_maximumHealth;
		return output;
	}
	public int GetMaxHP()
	{
		return m_maximumHealth;
	}
	public int GetCurrHP()
	{
		return m_currentHealth;
	}
	public float GetShieldPercentage()
	{
        float output = (float)m_currentShield / (float)GetMaxShield();
		return output;
	}
	public int GetMaxShield()
	{
		return m_maximumShield;
	}
	public int GetCurrShield()
	{
		return m_currentShield;
	}
	
	bool hasBeenHitAlready = false;
	public void RemotePlayerRequestsDirectDamage(int damage)
	{
		// Fixes bug where host can't die
		if (Network.isServer)
		{
			DamageMobHullDirectly (damage);
		}
		
		else
		{
			networkView.RPC ("PropagateRemoteDirectDamage", RPCMode.Server, damage);
		}
	}
	[RPC]
	void PropagateRemoteDirectDamage(int damage)
	{
		DamageMobHullDirectly(damage);
	}
	
	public void DamageMobHullDirectly(int damage)
	{
		//Debug.Log ("Mob: " + this.name + " recieves " + damage + " damage directly to hull");
		
		if(Network.isServer && !m_isInvincible)
		{
			m_currentHealth -= damage;
			if(m_currentHealth < 0)
			{
				//Mob is dead :(
				//Debug.Log ("Alerting Mob that it's ran out of HP");
				OnMobDies(null);
			}
			
			//If we're host, propagate the damage to the rest of the clients
			try 
			{
				networkView.RPC ("PropagateDamage", RPCMode.Others, m_currentHealth, m_currentShield);
			}
			catch (UnityException e) {}
			
			//Tell gamecontroller the capital ship is under attack
			if(this.tag == "Capital")
				m_GameStateController.GetComponent<GameStateController>().CapitalShipHasTakenDamage();
		}
	}
	public void ResetShieldRecharge()
	{
		networkView.RPC ("PropagateShieldRechargeReset", RPCMode.All);
	}
	[RPC]
	void PropagateShieldRechargeReset()
	{
		m_currentShieldDownTime = 0;
		isRegenerating = false;
	}
	public void DamageMob(int damage, GameObject firer, GameObject hitter = null)
	{
		//Debug.Log ("Damaging mob: " + this.name + ".");
		if(!m_isInvincible)
		{
			if(m_currentShield > 0)
			{
				//If shields are up, apply damage to the shield
				m_currentShield -= damage;
				if(m_currentShield <= 0)
				{
					ShieldOnOff (false);
					//if the shield gives out before all the damage is dealt, apply the remainder to the hull
					m_currentHealth += m_currentShield;
					if(m_currentHealth <= 0)
					{
						//Mob is dead :(
						OnMobDies (firer, hitter);
					}
					m_currentShield = 0;
				}

				if (hitter)
				{
					BeamBulletScript beam = hitter.GetComponent<BeamBulletScript>();

					Vector3 position = beam ? beam.beamHit.point : hitter.transform.position;
					int dType = beam ? (int)beam.m_damageType : (int)hitter.GetComponent<BasicBulletScript>().m_damageType;
					float magnitude = beam ? beam.GetDamage() : hitter.GetComponent<BasicBulletScript>().GetDamage();

					networkView.RPC ("PropagateShieldWibble", RPCMode.All, position, dType, magnitude);
				}
			}
			else
			{
				//If no shields, apply damage directly to hull
				m_currentHealth -= damage;
				if(m_currentHealth <= 0)
				{
					//Mob is dead :(
					OnMobDies(firer, hitter);
				}
			}
			
			if(Network.isServer)
			{
				//If we're host, propagate the damage to the rest of the clients
				try
				{
					networkView.RPC ("PropagateDamage", RPCMode.Others, m_currentHealth, m_currentShield);
				}
				catch (UnityException e) {}
				
				//Tell gamecontroller the capital ship is under attack
				if(this.tag == "Capital")
				{
					if(firer != null && firer.tag != "Asteroid")
						m_GameStateController.GetComponent<GameStateController>().CapitalShipHasTakenDamage();
				}
				
				if(this.tag == "Enemy")
				{
					if(firer != null && firer.tag != "Asteroid")
					{
						if(GetHPPercentage() < 0.3f)
						{
							//Alert enemy it's dying and should kamikaze
							//Debug.Log("Enemy: " + this.name + " is enraged!");
							this.GetComponent<EnemyScript>().AlertLowHP(firer);
						}
						else if(!hasBeenHitAlready)
						{
							hasBeenHitAlready = true;
							this.GetComponent<EnemyScript>().AlertFirstHit(firer);
						}
						else
						{
							this.GetComponent<EnemyScript>().NotifyEnemyUnderFire(firer);
						}
					}
				}
			}
			
			//Whatever happens, reset the shield cooldown
			ResetShieldRecharge();
		}
	}

	[RPC]
	void PropagateShieldWibble(Vector3 position, int type, float magnitude)
	{
		if(this.GetComponent<CapitalShipScript>())
		{
			this.GetComponent<CapitalShipScript>().BeginShaderCoroutine(position, type, magnitude);
		}
		else if(this.GetComponent<PlayerControlScript>())
		{
			this.GetComponent<PlayerControlScript>().BeginShaderCoroutine(position, type, magnitude);
		}
		else if(this.GetComponent<EnemyScript>())
		{
			this.GetComponent<EnemyScript>().BeginShaderCoroutine(position, type, magnitude);
		}
	}

	[RPC]
	void PropagateShieldRechargeStats(int shieldRechargeRate, float shieldRechargeDelay)
	{
		Debug.Log ("Recieved shield values: " + shieldRechargeRate + "R, " + shieldRechargeDelay + "D.");
		m_shieldRechargeRate = shieldRechargeRate;
		m_timeToRechargeShield = shieldRechargeDelay;
	}
	[RPC]
	void PropagateDamageAndMaxs(int currentHP, int maxHP, int currentShield, int maxShield)
	{
		//Debug.Log ("Recieved values: " + currentHP + "/" + maxHP + ".");
		m_currentHealth = currentHP;
		m_maximumHealth = maxHP;
		m_currentShield = currentShield;
		m_maximumShield = maxShield;
	}
	[RPC]
	void PropagateDamage(int currentHP, int currentShield)
	{
		m_currentHealth = currentHP;
		m_currentShield = currentShield;
	}
	[RPC]
	void PropagateShieldStatus(bool isUp)
	{
		ShieldOnOff (isUp);
	}

	// Shouldn't be necessary anymore, consider deletion
	Vector3 GetLaserImpactPoint(GameObject laser)
	{
		Vector3 output = Vector3.zero;
		RaycastHit info;
		if(Physics.Raycast(laser.transform.position, laser.transform.up, out info))
		{
			output = info.point;
		}

		Debug.Log ("Returned position: " + output);
		return output;
	}
	void ShieldOnOff (bool isUp)
	{
		GameObject shield = GetShield();
		if (shield)
		{
			if (shield.collider)
			{
				shield.collider.enabled = isUp;
			}
			shield.renderer.enabled = isUp;
		}
		
		if(Network.isServer)
			networkView.RPC ("PropagateShieldStatus", RPCMode.Others, isUp);
	}
	

	void OnMobDies (GameObject killer, GameObject hitter = null)
	{
        if(isDead == true)
        {
            return;
        }

		//Debug.Log ("Mob has died!");
		if(this.GetComponent<PlayerControlScript>() != null)
		{
			//Only act if we're the owner
			if(this.GetComponent<PlayerControlScript>().GetOwner() == Network.player)
			{
				//Object is a player, apply special rules
				Debug.Log ("[HealthScript]: Player has died!");
				
				Debug.Log ("[HealthScript]: Alerting GameController...");
				GameObject.FindGameObjectWithTag("GameController").GetComponent<GameStateController>().NotifyLocalPlayerHasDied(this.gameObject);
				//networkView.RPC ("PropagatePlayerDeath", RPCMode.Others);
				Debug.Log ("[HealthScript]: Destroying object...");
                networkView.RPC("PropagateEntityDied", RPCMode.All);
				
				//Use this to fix crashes
				//this.gameObject.SetActive(false);
			}
			else
			{
				//Tell the client that it's dead
				networkView.RPC ("PropagatePlayerHasJustDied", this.GetComponent<PlayerControlScript>().GetOwner());
                networkView.RPC("PropagateEntityDied", RPCMode.All);
			}
		}
		else if(this.tag == "Capital")
		{
			//Capital ship has been destroyed, game over
			//m_GameStateController.GetComponent<GameStateController>().CapitalShipHasBeenDestroyed();
			if(Network.isServer)
			{
				m_GameStateController.GetComponent<GameStateController>().TellAllClientsCapitalShipHasBeenDestroyed();
				//Network.Destroy(this.gameObject);
			}
			
			//Animate destruction, pause game, splash text?
			//TODO: Zoom to capital, long death animation, sounds + then overlay, nauts style
		}
		else if(this.tag == "Enemy")
		{
			//If this mob isn't a player, apply bounty to killer (if pc) and destroy mob
			if(killer && killer.transform.root.GetComponent<PlayerControlScript>() != null)
			{
				killer.transform.root.GetComponent<PlayerControlScript>().AddSpaceBucks(this.GetComponent<EnemyScript>().GetBounty());
			}

            if(Network.isServer)
                networkView.RPC("PropagateEntityDied", RPCMode.All);
			
			if(m_DeathObjectRef != null)
			{
				Network.Instantiate(m_DeathObjectRef, this.transform.position, this.transform.rotation, 0);
			}
		}
		else if(this.tag == "Asteroid")
		{
			if (hitter)
			{
				BeamBulletScript beam = hitter.GetComponent<BeamBulletScript>();

				if (beam)
				{
					GetComponent<AsteroidScript>().SplitAsteroid (beam.beamHit.point);
				}

				else
				{
					GetComponent<AsteroidScript>().SplitAsteroid (hitter.transform);
				}
			}

			else
			{
				GetComponent<AsteroidScript>().SplitAsteroid (transform);
			}
		}
		else if(this.tag == "Bullet")
		{
			//Tell the bullet to explode if it has AoE
			this.GetComponent<BasicBulletScript>().DetonateBullet();
		}
	}
	[RPC]
	void PropagatePlayerHasJustDied()
	{
		GameObject.FindGameObjectWithTag("GameController").GetComponent<GameStateController>().NotifyLocalPlayerHasDied(this.gameObject);
		//Network.Destroy (this.gameObject);
	}

    [RPC]
    void PropagateEntityDied()
    {
        isDead = true;
        GetComponent<Explode>().Fire();
    }
	
	/*[RPC]
	void PropagatePlayerDeath()
	{
		Destroy (this.gameObject);

		//this.gameObject.SetActive(false);

		/*this.renderer.enabled = false;
		this.enabled = false;
		this.GetComponent<HealthScript>().enabled = false;*/
	//}
	
	public void ResetHPOnRespawn()
	{
        m_currentShield = GetMaxShield();
		m_currentHealth = (int)(m_maximumHealth * 0.25f);
	}
	
	GameObject GetShield()
	{
		if (!m_shieldCache || m_shieldCache.tag != "Shield")
		{
			// Search child objects for the shield.
			Transform result = transform.Find (m_pathToShieldObject);
			m_shieldCache = result ? result.gameObject : null;
			
			if (!m_shieldCache || m_shieldCache.tag != "Shield")
			{
				// Fall back to old method and search
				foreach (Transform child in this.transform)
				{
					if (child.tag == "Shield")
					{
						m_shieldCache = child.gameObject;
					}
				}
				
				if (!m_shieldCache)
				{
					Debug.LogWarning ("No shield found for mob " + this.name);
				}
			}
		}
		
		return m_shieldCache;
	}

    public void SetModifier(float modifier_)
    {
        m_maximumHealth = (int)(m_maximumHealth * modifier_);
        m_currentHealth = m_maximumHealth;
        m_maximumShield = (int)(m_maximumShield * modifier_);
        m_currentShield = m_maximumShield;
        if (Network.isServer)
        {
            networkView.RPC("PropagateDamageAndMaxs", RPCMode.Others, m_currentHealth, m_maximumHealth, m_currentShield, m_maximumShield);
        }
    }

}

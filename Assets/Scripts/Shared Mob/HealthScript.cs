using UnityEngine;
using System.Collections;

public class HealthScript : MonoBehaviour 
{
	//Only need to use this for the capital ship
	[SerializeField] GameObject m_DeathObjectRef;

    /*[SerializeField] int m_maximumShield = 100;
    [SerializeField] int m_currentShield;
	
	[SerializeField] int m_maximumHealth = 100;
	[SerializeField] int m_currentHealth;
	
    [SerializeField] int m_shieldRechargeRate = 6;
    [SerializeField] float m_timeToRechargeShield = 4.0f;*/

    
	
	// Stops the script from repeating searching for the shield GameObject
    
    EquipmentTypePlating m_platingCache;
    EquipmentTypeShield m_shieldCache;

    bool isDead = false;
    
    float m_currentShieldDownTime = 0;
    bool isRegenerating = false;
    float regenFloatCatch = 0;

    bool m_shouldStop = false;

    bool m_isInvincible = false;
    bool hasBeenHitAlready = false;

    #region getset

    public bool GetShouldStop()
    {
        return m_shouldStop;
    }

    public void SetShouldStop(bool shouldStop_)
    {
        m_shouldStop = shouldStop_;
    }

    public bool IsInvincible()
    {
        return m_isInvincible;
    }

    public void SetInvincible(bool invincible_)
    {
        m_isInvincible = invincible_;
    }

    public float GetHPPercentage()
    {
        return m_platingCache.GetHealthPercentage();
    }
    public int GetMaxHP()
    {
        return m_platingCache.GetHealthMax();
    }
    public int GetCurrHP()
    {
        return m_platingCache.GetHealthCurrent();
    }
    public float GetShieldPercentage()
    {
        return m_shieldCache.GetShieldPercentage();
    }
    public int GetMaxShield()
    {
        return m_shieldCache.GetShieldMax();
    }
    public int GetCurrShield()
    {
        return m_shieldCache.GetShieldCurrent();
    }

    #endregion

    /*public void EquipNewPlating(int hullValue)
	{
		float percent = GetHPPercentage();
		m_maximumHealth = hullValue;
		
		m_currentHealth = (int)(hullValue * percent);
        
        if(m_currentHealth <= 0)
            m_currentHealth = m_maximumHealth;
		
		if(Network.isServer)
		{
			//Debug.Log ("Sent health values: " + m_currentHealth + "/" + m_maximumHealth + ".");
			networkView.RPC ("PropagateDamageAndMaxs", RPCMode.Others, m_currentHealth, m_maximumHealth, m_currentShield, m_maximumShield);
		}
	}*/
	
	/*public void EquipNewShield(int capacity, int rechargeRate, float rechargeDelay)
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
	}*/
	
	// Use this for initialization
	void Start () 
	{
        m_platingCache = GetComponent<EquipmentTypePlating>();
        m_shieldCache = GetComponent<EquipmentTypeShield>();
        
        if(!m_platingCache)
            Debug.LogError ("Object with health script '" + gameObject.name + "' has no attached plating!");
            
        if(!m_shieldCache)
            Debug.LogError ("Object with health script '" + gameObject.name + "' has no attached shield!");
	}
	
	// Update is called once per frame
	void Update () 
	{
		/*if(m_currentShieldDownTime < m_timeToRechargeShield && !m_shouldStop)
		{
			m_currentShieldDownTime += Time.deltaTime;
			if(m_currentShieldDownTime >= m_timeToRechargeShield)
			{
				isRegenerating = true;
				ShieldOnOff (true);
			}
		}*/

        /*if (isRegenerating && m_currentShield < GetMaxShield() && !m_shouldStop)
		{
			regenFloatCatch += Time.deltaTime * m_shieldRechargeRate;
			if(Mathf.FloorToInt(regenFloatCatch) > 0)
			{
				int increase = Mathf.FloorToInt(regenFloatCatch);
				m_currentShield += increase;
				regenFloatCatch -= increase;
				//Debug.Log ("Regenned " + increase + " shield.");
			}
		}*/
	}
	void OnCollisionEnter(Collision collision)
	{
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
                    HealthScript health = collision.gameObject.GetComponent<HealthScript>();
                    health.DamageMob(PCdamage, this.gameObject);
                }
                else if(collision.gameObject.tag == "Player")
                {
                    Ship shipComponent = GetComponent<Ship>();

                    float magnitude = collision.relativeVelocity.magnitude * collision.rigidbody.mass;
                    int PCdamage = (int)(magnitude * shipComponent.GetRamDam() * 10);
                    Debug.Log("Applying " + PCdamage + " damage to PC.");
                    HealthScript health = collision.gameObject.GetComponent<HealthScript>();
                    health.DamageMob(PCdamage, this.gameObject);
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
                        PlayerControlScript player = GetComponent<PlayerControlScript>();
                        NMdamage = (int)(magnitude * player.GetRamDam());
                    }
                    else
                    {
                        //TODO: Maybe parametise capital ship ram damage? (the 1.0f)
                        NMdamage = (int)(magnitude * 5.0f);
                    }
                    //Debug.Log ("Applying " + NMdamage + " damage to enemy.");

                    HealthScript health = collision.gameObject.GetComponent<HealthScript>();
                    health.DamageMob(NMdamage, this.gameObject);
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
        m_platingCache.RepairPlating(amount);
        
		/*m_currentHealth += amount;
		if(m_currentHealth > m_maximumHealth)
			m_currentHealth = m_maximumHealth;
		
		networkView.RPC ("PropagateDamage", RPCMode.Server, m_currentHealth, m_currentShield);
		Debug.Log ("Clienting sending new health value of " + m_currentHealth + " to host.");*/
	}
	
	
	
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
		if(Network.isServer && !m_isInvincible)
		{
			m_platingCache.DamagePlating(damage, null);
            m_shieldCache.ResetRechargeDelay();
			
			//Tell gamecontroller the capital ship is under attack
			if(this.tag == "Capital")
            {
                GameStateController.Instance().CapitalShipHasTakenDamage();
            }	
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
            if(m_shieldCache && m_shieldCache.GetIsShieldUp())
            {
                bool shouldAlsoHitHP = false;
                int overflowAmount = m_shieldCache.GetShieldCurrent() - damage;
                
                if(overflowAmount < 0)
                {
                    shouldAlsoHitHP = true;
                }
                
                m_shieldCache.DamageShield(damage);
                
                if(shouldAlsoHitHP)
                {
                    m_platingCache.DamagePlating(overflowAmount, firer, hitter);
                }
                else if(hitter != null)
                {
                    BeamBulletScript beam = hitter.GetComponent<BeamBulletScript>();
                    
                    Vector3 position = beam ? beam.GetBeamHit().point : hitter.transform.position;
                    int dType = beam ? (int)beam.GetDamageType() : (int)hitter.GetComponent<Bullet>().GetMajorElement();
                    float magnitude = beam ? beam.GetDamage() : hitter.GetComponent<Bullet>().GetDamage();
                    
                    networkView.RPC ("PropagateShieldWibble", RPCMode.All, position, dType, magnitude);
                }
            }
            else
            {
                m_platingCache.DamagePlating(damage, firer, hitter);
                
                if(m_shieldCache)
                    m_shieldCache.ResetRechargeDelay();
            }
        
			/*if(m_currentShield > 0)
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

				if (hitter != null)
				{
					BeamBulletScript beam = hitter.GetComponent<BeamBulletScript>();

					Vector3 position = beam ? beam.GetBeamHit().point : hitter.transform.position;
					int dType = beam ? (int)beam.GetDamageType() : (int)hitter.GetComponent<BasicBulletScript>().GetDamageType();
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
			}*/
			
			if(Network.isServer)
			{
				//If we're host, propagate the damage to the rest of the clients
				//networkView.RPC ("PropagateDamage", RPCMode.Others, m_currentHealth, m_currentShield);
				
				//Tell gamecontroller the capital ship is under attack
				if(this.tag == "Capital")
				{
					if(firer != null && firer.tag != "Asteroid")
                        GameStateController.Instance().CapitalShipHasTakenDamage();
				}
				
				if(this.tag == "Enemy")
				{
					if(firer != null && firer.tag != "Asteroid")
					{
						if(GetHPPercentage() < 0.3f)
						{
							//Alert enemy it's dying and should kamikaze
							//Debug.Log("Enemy: " + this.name + " is enraged!");
							this.GetComponent<ShipEnemy>().AlertLowHP(firer);
						}
						else if(!hasBeenHitAlready)
						{
							hasBeenHitAlready = true;
							this.GetComponent<ShipEnemy>().AlertFirstHit(firer);
						}
						else
						{
							this.GetComponent<ShipEnemy>().NotifyEnemyUnderFire(firer);
						}
					}
				}
			}
			
			//Whatever happens, reset the shield cooldown
			//ResetShieldRecharge();
		}
	}

	[RPC]
	void PropagateShieldWibble(Vector3 position, int type, float magnitude)
	{
        Ship ship;
        if ((ship = GetComponent<Ship>()) != null)
		{
            ship.BeginShaderCoroutine(position, type, magnitude);
		}
	}

	[RPC]
	void PropagateShieldRechargeStats(int shieldRechargeRate, float shieldRechargeDelay)
	{
		//Debug.Log ("Recieved shield values: " + shieldRechargeRate + "R, " + shieldRechargeDelay + "D.");
		//m_shieldRechargeRate = shieldRechargeRate;
		//m_timeToRechargeShield = shieldRechargeDelay;
	}
	[RPC]
	void PropagateDamageAndMaxs(int currentHP, int maxHP, int currentShield, int maxShield)
	{
		//Debug.Log ("Recieved values: " + currentHP + "/" + maxHP + ".");
		//m_currentHealth = currentHP;
		//m_maximumHealth = maxHP;
		//m_currentShield = currentShield;
		//m_maximumShield = maxShield;
	}
	[RPC]
	void PropagateDamage(int currentHP, int currentShield)
	{
		//m_currentHealth = currentHP;
		//m_currentShield = currentShield;
	}
	[RPC]
	void PropagateShieldStatus(bool isUp)
	{
		ShieldOnOff (isUp);
	}

	public void ShieldOnOff (bool isUp)
	{
        Ship ship = GetComponent<Ship>();
        GameObject shield = ship.GetShield();
		if (shield != null)
		{
			if (shield.collider != null)
			{
				shield.collider.enabled = isUp;
			}
			shield.renderer.enabled = isUp;
		}
		
		if(Network.isServer)
			networkView.RPC ("PropagateShieldStatus", RPCMode.Others, isUp);
	}
	

	public void OnMobDies (GameObject killer, GameObject hitter = null)
	{
        if(isDead == true)
        {
            return;
        }

        PlayerControlScript ship;

		//Debug.Log ("Mob has died!");
		if((ship = GetComponent<PlayerControlScript>()) != null)
		{
			//Only act if we're the owner
            if (ship.GetOwner() == Network.player)
			{
				//Object is a player, apply special rules
				Debug.Log ("[HealthScript]: Player has died!");
				
				Debug.Log ("[HealthScript]: Alerting GameController...");
                GameStateController.Instance().NotifyLocalPlayerHasDied(this.gameObject);
				//networkView.RPC ("PropagatePlayerDeath", RPCMode.Others);
				Debug.Log ("[HealthScript]: Destroying object...");
                networkView.RPC("PropagateEntityDied", RPCMode.All);
				
				//Use this to fix crashes
				//this.gameObject.SetActive(false);
			}
			else
			{
				//Tell the client that it's dead
                networkView.RPC("PropagatePlayerHasJustDied", ship.GetOwner());
                networkView.RPC("PropagateEntityDied", RPCMode.All);
			}
		}
		else if(this.tag == "Capital")
		{
			//Capital ship has been destroyed, game over
			if(Network.isServer)
			{
                GameStateController.Instance().TellAllClientsCapitalShipHasBeenDestroyed();
				//Network.Destroy(this.gameObject);
			}
			
			//Animate destruction, pause game, splash text?
			//TODO: Zoom to capital, long death animation, sounds + then overlay, nauts style
		}
		else if(this.tag == "Enemy")
		{
			//If this mob isn't a player, apply bounty to killer (if pc) and destroy mob
			if(killer != null && killer.transform.root.GetComponent<PlayerControlScript>() != null)
			{
                PlayerControlScript playerShip = killer.transform.root.GetComponent<PlayerControlScript>();
                Inventory playerInv = playerShip.GetComponent<Inventory>();
                playerInv.SetCash(this.GetComponent<ShipEnemy>().GetBountyAmount() + playerInv.GetCurrentCash());
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
			if (hitter != null)
			{
				BeamBulletScript beam = hitter.GetComponent<BeamBulletScript>();

				if (beam != null)
				{
					GetComponent<AsteroidScript>().SplitAsteroid (beam.GetBeamHit().point);
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
            
            Destroy (gameObject);
		}
		else if(this.tag == "Bullet")
		{
			//Tell the bullet to explode if it has AoE
			this.GetComponent<BasicBulletScript>().DetonateBullet();
		}
        else if(this.tag == "Shop")
        {
            Debug.Log ("What the hell happened here?!");
        }
	}
	[RPC]
	void PropagatePlayerHasJustDied()
	{
        GameStateController.Instance().NotifyLocalPlayerHasDied(this.gameObject);
		//Network.Destroy (this.gameObject);
	}

    [RPC]
    void PropagateEntityDied()
    {
        isDead = true;
        GetComponent<Explode>().Fire();
    }
	
	public void ResetHPOnRespawn()
	{
        //m_currentShield = GetMaxShield();
		//m_currentHealth = (int)(m_maximumHealth * 0.25f);
	}

    public void SetModifier(float modifier_)
    {
        /*m_maximumHealth = (int)(m_maximumHealth * modifier_);
        m_currentHealth = m_maximumHealth;
        m_maximumShield = (int)(m_maximumShield * modifier_);
        m_currentShield = m_maximumShield;
        if (Network.isServer)
        {
            networkView.RPC("PropagateDamageAndMaxs", RPCMode.Others, m_currentHealth, m_maximumHealth, m_currentShield, m_maximumShield);
        }*/
    }

}

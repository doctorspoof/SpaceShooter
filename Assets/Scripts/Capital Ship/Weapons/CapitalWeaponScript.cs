using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum AimingStyle
{
	None = 0,
	Basic = 1,
	Leading = 2,
	EdgeExplode = 3,
	Beam = 4
}

public class CapitalWeaponScript : MonoBehaviour 
{
    /* Serializable members */
    //Generic weapon stats
    [SerializeField,Range(1.0f, 40.0f)] float m_rotateSpeed = 20f;              // The speed at which the turret can rotate around Z-axis
    [SerializeField,Range(0.5f, 5.0f)]  float m_recoilTime = 2.0f;              // The time between shots
    [SerializeField]                    GameObject bulletRef;                   // Reference to the bullet to be fired
    [SerializeField]                    Transform[] m_firePoints;               // List of all the points shots will be fired from
    [SerializeField]                    GameObject[] m_cannons;                 // Reference to the barrels that will need to be told to recoil upon firing
    [SerializeField]                    AimingStyle m_turretAimStyle;           // The type of aiming style the turret should use
    [SerializeField]                    Vector3 m_posOffset;                    // The offset from the turret point to correctly place this turret instance
    
    //Shot stats
    [SerializeField,Range(1, 50)]       int m_shotsPerVolley = 1;               // The number of shots per fire
	[SerializeField, Range(0f, 100.0f)] float m_spreadFactor = 0;               // The amount the individual shots will spread out from the target direction
	[SerializeField]                    bool m_evenSpread = true;               // Whether or not the shots will spread evenly away from the target direction
    [SerializeField]                    bool m_fireAllShotsAtOnce = true;       // Whether all shots are fired together, or sequentially
    [SerializeField, Range(0.0f, 5.0f)] float m_sequentialFireTime = 0.5f;      // How long the total firing sequence will take to complete
    [SerializeField]                    bool m_singleFirePointPerShot = true;   // Whether or not all shots in a volley come from the same point
    
    //Beam stats
	[SerializeField]                    bool m_isBeam;                          // Whether or not the bullet to be fired is a beam
	[SerializeField, Range(0.0f, 5.0f)] float m_beamRechargeDelay = 1.0f;       // The delay between ceasing fire with a beam, and the beam beginning to recharge
    
    /* Internal members */
    
    
    Vector3 m_flakFiringOffset = Vector3.zero;
    public bool isForwardFacing;
    int m_inTurretSlotNum = -1;
    
    
    // Track fire state
    GameObject target = null;
    EnemyScript m_targetEnemyScript = null;
    float m_currentReloadTime = 0.0f;
    int currentFirePoint = 0;
    bool coroutineHasFinished = true;
    
    // Beam/Laser stats
    bool isBeaming = false;
    float m_currentRechargeDelay = 0.0f;
    GameObject[] currentBeams = null;
    
	/* Getters/Setters */
    public bool IsForwardFacing()
    {
        return isForwardFacing;
    }
    public void SetForwardFacing(bool forward)
    {
        isForwardFacing = forward;
    }
    
	public float GetRecoilTime()
	{
		return m_recoilTime;
	}

	public int GetNumCannons()
	{
		return m_cannons.Length;
	}

    /* Unity functions */
	void Start () 
	{
		m_currentReloadTime = m_recoilTime;
		currentBeams = new GameObject[m_shotsPerVolley];
	}
	
	void Update () 
	{
		if(Network.isServer)
		{
			if(m_isBeam)
			{
				if(!isBeaming)
				{
					if(m_currentRechargeDelay >= m_beamRechargeDelay)
					{
						if(m_currentReloadTime < m_recoilTime)
							m_currentReloadTime += Time.deltaTime;
					}
					else
						m_currentRechargeDelay += Time.deltaTime;
				}
				else
				{
					m_currentReloadTime -= Time.deltaTime;
				}
			}
			else
			{
				if(m_currentReloadTime < m_recoilTime)
					m_currentReloadTime += Time.deltaTime;
			}
			
			if(target == null || target.GetComponent<HealthScript>() == null)
			{
				if(m_isBeam && isBeaming)
				{
					StopFiringBeam();
				}

				//Rotate towards forward
                RotateTowards(transform.position + transform.parent.up);
				
				//Look for enemy
				//Only look for enemy layer
				SetTarget(FindClosestTarget (out m_targetEnemyScript));
			}
			else
			{
				switch(m_turretAimStyle)
				{
					case AimingStyle.None:
					{
						//The turret does not need to rotate. Do nothing.
						break;
					}
					case AimingStyle.Basic:
					{
						RotateTowards(target.transform.position);
						bool didFire;
						CheckTargetsAndFire(target.transform.position, out didFire);
						break;
					}
					case AimingStyle.Leading:
					{
						//Similar to basic, but predict target position at time of impact
						float timeTakenToTravel = Vector3.Distance(target.transform.position, this.transform.position) / bulletRef.GetComponent<BasicBulletScript>().GetBulletSpeed();
						Vector3 predictedTargetPos = target.transform.position + (target.rigidbody.velocity * timeTakenToTravel);

						RotateTowards(predictedTargetPos);
						bool didFire;
						CheckTargetsAndFire(predictedTargetPos, out didFire);
						break;
					}
					case AimingStyle.EdgeExplode:
					{
						Vector3 targetPos = target.transform.position + m_flakFiringOffset;
						RotateTowards(targetPos);
						bool didFire;
						CheckTargetsAndFire(targetPos, out didFire);
						if(didFire)
						{
							//If we fired, we should get a new randomOffset
							Vector3 randOffset = Random.insideUnitCircle * bulletRef.GetComponent<BasicBulletScript>().GetAoERange();
							m_flakFiringOffset = randOffset;
						}
						break;
					}
					case AimingStyle.Beam:
					{
						RotateTowards(target.transform.position);
						//Ensure we are beam type
						if(m_isBeam)
						{
							if(isBeaming)
							{
								//If we're already firing, check our charge level
								if(m_currentReloadTime < (m_recoilTime * 0.25f))
								{
									//If we're below 1/4, stop firing
									StopFiringBeam();
								}
							}
							else
							{
								//If we're ready to fire	
								if(m_currentReloadTime >= m_recoilTime)
								{
									bool fire = false;
									CheckTargetsAndFire(target.transform.position, out fire);
								}
							}
						}
						break;
					}
				}
			}
		}
	}

    /* Custom functions */
    public void RotateTowards(Vector3 targetPosition)
    {
        Vector2 targetDirection = targetPosition - transform.parent.position;
        float idealAngle = Mathf.Rad2Deg * (Mathf.Atan2(targetDirection.y, targetDirection.x) - Mathf.PI / 2);
        float currentAngle = transform.parent.rotation.eulerAngles.z;

        if (Mathf.Abs(Mathf.DeltaAngle(idealAngle, currentAngle)) > 0.1f && true) /// turn to false to use old rotation movement
        {
            float nextAngle = Mathf.MoveTowardsAngle(currentAngle, idealAngle, m_rotateSpeed * Time.deltaTime);
            transform.parent.rotation = Quaternion.Euler(new Vector3(0.0f, 0.0f, nextAngle));
        }
        else
        {
            Quaternion rotate = Quaternion.LookRotation(targetDirection, Vector3.back);
            rotate.x = 0;
            rotate.y = 0;

            transform.parent.rotation = Quaternion.Slerp(transform.parent.rotation, rotate, m_rotateSpeed / 50 * Time.deltaTime);
        }
    }

	void CheckTargetsAndFire(Vector3 targetPos, out bool didFire, bool enemyOverEnemy = true)
	{
		didFire = false;
		if (IsObjectInWeaponRange (targetPos))
		{
			if(m_currentReloadTime >= m_recoilTime)
			{
				Fire ();
				didFire = true;
			}
			
			// If we're aiming at an asteroid, see if any enemies have approached
            GameObject enemyTarget = m_targetEnemyScript ? m_targetEnemyScript.GetTarget() : null; 
            if (target.tag == "Asteroid")
            {
                GameObject newTarget = FindClosestTarget(out m_targetEnemyScript, true);

                // Check if a new target has been found
                if (newTarget)
                {
                    SetTarget(newTarget);
                }
            }
            else if (enemyOverEnemy && m_targetEnemyScript && enemyTarget && enemyTarget.tag != "Capital")
            {
                //If we're aiming for an enemy, see if a closer enemy is present
                EnemyScript copy = m_targetEnemyScript;
                GameObject newTarget = FindClosestTarget(out m_targetEnemyScript, true, true);

                // Check if a new target has been found
                if (newTarget && (Vector3.SqrMagnitude(newTarget.transform.position - transform.position) < Vector3.SqrMagnitude(target.transform.position - transform.position)))
                {
                    SetTarget(newTarget);
                }
                else
                {
                    m_targetEnemyScript = copy;
                }
            }
		}
		else
		{
			//If target is out of range, see if there's a closer target
			SetTarget(FindClosestTarget (out m_targetEnemyScript));
		}
	}
	
	bool IsObjectInWeaponRange (GameObject other, float rangeLimiter = 0.8f)
	{
		// Compare squared distance for the sake of performance
		float distance = Vector3.Distance(other.transform.position, transform.position);
		float weaponRange = m_isBeam ?
										bulletRef.GetComponent<BeamBulletScript>().m_beamLength :
										bulletRef.GetComponent<BasicBulletScript>().CalculateMaxDistance();
		
		Debug.DrawLine (transform.position, transform.position + transform.up * weaponRange * rangeLimiter, Color.cyan);
		rangeLimiter = Mathf.Clamp (rangeLimiter, 0f, 1f);
		return weaponRange * rangeLimiter >= distance ? true : false;
	}
	bool IsObjectInWeaponRange(Vector3 pos, float rangeLimiter = 0.8f)
	{
		// Compare squared distance for the sake of performance
		float distance = Vector3.Distance(pos, transform.position);
		float weaponRange = m_isBeam ?
										bulletRef.GetComponent<BeamBulletScript>().m_beamLength :
										bulletRef.GetComponent<BasicBulletScript>().CalculateMaxDistance();
		
		Debug.DrawLine (transform.position, transform.position + transform.up * weaponRange * rangeLimiter, Color.cyan);
		rangeLimiter = Mathf.Clamp (rangeLimiter, 0f, 1f);
		return weaponRange * rangeLimiter >= distance ? true : false;
	}
	
	void Fire()
	{
		if(m_isBeam)
		{
			FireBeam ();
		}
		else
		{
			FireBullet();
		}
	}

	void FireBullet()
	{
		float spreadIncrement = (m_spreadFactor * 2) / m_shotsPerVolley;
		if(m_fireAllShotsAtOnce)
		{
			for(int i = 0; i < m_shotsPerVolley; i++)
			{
				SpawnBullet(i, spreadIncrement);
				
				if(!m_singleFirePointPerShot)
				{
					RecoilAndChangeBarrel();
				}
			}
			
			if(m_singleFirePointPerShot)
			{
				RecoilAndChangeBarrel();
			}

			m_currentReloadTime = 0;
		}
		else
		{
			if(coroutineHasFinished)
			{
				m_currentReloadTime = 0;
				StartCoroutine(SequentialFireLoop(m_sequentialFireTime, m_shotsPerVolley, spreadIncrement));
			}
		}
	}

	
	void FireBeam()
	{
		for(int i = 0; i < m_shotsPerVolley; i++)
		{
			if(!isBeaming && currentBeams[i] == null)
			{
				Quaternion bulletRot = this.transform.rotation;
				float length = bulletRef.GetComponent<BeamBulletScript>().m_beamLength;

				GameObject bullet = (GameObject)Network.Instantiate(bulletRef, this.transform.position, bulletRot, 0);
				bullet.transform.parent = this.transform.parent;
				bullet.transform.localScale = new Vector3(bullet.transform.localScale.x, length, bullet.transform.localScale.z);
				bullet.GetComponent<BeamBulletScript>().SetOffset(new Vector3(0, (length * 0.5f), 0));
				bullet.GetComponent<BeamBulletScript>().firer = transform.parent.gameObject;
				bullet.GetComponent<BeamBulletScript>().ParentBeamToCShipTower(m_inTurretSlotNum);

				//GameStateController gsc = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameStateController>();
				//bullet.GetComponent<BeamBulletScript>().ParentBeamToFirer(gsc.GetNameFromNetworkPlayer(transform.parent.GetComponent<PlayerControlScript>().GetOwner()));
				currentBeams[i] = bullet;
			}
		}
		isBeaming = true;
	}
    
	void StopFiringBeam()
	{
		if(isBeaming && currentBeams != null)
		{
			for(int i = 0; i < m_shotsPerVolley; i++)
			{
				if(currentBeams[i] != null)
				{
					Network.Destroy(currentBeams[i]);
				}
			}

			isBeaming = false;
			m_currentRechargeDelay = 0.0f;
		}
	}

	void SpawnBullet(int bulletNum, float spreadInc)
	{
		float randAngle;
		if(m_evenSpread)
			randAngle = -m_spreadFactor + (spreadInc * bulletNum);
		else
			randAngle = Random.Range(-m_spreadFactor, m_spreadFactor + 1.0f);
		
		Quaternion bulletRot = this.transform.rotation * Quaternion.Euler(0, 0,randAngle);
		Vector3 pos = m_firePoints[currentFirePoint].position;
		pos.z -= 0.2f;
		GameObject bullet = (GameObject)Network.Instantiate(bulletRef, pos, bulletRot, 0);
		bullet.GetComponent<BasicBulletScript>().SetFirer(this.transform.parent.gameObject);
	}

	
	IEnumerator SequentialFireLoop(float m_time, int m_shots, float m_spreadIncrement)
	{
		coroutineHasFinished = false;

		float t = 0;
		bool isDone = false;
		int i = 0;
		while(!isDone)
		{
			t += Time.deltaTime;

			if(t >= (m_time / m_shots))
			{
				//Fire a shot
				SpawnBullet(i, m_spreadIncrement);

				if(!m_singleFirePointPerShot)
				{
					SwitchToNextFirePoint();
				}
				i++;
				t = 0;

				if(i >= m_shots)
					isDone = true;
			}

			yield return 0;
		}

		coroutineHasFinished = true;
	}

	[RPC] void PropagateRecoil()
	{
		m_cannons[currentFirePoint].GetComponent<BarrelScript>().Recoil();
		SwitchToNextFirePoint();
	}

	void RecoilAndChangeBarrel()
	{
		if(m_cannons != null)
		{
			if( currentFirePoint < m_cannons.Length && m_cannons[currentFirePoint] != null)
			{
				m_cannons[currentFirePoint].GetComponent<BarrelScript>().Recoil();
				networkView.RPC ("PropagateRecoil", RPCMode.Others);
				SwitchToNextFirePoint();
			}
			else
			{
				//If there are cannons, but not for this FP, recoil barrel 1
				m_cannons[0].GetComponent<BarrelScript>().Recoil();
				networkView.RPC ("PropagateRecoil", RPCMode.Others);
				SwitchToNextFirePoint();
			}
		}
		else
		{
			//If no cannons, no recoil!
			SwitchToNextFirePoint();
		}
	}
    
	void SwitchToNextFirePoint()
	{
		currentFirePoint++;
		if(currentFirePoint >= m_firePoints.Length)
			currentFirePoint = 0;
	}
	
	GameObject FindClosestTarget (out EnemyScript enemyScript, bool enemyOnly = false, bool targettingShipOnly = false)
	{
		int layerMask = enemyOnly ? (1 << Layers.enemy) | (1 << Layers.enemyCollide) : (1 << Layers.enemy) | (1 << Layers.asteroid) | (1 << Layers.enemyDestructibleBullet) | (1 << Layers.enemyCollide);
        GameObject[] enemies = transform.root.GetComponent<CapitalShipScript>().RequestTargets(layerMask).ToArray();
        float shortestDist = Mathf.Pow(999, 2);
        GameObject closestNME = null;
        bool closestEnemyTargettingShip = false;

        foreach (GameObject enemy in enemies)
        {
            // Calculate distance
            float dist = Vector3.SqrMagnitude(enemy.transform.position - this.transform.position);

            // Prioritise enemies over asteroids
            if (!closestNME || dist < shortestDist || (closestNME.layer == Layers.asteroid && enemy.layer == Layers.enemy))
            {
                // Replace only if the ship requests a target which is attacking them
                if (closestNME && targettingShipOnly)
                {
                    if (enemy.layer == Layers.enemy)
                    {
                        EnemyScript script = enemy.GetComponent<EnemyScript>();
                        GameObject enemyTarget = script.GetTarget();
                        if ((enemyTarget && enemyTarget.layer == Layers.capital) || !closestEnemyTargettingShip)
                        {
                            shortestDist = dist;
                            closestNME = enemy;
                            closestEnemyTargettingShip = true;
                        }
                    }
                }

                // Nothing special needs to be done here
                else
                {
                    shortestDist = dist;
                    closestNME = enemy;
                }
            }
        }
        if (closestNME)
            enemyScript = closestNME.GetComponent<EnemyScript>();
		else
			enemyScript = null;
        return closestNME;
	}

    public void SetTarget(GameObject target_)
    {
        CapitalShipScript CShip = transform.root.GetComponent<CapitalShipScript>();
        if(target != null)
        {
            CShip.UnclaimTarget(target);
        }

        if(target_ != null)
        {
            CShip.ClaimTarget(target_);
        }
        
        target = target_;
    }
	
	public void ParentThisWeaponToCShip(int location)
	{
		m_inTurretSlotNum = location;
		networkView.RPC ("PropagateParentToLocation", RPCMode.All, location);
	}
    
	[RPC] public void PropagateParentToLocation(int location)
	{
		m_inTurretSlotNum = location;
		GameObject cship = GameObject.FindGameObjectWithTag("Capital");
		this.transform.parent = cship.GetComponent<CapitalShipScript>().GetCTurretHolderWithId(location).transform;
		this.transform.localPosition = m_posOffset;
	}

	void OnSerializeNetworkView(BitStream stream, NetworkMessageInfo info)
	{
		float zRot = this.transform.rotation.eulerAngles.z;
		
		if(stream.isWriting)
		{
			stream.Serialize(ref zRot);
		}
		else
		{
			stream.Serialize(ref zRot);
			this.transform.rotation = Quaternion.Euler(0, 0, zRot);
		}
	}

}

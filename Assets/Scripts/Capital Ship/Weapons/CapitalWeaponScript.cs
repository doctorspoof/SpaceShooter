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
    [SerializeField]                    AimingStyle m_turretAimStyle;           // The type of aiming style the turret should use
    [SerializeField]                    Vector3 m_posOffset;                    // The offset from the turret point to correctly place this turret instance
    
    /* Internal members */
    public bool m_isForwardFacing;
    int m_inTurretSlotNum = -1;
    
    // Track fire state
    [SerializeField] GameObject m_target = null;
    ShipEnemy m_targetShipEnemy = null;
    
	/* Getters/Setters */
    public bool IsForwardFacing()
    {
        return m_isForwardFacing;
    }
    public void SetForwardFacing(bool forward)
    {
        m_isForwardFacing = forward;
    }

    /* Unity functions */
	void Start () 
	{

	}
	
	void Update () 
	{
		if(Network.isServer)
		{
			if(m_target == null || m_target.GetComponent<HealthScript>() == null)
			{

				//Rotate towards forward
                RotateTowards(transform.position + transform.parent.up);
				
				//Look for enemy
				//Only look for enemy layer
				SetTarget(FindClosestTarget (out m_targetShipEnemy));
			}
			else
			{
                float distToTarget = Vector3.Distance(m_target.transform.position, transform.position);
                if(distToTarget < GetComponent<EquipmentTypeWeapon>().GetBulletRange())
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
    						RotateTowards(m_target.transform.position);
                            Fire ();
    						break;
    					}
    					case AimingStyle.Leading:
    					{
    						//Similar to basic, but predict target position at time of impact
                            float timeTakenToTravel = Vector3.Distance(m_target.transform.position, this.transform.position + transform.root.rigidbody.velocity) / GetComponent<EquipmentTypeWeapon>().GetBulletRange();
    						Vector3 predictedTargetPos = m_target.transform.position + (m_target.rigidbody.velocity * timeTakenToTravel);
    
    						RotateTowards(predictedTargetPos);
                            Fire();
    						break;
    					}
    					case AimingStyle.EdgeExplode:
    					{
                            Debug.Log ("EdgeExplode is deprecated");
    						break;
    					}
    					case AimingStyle.Beam:
    					{
    						RotateTowards(m_target.transform.position);
                            Fire ();
    						break;
    					}
    				}
                }
                else
                {
                    m_target = null;
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

	bool IsObjectInWeaponRange (GameObject other, float rangeLimiter = 0.8f)
	{
		// Compare squared distance for the sake of performance
		float distance = Vector3.Distance(other.transform.position, transform.position);
		float weaponRange = GetComponent<EquipmentTypeWeapon>().GetBulletRange();
		
		Debug.DrawLine (transform.position, transform.position + transform.up * weaponRange * rangeLimiter, Color.cyan);
		rangeLimiter = Mathf.Clamp (rangeLimiter, 0f, 1f);
		return weaponRange * rangeLimiter >= distance ? true : false;
	}
	bool IsObjectInWeaponRange(Vector3 pos, float rangeLimiter = 0.8f)
	{
		// Compare squared distance for the sake of performance
		float distance = Vector3.Distance(pos, transform.position);
        float weaponRange = GetComponent<EquipmentTypeWeapon>().GetBulletRange();
		
		Debug.DrawLine (transform.position, transform.position + transform.up * weaponRange * rangeLimiter, Color.cyan);
		rangeLimiter = Mathf.Clamp (rangeLimiter, 0f, 1f);
		return weaponRange * rangeLimiter >= distance ? true : false;
	}
	
	void Fire()
	{
        GetComponent<EquipmentTypeWeapon>().MobRequestsFire();
	}
    
	void StopFiringBeam()
	{
        GetComponent<EquipmentTypeWeapon>().PlayerReleaseFire();
	}
	
	GameObject FindClosestTarget (out ShipEnemy ShipEnemy, bool enemyOnly = false, bool targettingShipOnly = false)
	{
		//int layerMask = enemyOnly ? (1 << Layers.enemy) | (1 << Layers.enemyCollide) : (1 << Layers.enemy) | (1 << Layers.asteroid) | (1 << Layers.enemyDestructibleBullet) | (1 << Layers.enemyCollide);
        int layerMask = enemyOnly ? Layers.GetLayerMask(gameObject.layer, MaskType.Targetting) : Layers.GetLayerMask(gameObject.layer);
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
                        ShipEnemy script = enemy.GetComponent<ShipEnemy>();
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
            ShipEnemy = closestNME.GetComponent<ShipEnemy>();
		else
			ShipEnemy = null;
        return closestNME;
	}

    public void SetTarget(GameObject target_)
    {
        CapitalShipScript CShip = transform.root.GetComponent<CapitalShipScript>();
        if(m_target != null)
        {
            CShip.UnclaimTarget(m_target);
        }

        if(target_ != null)
        {
            CShip.ClaimTarget(target_);
        }
        
        m_target = target_;
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
        if(cship != null)
        {
    		this.transform.parent = cship.GetComponent<CapitalShipScript>().GetCTurretHolderWithID(location).transform;
    		this.transform.localPosition = m_posOffset;
        }
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

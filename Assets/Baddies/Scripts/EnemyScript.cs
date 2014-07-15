﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum Order
{
    Idle = 0,
    Move = 1,
    Attack = 2,
    Explore = 3,
    Protect = 4
}

public enum ShipSize
{
    Utility = 0,
    Large = 1,
    Medium = 2,
    Small = 3
}

public class EnemyScript : Ship
{

    [SerializeField]
    int m_bountyAmount = 1;
    public int BountyAmount
    {
        get { return m_bountyAmount; }
    }

    [SerializeField]
    bool m_hasTurrets = false;

    EnemyGroup m_parentGroup;

    GameObject m_target;
    public GameObject GetTarget()
    {
        return m_target;
    }
    Vector2 m_moveTarget;
    [SerializeField]
    Order m_currentOrder = Order.Idle;
    public Order CurrentOrder
    {
        get { return m_currentOrder; }
    }

    //Vector3 lastFramePosition;
    //public Vector3 LastFramePosition
    //{
    //    get { return lastFramePosition; }
    //    set { lastFramePosition = value; }
    //}

    Vector2 formationPosition;
    public Vector2 FormationPosition
    {
        get { return formationPosition; }
        set { formationPosition = value; }
    }
    public Vector2 GetWorldCoordinatesOfFormationPosition(Vector2 targetLocation)
    {
        return (Vector2)(m_parentGroup.transform.rotation * formationPosition) + targetLocation;
    }
    public Vector2 GetLocalFormationPosition()
    {
        return m_parentGroup.transform.rotation * formationPosition;
    }

    [SerializeField]
    string[] allowedAttacksForShip;
    IAttack currentAttackType = null;
    float randomOffsetFromTarget = 0;

    //[SerializeField]
    //List<IAttack> attackVariations;
    //public IAttack GetRandomAttack()
    //{
    //    return attackVariations[UnityEngine.Random.Range(0, attackVariations.Count)];
    //}

    [SerializeField]
    ShipSize shipSize;

    /// <summary>
    /// Returns ship type.
    /// </summary>
    public ShipSize ShipSize
    {
        get { return shipSize; }
    }

    /// <summary>
    /// Sets the group the ship belongs to.
    /// </summary>
    /// <param name="group"></param>
    public void SetParentGroup(EnemyGroup group)
    {
        m_parentGroup = group;
    }

    public bool CancelOrder()
    {
        m_target = null;
        m_currentOrder = Order.Idle;
        return true;
    }

    public void SetMoveTarget(Vector2 target)
    {
        if (Vector3.Distance((Vector2)transform.position, target) < 0.8f)
        {
            return;
        }

        m_moveTarget = target;
        m_currentOrder = Order.Move;
    }

    public Vector2 GetMoveTarget()
    {
        return m_moveTarget;
    }

    public void SetTarget(GameObject target)
    {
        m_target = target;

        foreach (GameObject turret in GetAttachedTurrets())
        {
            EnemyTurretScript turretScript = turret.GetComponent<EnemyTurretScript>();
            turretScript.SetTarget(m_target);
        }

        m_currentOrder = Order.Attack;
    }

    public void AlertLowHP(GameObject lastHit)
    {
        //if (lastHit.tag == "Capital")
        //{
        //    m_currentIntention = IntentionAI.KamikazeAttackCapitalShip;
        //}
        //else if (lastHit.tag == "Player")
        //{
        //    m_currentIntention = IntentionAI.KamikazeAttackPlayerShip;
        //}
    }
    public void AlertFirstHit(GameObject shooter)
    {
        //If we're already determined or kamikaze, ignore retaliation
        //if (m_currentIntention == IntentionAI.AttackCapitalShip || m_currentIntention == IntentionAI.AttackPlayerShip)
        //{
        //    if (shooter != null && shooter.tag == "Capital")
        //    {
        //        m_currentIntention = IntentionAI.DeterminedAttackCapitalShip;
        //        m_target = shooter;
        //    }
        //    else if (shooter != null && shooter.tag == "Player")
        //    {
        //TODO

        //If we're struck by player, check how many allies are nearby
        //Collider[] allies = GetAlliesInRange(10);
        //if(allies.Length > 5)
        //{
        //    //If we have 5 buddies nearby, split into two groups
        //    int endI = (int)(allies.Length / 2);
        //    for(int i = 0; i < (int)(allies.Length / 2); i++)
        //    {
        //        //First group should attack the CShip
        //        allies[i].gameObject.GetComponent<EnemyScript>().BeCommandedToAttackCShip();
        //    }
        //    for(int i = endI; i < (int)(allies.Length); i++)
        //    {
        //        allies[i].gameObject.GetComponent<EnemyScript>().BeCommandedToAttackPlayer(shooter);
        //    }

        //    //Join the attack on the player
        //    this.BeCommandedToAttackPlayer(shooter);
        //}
        //else
        //{
        //    if(m_hasTurrets)
        //    {
        //        m_currentIntention = IntentionAI.DeterminedCapitalShootingPlayer;
        //        m_secondaryTarget = shooter;
        //        GameObject[] turrets = GetAttachedTurrets();
        //        foreach(GameObject turret in turrets)
        //        {
        //            turret.GetComponent<EnemyTurretScript>().SetTarget(m_secondaryTarget);
        //        }
        //    }
        //    else
        //    {
        //        //Otherwise, retaliate against player
        //        m_currentIntention = IntentionAI.AttackPlayerShip;
        //        m_target = shooter;
        //    }
        //}
        //    }
        //}
    }

    protected override void Awake()
    {
        Init();
    }

    // Use this for initialization
    void Start()
    {
        shipTransform = transform;
        ResetThrusters();

        //lastFramePosition = shipTransform.position;
        currentAttackType = AIAttackCollection.GetAttack(allowedAttacksForShip[Random.Range(0, allowedAttacksForShip.Length)]);
        randomOffsetFromTarget = Random.Range(-GetMinimumWeaponRange(), GetMinimumWeaponRange());
        ResetShipSpeed();
    }

    public void NotifyEnemyUnderFire(GameObject attacker)
    {
        m_parentGroup.CancelAllOrders();
        m_parentGroup.OrderAttack(attacker.transform.root.gameObject);
    }

    bool isGoingLeft = false;
    // Update is called once per frame
    protected override void Update()
    {
        //m_shipSpeed = 0;

        if (Network.isServer)
        {
            base.Update();

            //EnemyScript slowestShip = m_parentGroup.GetSlowestShip();

            switch (m_currentOrder)
            {
                case Order.Idle:
                    {

                        //if (!InFormation(2f))
                        //{
                        //    currentlyMovingToPosition = true;
                        //}
                        //else if (InFormation(0.8f))
                        //{
                        //    currentlyMovingToPosition = false;
                        //}

                        //if (currentlyMovingToPosition)
                        //{
                        //    MoveToFormation();
                        //}
                        break;
                    }
                case Order.Move:
                    {
                        MoveTowardTarget();

                        if (Vector3.SqrMagnitude((Vector2)shipTransform.position - m_moveTarget) < 0.64f || m_parentGroup.HasGroupArrivedAtLocation())
                        {
                            m_currentOrder = Order.Idle;
                        }

                        break;
                    }
                case Order.Attack:
                    {
                        if (m_target == null)
                        {
                            Debug.Log("target is null");
                            m_currentOrder = Order.Idle;
                            break;
                        }
                        
                        Vector3 direction = Vector3.Normalize(m_target.transform.position - shipTransform.position);
                        Ray ray = new Ray(shipTransform.position, direction);

                        float shipDimension = 0;
                        Ship targetShip = m_target.GetComponent<Ship>();
                        if (targetShip != null)
                        {
                            shipDimension = targetShip.GetCalculatedSizeByPosition(shipTransform.position);
                        }

                        float minWeaponRange = GetMinimumWeaponRange();

                        float totalRange = minWeaponRange <= shipDimension ? minWeaponRange + shipDimension : minWeaponRange;

                        RaycastHit hit;
                        if (!m_target.collider.Raycast(ray, out hit, totalRange))
                        {
                            Vector2 normalOfDirection = GetNormal(direction);

                            RotateTowards((Vector2)m_target.transform.position + (randomOffsetFromTarget * normalOfDirection));

                            rigidbody.AddForce(shipTransform.up * GetCurrentMomentum() * Time.deltaTime);
                        }
                        else
                        {
                            currentAttackType.Attack(this, m_target);
                        }

                        break;
                    }
                case Order.Explore:
                    {
                        break;
                    }
                case Order.Protect:
                    {
                        break;
                    }
            }

        }
    }

    int sendCounter = 0;
    float prevZRot = 0.0f;
    void OnSerializeNetworkView(BitStream stream, NetworkMessageInfo info)
    {
        sendCounter++;

        //Handle positions manually
        float posX = shipTransform.position.x;
        float posY = shipTransform.position.y;

        float rotZ = shipTransform.rotation.eulerAngles.z;

        Vector3 velocity = rigidbody.velocity;

        if (stream.isWriting)
        {
            if (sendCounter >= 2)
            {
                sendCounter = 0;
                //We're the owner, send our info to other people
                stream.Serialize(ref posX);
                stream.Serialize(ref posY);
                stream.Serialize(ref rotZ);
                stream.Serialize(ref velocity);
            }
        }
        else
        {
            //We're recieving info for this mob
            prevZRot = rotZ;

            stream.Serialize(ref posX);
            stream.Serialize(ref posY);
            stream.Serialize(ref rotZ);
            stream.Serialize(ref velocity);

            shipTransform.position = new Vector3(posX, posY, 10.0f);
            shipTransform.rotation = Quaternion.Euler(0, 0, rotZ);
            rigidbody.velocity = velocity;

            StartCoroutine(BeginInterp());
        }
    }
    float t = 0;
    IEnumerator BeginInterp()
    {
        t = 0;
        while (t < 0.4f)
        {
            t += Time.deltaTime;
            rigidbody.MovePosition(rigidbody.position + (rigidbody.velocity * Time.deltaTime * Time.deltaTime));
            yield return 0;
        }
    }


    public int GetBounty()
    {
        return m_bountyAmount;
    }

    public void OnPlayerWin()
    {
        //GameObject winPoint = GameObject.FindGameObjectWithTag("CSTarget");
        //m_target = winPoint;
    }
    public void OnPlayerLoss()
    {
        //m_shouldStop = true;
    }
    public void TellEnemyToFreeze()
    {
        //m_shouldStop = true;
    }
    public void AlertEnemyUnFreeze()
    {
        //m_shouldStop = false;
    }

    public GameObject[] GetAttachedTurrets()
    {
        List<GameObject> turrets = new List<GameObject>();
        foreach (Transform child in transform)
        {
            if (child.tag == "EnemyTurret")
            {
                turrets.Add(child.gameObject);
            }
        }

        return turrets.ToArray();
    }

    public Collider[] GetAlliesInRange(float range)
    {
        int layerMask = 1 << 11;
        return Physics.OverlapSphere(shipTransform.position, range, layerMask);
    }

    public override float GetMinimumWeaponRange()
    {
        //float weaponRange = 0;
        EnemyWeaponScript enemyWeaponScript;
        if ((enemyWeaponScript = GetComponent<EnemyWeaponScript>()) != null)
        {
            weaponRange = enemyWeaponScript.GetRange();
            return weaponRange;
        }

        GameObject[] turrets = GetAttachedTurrets();
        if (turrets != null)
        {
            foreach (GameObject turret in GetAttachedTurrets())
            {
                EnemyTurretScript turretScript = turret.GetComponent<EnemyTurretScript>();
                if (turretScript != null && (weaponRange == 0 || turretScript.GetRange() < weaponRange))
                {
                    weaponRange = turretScript.GetRange();
                }
            }
            return weaponRange;
        }

        return weaponRange;
    }

    private void MoveTowardTarget()
    {
        if (Vector2.Distance(GetWorldCoordinatesOfFormationPosition(m_parentGroup.transform.position), m_moveTarget) > Vector2.Distance(shipTransform.position, m_moveTarget))
        {
            Vector2 distanceToClosestFormationPosition = GetVectorDistanceFromClosestFormation();
            Vector2 distanceToTargetPosition = (m_moveTarget - (Vector2)shipTransform.position);

            //float speed = m_parentGroup.GetSlowestShipSpeed();

            float t = Mathf.Clamp(distanceToClosestFormationPosition.magnitude, 0, 5) / 5.0f;
            Vector2 directionToMove = (distanceToTargetPosition.normalized * (1 - t)) + (distanceToClosestFormationPosition.normalized * t);

            //Debug.DrawRay(transform.position, Vector3.Normalize(directionToMove), Color.cyan);
            //Debug.DrawLine(transform.position, (Vector2)transform.position + distanceToClosestFormationPosition, Color.green);
            //Debug.DrawRay(transform.position, Vector3.Normalize(distanceToTargetPosition), Color.blue);
            //Debug.DrawLine(transform.position, GetWorldCoordinatesOfFormationPosition(m_parentGroup.transform.position));

            RotateTowards((Vector2)shipTransform.position + directionToMove);
        }
        else
        {
            RotateTowards(GetWorldCoordinatesOfFormationPosition(m_parentGroup.transform.position));
        }

        rigidbody.AddForce(shipTransform.up * GetCurrentMomentum() * Time.deltaTime);
    }

    /// <summary>
    /// Gets the closest vector distance from the current position to a valid formation position on the line from
    /// the group to the target.
    /// </summary>
    /// <returns></returns>
    public Vector2 GetVectorDistanceFromClosestFormation()
    {
        Vector2 currentGroupFormationPosition = GetWorldCoordinatesOfFormationPosition(m_parentGroup.transform.position);
        Vector2 directionFromTargetToGroupPosition = currentGroupFormationPosition - m_moveTarget;

        Vector2 normalOfGroupPosToTarget = GetNormal(directionFromTargetToGroupPosition).normalized;

        float d = -Vector2.Dot(((Vector2)shipTransform.position - currentGroupFormationPosition), normalOfGroupPosToTarget);

        //Debug.DrawLine(transform.position, (Vector2)transform.position + normalOfGroupPosToTarget, Color.red);

        return normalOfGroupPosToTarget * d;
    }

    /// <summary>
    /// Gets the closest distance from the current position to a valid formation position on the line from
    /// the group to the target.
    /// </summary>
    /// <returns></returns>
    public float GetDistanceFromClosestFormation()
    {
        return GetVectorDistanceFromClosestFormation().magnitude;
    }

    public float GetDistanceFromFormation()
    {
        return Vector2.Distance(shipTransform.position, GetWorldCoordinatesOfFormationPosition(m_parentGroup.transform.position));
    }

    public Vector2 GetNormal(Vector2 direction)
    {
        return new Vector2(direction.y, -direction.x);
    }

    public bool InFormation(float distance)
    {
        return Vector2.Distance(shipTransform.position, GetWorldCoordinatesOfFormationPosition(m_parentGroup.transform.position)) < distance;
    }

    void OnDestroy()
    {

        if (m_parentGroup != null)
            m_parentGroup.RemoveEnemyFromGroup(this);
    }

	//Do shield fizzle wizzle
	int shaderCounter = 0;
	public void BeginShaderCoroutine(Vector3 position, int type, float magnitude)
	{
		//Debug.Log ("Bullet collision, beginning shader coroutine");
		Vector3 pos = this.transform.InverseTransformPoint(position);
		pos = new Vector3(pos.x * transform.localScale.x, pos.y * transform.localScale.y, pos.z);
		GetShield().renderer.material.SetVector("_ImpactPos" + (shaderCounter + 1).ToString(), new Vector4(pos.x, pos.y, pos.z, 1));
		GetShield().renderer.material.SetFloat("_ImpactTime" + (shaderCounter + 1).ToString(), 1.0f);
		GetShield().renderer.material.SetInt("_ImpactTypes" + (shaderCounter + 1).ToString(), type);
		GetShield().renderer.material.SetFloat("_ImpactMagnitude" + (shaderCounter + 1).ToString(), magnitude);
		
		StartCoroutine(ReduceShieldEffectOverTime(shaderCounter));
		
		++shaderCounter;
		if(shaderCounter >= 4)
			shaderCounter = 0;
	}
	public void BeginShaderCoroutine(Vector3 position)
	{
		//Debug.Log ("Bullet collision, beginning shader coroutine");
		Vector3 pos = this.transform.InverseTransformPoint(position);
		pos = new Vector3(pos.x * transform.localScale.x, pos.y * transform.localScale.y, pos.z);
		GetShield().renderer.material.SetVector("_ImpactPos" + (shaderCounter + 1).ToString(), new Vector4(pos.x, pos.y, pos.z, 1));
		GetShield().renderer.material.SetFloat("_ImpactTime" + (shaderCounter + 1).ToString(), 1.0f);
		GetShield().renderer.material.SetInt("_ImpactTypes" + (shaderCounter + 1).ToString(), 0);
		GetShield().renderer.material.SetFloat("_ImpactMagnitude" + (shaderCounter + 1).ToString(), 0.0f);
		
		StartCoroutine(ReduceShieldEffectOverTime(shaderCounter));
		
		++shaderCounter;
		if(shaderCounter >= 4)
			shaderCounter = 0;
	}

    bool coroutineIsRunning = false;
    bool coroutineForceStopped = false;
    IEnumerator ReduceShieldEffectOverTime(int i)
    {
        float t = 0;
        coroutineIsRunning = true;
        //while(t <= 1.0f && coroutineIsRunning)
        while (t <= 1.0f)
        {
            t += Time.deltaTime;
            GameObject shield = GetShield();
            //float time = shield.renderer.material.GetFloat("_ImpactTime" + (i + 1).ToString());

            //oldImp.w = 1.0f - t;

            shield.renderer.material.SetFloat("_ImpactTime" + (i + 1).ToString(), 1.0f - t);
            yield return 0;
        }

        /*if(!coroutineIsRunning)
                coroutineForceStopped = true;*/

        coroutineIsRunning = false;
    }

    GameObject m_shieldCache = null;

    [SerializeField]
    string m_pathToShieldObject = "Composite Collider/Shield";

    public GameObject GetShield()
    {
        if (!m_shieldCache || m_shieldCache.tag != "Shield")
        {
            // Search child objects for the shield.
            Transform result = shipTransform.Find(m_pathToShieldObject);
            m_shieldCache = result ? result.gameObject : null;

            if (!m_shieldCache || m_shieldCache.tag != "Shield")
            {
                // Fall back to old method and search
                foreach (Transform child in shipTransform)
                {
                    if (child.tag == "Shield")
                    {
                        m_shieldCache = child.gameObject;
                    }
                }

                if (!m_shieldCache)
                {
                    Debug.LogWarning("No shield found for mob " + this.name);
                }
            }
        }

        return m_shieldCache;
    }
}

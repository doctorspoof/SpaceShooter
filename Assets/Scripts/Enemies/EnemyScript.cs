using UnityEngine;
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

    [SerializeField] int m_bountyAmount = 1;
    
    [SerializeField] bool m_hasTurrets = false;
    
    [SerializeField] Order m_currentOrder = Order.Idle;

    [SerializeField] string[] m_allowedAttacksForShip;

    [SerializeField] ShipSize m_shipSize;



    

    GameObject m_target;

    Vector2 m_moveTarget;

    Vector2 m_formationPosition;

    bool m_isGoingLeft = false;

    int m_sendCounter = 0;
    float m_prevZRot = 0.0f;



    EnemyGroup m_parentGroup;

    IAttack m_currentAttackType = null;
    float m_randomOffsetFromTarget = 0;



    #region getset

    public Vector2 GetFormationPosition()
    {
        return m_formationPosition;
    }

    public void SetFormationPosition(Vector2 formationPosition_)
    {
        m_formationPosition = formationPosition_;
    }

    public Order GetCurrentOrder()
    {
        return m_currentOrder;
    }

    public GameObject GetTarget()
    {
        return m_target;
    }

    public int GetBountyAmount()
    {
        return m_bountyAmount;
    }

    public ShipSize GetShipSize()
    {
        return m_shipSize;
    }

    public void SetParentGroup(EnemyGroup group)
    {
        m_parentGroup = group;
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

    /// <summary>
    /// Sets the target of this ship. Forwards the target onto any turrets this ship has.
    /// </summary>
    /// <param name="target"></param>
    public void SetTarget(GameObject target)
    {
        m_target = target;

        foreach (GameObject turret in GetAttachedTurrets())
        {
            EnemyTurret turretScript = turret.GetComponent<EnemyTurret>();
            turretScript.SetTarget(m_target);
        }

        m_currentOrder = Order.Attack;
    }

    public int GetBounty()
    {
        return m_bountyAmount;
    }

    #endregion getset

    

    protected override void Awake()
    {
        base.Awake();
    }

    // Use this for initialization
    void Start()
    {
        m_shipTransform = transform;
        ResetThrusters();

        //lastFramePosition = shipTransform.position;
        m_currentAttackType = AIAttackCollection.GetAttack(m_allowedAttacksForShip[Random.Range(0, m_allowedAttacksForShip.Length)]);
        m_randomOffsetFromTarget = Random.Range(-GetMinimumWeaponRange(), GetMinimumWeaponRange());
        ResetShipSpeed();
    }

    protected override void Update()
    {
        base.Update();

        if (Network.isServer)
        {

            switch (m_currentOrder)
            {
                case Order.Idle:
                    {
                        break;
                    }
                case Order.Move:
                    {
                        MoveTowardTarget();

                        if (Vector3.SqrMagnitude((Vector2)m_shipTransform.position - m_moveTarget) < 0.64f || m_parentGroup.HasGroupArrivedAtLocation())
                        {
                            m_currentOrder = Order.Idle;
                        }

                        break;
                    }
                case Order.Attack:
                    {
                        if (m_target == null)
                        {
                            m_currentOrder = Order.Idle;
                            break;
                        }

                        Vector3 direction = Vector3.Normalize(m_target.transform.position - m_shipTransform.position);
                        Ray ray = new Ray(m_shipTransform.position, direction);

                        float shipDimension = 0;
                        Ship targetShip = m_target.GetComponent<Ship>();
                        if (targetShip != null)
                        {
                            shipDimension = targetShip.GetCalculatedSizeByPosition(m_shipTransform.position);
                        }

                        float minWeaponRange = GetMinimumWeaponRange();

                        float totalRange = minWeaponRange <= shipDimension ? minWeaponRange + shipDimension : minWeaponRange;

                        RaycastHit hit;
                        if (!m_target.collider.Raycast(ray, out hit, totalRange))
                        {
                            Vector2 normalOfDirection = GetNormal(direction);

                            RotateTowards((Vector2)m_target.transform.position + (m_randomOffsetFromTarget * normalOfDirection));

                            rigidbody.AddForce(m_shipTransform.up * GetCurrentMomentum() * Time.deltaTime);
                        }
                        else
                        {
                            m_currentAttackType.Attack(this, m_target);
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

    void OnDestroy()
    {

        if (m_parentGroup != null)
            m_parentGroup.RemoveEnemyFromGroup(this);
    }

    public void AlertLowHP(GameObject lastHit)
    {

    }
    public void AlertFirstHit(GameObject shooter)
    {
        
    }

    public void NotifyEnemyUnderFire(GameObject attacker)
    {
        if (m_parentGroup != null)
        {
            m_parentGroup.CancelAllOrders();
            m_parentGroup.OrderAttack(attacker.transform.root.gameObject);
        }
    }

    
    void OnSerializeNetworkView(BitStream stream, NetworkMessageInfo info)
    {
        m_sendCounter++;

        //Handle positions manually
        float posX = m_shipTransform.position.x;
        float posY = m_shipTransform.position.y;

        float rotZ = m_shipTransform.rotation.eulerAngles.z;

        Vector3 velocity = rigidbody.velocity;

        if (stream.isWriting)
        {
            if (m_sendCounter >= 2)
            {
                m_sendCounter = 0;
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
            m_prevZRot = rotZ;

            stream.Serialize(ref posX);
            stream.Serialize(ref posY);
            stream.Serialize(ref rotZ);
            stream.Serialize(ref velocity);

            m_shipTransform.position = new Vector3(posX, posY, 10.0f);
            m_shipTransform.rotation = Quaternion.Euler(0, 0, rotZ);
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

    public void OnPlayerWin()
    {
    }

    public void OnPlayerLoss()
    {
        
    }

    public void TellEnemyToFreeze()
    {
    }

    public void AlertEnemyUnFreeze()
    {
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

    /// <summary>
    /// Gets the lowest weapon range of all the attached weapons
    /// </summary>
    /// <returns></returns>
    public override float GetMinimumWeaponRange()
    {
        EnemyWeaponScript enemyWeaponScript;
        if ((enemyWeaponScript = GetComponent<EnemyWeaponScript>()) != null)
        {
            m_minWeaponRange = enemyWeaponScript.GetRange();
            return m_minWeaponRange;
        }

        GameObject[] turrets = GetAttachedTurrets();
        if (turrets != null)
        {
            foreach (GameObject turret in GetAttachedTurrets())
            {
                EnemyTurret turretScript = turret.GetComponent<EnemyTurret>();
                if (turretScript != null && (m_minWeaponRange == 0 || turretScript.GetRange() < m_minWeaponRange))
                {
                    m_minWeaponRange = turretScript.GetRange();
                }
            }
        }

        return m_minWeaponRange;
    }

    /// <summary>
    /// Gets the highest weapon range of all the attached weapons
    /// </summary>
    /// <returns></returns>
    public override float GetMaximumWeaponRange()
    {
        EnemyWeaponScript enemyWeaponScript;
        if ((enemyWeaponScript = GetComponent<EnemyWeaponScript>()) != null)
        {
            m_maxWeaponRange = enemyWeaponScript.GetRange();
            return m_maxWeaponRange;
        }

        GameObject[] turrets = GetAttachedTurrets();
        if (turrets != null)
        {
            foreach (GameObject turret in GetAttachedTurrets())
            {
                EnemyTurret turretScript = turret.GetComponent<EnemyTurret>();
                if (turretScript != null && (m_maxWeaponRange == 0 || turretScript.GetRange() > m_maxWeaponRange))
                {
                    m_maxWeaponRange = turretScript.GetRange();
                }
            }
        }

        return m_maxWeaponRange;
    }

    void MoveTowardTarget()
    {
        if (Vector2.Distance(GetWorldCoordinatesOfFormationPosition(m_parentGroup.transform.position), m_moveTarget) > Vector2.Distance(m_shipTransform.position, m_moveTarget))
        {
            Vector2 distanceToClosestFormationPosition = GetVectorDistanceFromClosestFormation();
            Vector2 distanceToTargetPosition = (m_moveTarget - (Vector2)m_shipTransform.position);

            float t = Mathf.Clamp(distanceToClosestFormationPosition.magnitude, 0, 5) / 5.0f;
            Vector2 directionToMove = (distanceToTargetPosition.normalized * (1 - t)) + (distanceToClosestFormationPosition.normalized * t);

            //Debug.DrawRay(transform.position, Vector3.Normalize(directionToMove), Color.cyan);
            //Debug.DrawLine(transform.position, (Vector2)transform.position + distanceToClosestFormationPosition, Color.green);
            //Debug.DrawRay(transform.position, Vector3.Normalize(distanceToTargetPosition), Color.blue);
            //Debug.DrawLine(transform.position, GetWorldCoordinatesOfFormationPosition(m_parentGroup.transform.position));

            RotateTowards((Vector2)m_shipTransform.position + directionToMove);
        }
        else
        {
            RotateTowards(GetWorldCoordinatesOfFormationPosition(m_parentGroup.transform.position));
        }

        rigidbody.AddForce(m_shipTransform.up * GetCurrentMomentum() * Time.deltaTime);
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

        float d = -Vector2.Dot(((Vector2)m_shipTransform.position - currentGroupFormationPosition), normalOfGroupPosToTarget);

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
        return Vector2.Distance(m_shipTransform.position, GetWorldCoordinatesOfFormationPosition(m_parentGroup.transform.position));
    }

    public Vector2 GetNormal(Vector2 direction)
    {
        return new Vector2(direction.y, -direction.x);
    }

    /// <summary>
    /// Determines whether this ship is within a specified distance to 
    /// </summary>
    /// <param name="distance"></param>
    /// <returns></returns>
    public bool InFormation(float distance)
    {
        return Vector2.Distance(m_shipTransform.position, GetWorldCoordinatesOfFormationPosition(m_parentGroup.transform.position)) < distance;
    }

    /// <summary>
    /// Gets the local formation position if the parent position were at the targetLocation position
    /// </summary>
    /// <param name="targetLocation"></param>
    /// <returns></returns>
    public Vector2 GetWorldCoordinatesOfFormationPosition(Vector2 targetLocation)
    {
        return (Vector2)(m_parentGroup.transform.rotation * m_formationPosition) + targetLocation;
    }

    /// <summary>
    /// Returns the formation position with regards to the rotation of the parent
    /// </summary>
    /// <returns></returns>
    public Vector2 GetLocalFormationPosition()
    {
        return m_parentGroup.transform.rotation * m_formationPosition;
    }

    public bool CancelOrder()
    {
        m_target = null;
        m_currentOrder = Order.Idle;
        return true;
    }

}

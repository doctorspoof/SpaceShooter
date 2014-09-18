using UnityEngine;
using System.Collections.Generic;

public enum AIShipOrder
{
    Idle = 0,
    Move = 1,
    Attack = 2,
    StayInFormation = 3
}

public enum AIShipRequestInfo
{
    Ship = 0,
    Move = 1,
    Target = 2
}

public enum AIShipNotifyInfo
{
    ParentChanged = 0,
    ChildAdded = 1,
    ChildRemoved = 2,
    SetFormationPosition = 3,
    Fire = 4,
    Promoted = 5
}

[RequireComponent(typeof(MeshFilter))]
public class Ship : MonoBehaviour, IEntity, ICloneable
{

    public int depth;

    [SerializeField] protected string m_ownerSt;

    [SerializeField] float m_maxShipSpeed;
    [SerializeField] float m_currentShipSpeed = 0.0f;

    [SerializeField] float m_afterburnerIncreaseOfSpeed;
    [SerializeField] float m_afterburnerLength;
    [SerializeField] float m_afterburnerRechargeTime;

    [SerializeField] float m_rotateSpeed = 5.0f;

    [SerializeField] float m_ramDamageMultiplier = 2.5f;

    [SerializeField] bool maunuallySetWidthAndHeight = false;

    [SerializeField] protected float m_minWeaponRange = 0.0f;
    [SerializeField] protected float m_maxWeaponRange = 0.0f;

    [SerializeField] float m_shipWidth;
    [SerializeField] float m_shipHeight;

    [SerializeField] bool m_special;

    [SerializeField] string m_pathToShieldObject = "Composite Collider/Shield";

    [SerializeField] AIShipOrder m_currentOrder = AIShipOrder.Idle;




    bool m_afterburnersFiring = false, m_afterburnersRecharged = true;
    float m_currentAfterburnerTime = 0.0f, m_currentAfterburnerRechargeTime = 0.0f;

    float m_currentAngularVelocity = 0;
    float m_currentRotation = 0, m_lastRotation = 0;
    float m_maxThrusterVelocitySeen = 0, m_maxAngularVelocitySeen = 0;

    int shaderCounter = 0;

    GameObject m_target = null;
    List<Vector2> m_waypoints = new List<Vector2>();


    //bool coroutineIsRunning = false;
    //bool coroutineForceStopped = false;



    int m_sendCounter = 0;

    protected Ship m_cacheParent = null;
     Vector2 m_formationPosition;

    Transform m_thrustersHolder = null, m_afterburnersHolder = null;
    Thruster[] m_thrusters = null, m_afterburners = null;

    protected NetworkPlayer m_owner;
    
    protected Transform m_shipTransform;
    protected Rigidbody m_shipRigidbody;

    GameObject m_shieldCache = null;

    AINode m_node;

    //this was mainly for testing, may be deleted eventually
#pragma warning disable 0414
    [SerializeField] public int shipID = -1;
    static int ids = 0;
#pragma warning restore 0414

    #region getset

    public bool IsSpecial()
    {
        return m_special;
    }

    public float GetMaxShipSpeed()
    {
        return m_maxShipSpeed;
    }

    public void SetMaxShipSpeed(float maxSpeed_)
    {
        m_maxShipSpeed = maxSpeed_;
    }

    public float GetCurrentShipSpeed()
    {
        return m_afterburnersFiring == true ? m_currentShipSpeed + m_afterburnerIncreaseOfSpeed : m_currentShipSpeed;
    }

    public void SetCurrentShipSpeed(float currentSpeed)
    {
        m_currentShipSpeed = Mathf.Clamp(currentSpeed, 0, m_maxShipSpeed);
    }

    /// <summary>
    /// Sets the currentShipSpeed based off a momentum value
    /// </summary>
    /// <param name="currentMomentum_"></param>
    public void SetShipMomentum(float currentMomentum_)
    {
        m_currentShipSpeed = Mathf.Min(m_maxShipSpeed, currentMomentum_ / rigidbody.mass);
    }

    public float GetMaxMomentum()
    {
        return m_maxShipSpeed * rigidbody.mass;
    }

    public float GetCurrentMomentum()
    {
        return GetCurrentShipSpeed() * rigidbody.mass;
    }

    public float GetRamDam()
    {
        return m_ramDamageMultiplier;
    }

    public float GetRotateSpeed()
    {
        return m_rotateSpeed;
    }

    public void SetRotateSpeed(float rotateSpeed_)
    {
        m_rotateSpeed = rotateSpeed_;
    }

    public float GetShipWidth()
    {
        return m_shipWidth;
    }

    public float GetShipHeight()
    {
        return m_shipHeight;
    }

    /// <summary>
    /// Returns thrusters. Do not use if the engine has changed as it will not return the updated thrusters. Use FindThrusters instead
    /// </summary>
    /// <returns></returns>
    public Thruster[] GetThrusters()
    {
        return m_thrusters;
    }

    public Thruster[] GetAfterburners()
    {
        return m_afterburners;
    }

    public virtual float GetMinimumWeaponRange()
    {
        return m_minWeaponRange;
    }

    public virtual float GetMaximumWeaponRange()
    {
        return m_maxWeaponRange;
    }

    public NetworkPlayer GetOwner()
    {
        return m_owner;
    }

    public AINode GetAINode()
    {
        return m_node;
    }

    public void SetAINode(AINode node_)
    {
        m_node = node_;
    }

    public void AddChildShip(Ship child_)
    {
        m_node.AddChild(child_.GetAINode(), true);
    }

    public List<Vector2> GetWaypoints()
    {
        return m_waypoints;
    }

    public void SetTargetMove(Vector2 target_)
    {
        if (Vector3.Distance((Vector2)transform.position, target_) < 0.8f)
        {
            return;
        }

        OrderMove(target_);

        //Debug.Log("Received move order");

        m_currentOrder = AIShipOrder.Move;
    }

    public GameObject GetTarget()
    {
        return m_target;
    }

    /// <summary>
    /// Sets the target of this ship. Forwards the target onto any turrets this ship has.
    /// </summary>
    /// <param name="target"></param>
    public void SetTarget(GameObject target)
    {
        bool moveOrderNeeded = false;

        if (Vector2.Distance(transform.position, target.transform.position) > GetMinimumWeaponRange() * 2)
        {
            Vector2 closerPosition = Vector2.MoveTowards(target.transform.position, transform.position, GetMinimumWeaponRange() * 2);
            SetTargetMove(closerPosition);
            moveOrderNeeded = true;
        }

        m_target = target;

        GameObject[] turrets = GetAttachedTurrets();
        foreach (GameObject turret in turrets)
        {
            EnemyTurret turretScript = turret.GetComponent<EnemyTurret>();
            turretScript.SetTarget(m_target);
        }

        if(!moveOrderNeeded)
        {
            m_currentOrder = AIShipOrder.Attack;
        }
    }

    public Vector2 GetFormationPosition()
    {
        return m_formationPosition;
    }

    public void SetFormationPosition(Vector2 formationPosition_)
    {
        m_formationPosition = formationPosition_;
        m_currentOrder = AIShipOrder.StayInFormation;
    }

#endregion

    protected virtual void Awake()
    {
        shipID = ids++;

        m_shipTransform = transform;
        m_shipRigidbody = rigidbody;

        if (GetShipWidth() == 0 || GetShipHeight() == 0)
            SetShipSizes();

        m_node = new AINode(this);

        m_node.onChildAdded += x =>
        {
            this.Notify((int)AIShipNotifyInfo.ChildAdded, new object[] { x });
        };

        m_node.onChildRemoved += x =>
        {
            this.Notify((int)AIShipNotifyInfo.ChildRemoved, new object[] { x });
        };

        m_node.onPromoted += delegate()
        {
            GetAINode().SendNotify(AIHierarchyRelation.Children, (int)AIShipNotifyInfo.ParentChanged, new object[] { this });
            if (GetAINode().GetParent() != null)
            {
                GetAINode().GetParent().GetEntity().RequestOrder(this);
            }
        };
    }

    protected virtual void Start()
    {
        object[] info = GetAINode().GetParent().RequestInformation((int)AIShipRequestInfo.Ship);
        if(info != null)
        {
            m_cacheParent = (Ship)info[0];
        }

        m_shipTransform = transform;
        ResetThrusters();

        ResetShipSpeed();
    }

    protected virtual void Update()
    {
        if(m_thrustersHolder == null)
        {
            ResetThrusters();
        }

        if (m_afterburnersFiring == true)
        {
            m_currentAfterburnerTime += Time.deltaTime;
            if (m_currentAfterburnerTime >= m_afterburnerLength)
            {
                AfterburnerFinished();
            }
        }

        if (m_afterburnersFiring == false)
        {
            if (m_afterburnersRecharged == false)
            {
                m_currentAfterburnerRechargeTime += Time.deltaTime;
                if (m_currentAfterburnerRechargeTime >= m_afterburnerRechargeTime)
                {
                    m_afterburnersRecharged = true;
                    m_currentAfterburnerRechargeTime = 0;
                }
            }
        }
    }

    protected virtual void FixedUpdate()
    {
        UpdateCurrentAngularVelocity();

        // we cant calculate the max velocity neatly so we check to see if its larger
        if (m_maxThrusterVelocitySeen < m_shipRigidbody.velocity.magnitude)
        {
            m_maxThrusterVelocitySeen = m_shipRigidbody.velocity.magnitude;
        }

        if (m_maxAngularVelocitySeen < Mathf.Abs(m_currentAngularVelocity))
        {
            m_maxAngularVelocitySeen = Mathf.Abs(m_currentAngularVelocity);
        }

        if (Network.isServer)
        {
            switch (m_currentOrder)
            {
                case (AIShipOrder.Attack):
                    {
                        if (m_target == null)
                        {
                            m_currentOrder = AIShipOrder.Idle;
                            break;
                        }

                        MoveTowardTarget(m_target.transform.position, GetCurrentMomentum());

                        //tell self to fire
                        Notify((int)AIShipNotifyInfo.Fire, null);
                        //tell children to fire
                        GetAINode().SendNotify(AIHierarchyRelation.Children, (int)AIShipNotifyInfo.Fire, null);

                        //Vector3 direction = Vector3.Normalize(m_target.transform.position - m_shipTransform.position);
                        //Ray ray = new Ray(m_shipTransform.position, direction);

                        //float shipDimension = 0;
                        //Ship targetShip = m_target.GetComponent<Ship>();
                        //if (targetShip != null)
                        //{
                        //    shipDimension = targetShip.GetCalculatedSizeByPosition(m_shipTransform.position);
                        //}

                        //float minWeaponRange = GetMinimumWeaponRange();

                        //float totalRange = minWeaponRange <= shipDimension ? minWeaponRange + shipDimension : minWeaponRange;

                        //RaycastHit hit;
                        //if (!m_target.collider.Raycast(ray, out hit, totalRange))
                        //{
                        //    Vector2 normalOfDirection = GetNormal(direction);

                        //    RotateTowards((Vector2)m_target.transform.position + (m_randomOffsetFromTarget * normalOfDirection));

                        //    rigidbody.AddForce(m_shipTransform.up * GetCurrentMomentum() * Time.deltaTime);
                        //}
                        //else
                        //{
                        //    m_currentAttackType.Attack(this, m_target);
                        //}
                        break;
                    }
                case (AIShipOrder.Move):
                    {
                        if (m_waypoints.Count > 0)
                        {
                            MoveTowardTarget(m_waypoints[0], GetCurrentMomentum());

                            if (Vector3.SqrMagnitude((Vector2)m_shipTransform.position - m_waypoints[0]) < 0.64f)
                            {
                                m_waypoints.RemoveAt(0);

                                if(m_waypoints.Count == 0)
                                {
                                    // If we are looking at attcking a unit, follow up by setting the current order to attack
                                    if (m_target != null)
                                    {
                                        m_currentOrder = AIShipOrder.Attack;
                                    }
                                    else
                                    {
                                        m_currentOrder = AIShipOrder.Idle;
                                    }
                                }
                            }
                        }
                        break;
                    }
                case (AIShipOrder.StayInFormation):
                    {

                        // if we are out of position, move towards formation position
                        MoveTowardFormation();
                        break;
                    }
                case (AIShipOrder.Idle):
                    {
                        if(GetAINode().GetParent() != null)
                        {
                            GetAINode().GetParent().GetEntity().RequestOrder(this);
                        }
                        
                        break;
                    }
                default:
                    {
                        break;
                    }
            }

        }
    }

    /// <summary>
    /// Clean up AINode. DO NOT LET THE DEAD SUFFER ETERNAL LIFE, HAVE THEY NOT BEEN HURT ENOUGH?!
    /// </summary>
    void OnDestroy()
    {
        m_node.Destroy();
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
            //m_prevZRot = rotZ;

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
    System.Collections.IEnumerator BeginInterp()
    {
        t = 0;
        while (t < 0.4f)
        {
            t += Time.deltaTime;
            rigidbody.MovePosition(rigidbody.position + (rigidbody.velocity * Time.deltaTime * Time.deltaTime));
            yield return 0;
        }
    }

    public void ResetShipSpeed()
    {
        m_currentShipSpeed = m_maxShipSpeed;
    }

    /// <summary>
    /// Gets the max distance by pythagorean theorem
    /// </summary>
    /// <returns></returns>
    public float GetMaxSize()
    {
        if (GetShipWidth() == 0 || GetShipHeight() == 0)
        {
            SetShipSizes();
        }

        return Mathf.Sqrt(Mathf.Pow(GetShipWidth(), 2) + Mathf.Pow(GetShipHeight(), 2));
    }

    private void SetShipSizes()
    {
        if (!maunuallySetWidthAndHeight)
        {
            MeshFilter filter = GetComponent<MeshFilter>();
            Mesh mesh = filter.mesh;

            bool bTop = false, bBottom = false, bLeft = false, bRight = false;
            Vector2 top = new Vector2(), bottom = new Vector2(), left = new Vector2(), right = new Vector2();

            foreach (Vector3 vertex in mesh.vertices)
            {
                if (bTop == false || vertex.y > top.y)
                {
                    top = vertex;
                    bTop = true;
                }
                if (bBottom == false || vertex.y < bottom.y)
                {
                    bottom = vertex;
                    bBottom = true;
                }
                if (bLeft == false || vertex.x < left.x)
                {
                    left = vertex;
                    bLeft = true;
                }
                if (bRight == false || vertex.x > right.x)
                {
                    right = vertex;
                    bRight = true;
                }
            }

            m_shipWidth = (right.x - left.x) * transform.localScale.x;
            m_shipHeight = (top.y - bottom.y) * transform.localScale.y;
        }

    }

    void MoveTowardFormation()
    {
        Vector2 formationPos = GetWorldCoordinatesOfFormationPosition(m_cacheParent.transform.position);

        Quaternion formationDirection = Quaternion.LookRotation(formationPos - (Vector2)transform.position, Vector3.back);
        formationDirection.x = 0;
        formationDirection.y = 0;

        Quaternion parentCurrentDirection = m_cacheParent.transform.rotation;
        // if we are in position, rotate to face the parents direction and then move at same speed as parent
        float t = Mathf.Clamp(Vector2.SqrMagnitude((Vector2)transform.position - formationPos), 0f, 1f);

        Quaternion target = Quaternion.Slerp(parentCurrentDirection, formationDirection, t);

        RotateTowards(target);
        MoveForward(Mathf.Lerp(m_cacheParent.GetCurrentMomentum(), GetCurrentMomentum() * 1.5f, t));
    }

    void MoveTowardTarget(Vector2 moveTarget_, float momentum_)
    {
        // TODO: this was reliant on the EnemyGroup. Needs changing so that it can follow its own target, or stay in formation otherwise.
        //if (Vector2.Distance(GetWorldCoordinatesOfFormationPosition(m_parentTransform.position), m_targetMove) > Vector2.Distance(m_shipTransform.position, m_targetMove))
        //{
        //    Vector2 distanceToClosestFormationPosition = GetVectorDistanceFromClosestFormation();
        //    Vector2 distanceToTargetPosition = (m_targetMove - (Vector2)m_shipTransform.position);

        //    float t = Mathf.Clamp(distanceToClosestFormationPosition.magnitude, 0, 5) / 5.0f;
        //    Vector2 directionToMove = (distanceToTargetPosition.normalized * (1 - t)) + (distanceToClosestFormationPosition.normalized * t);

        //    //Debug.DrawRay(transform.position, Vector3.Normalize(directionToMove), Color.cyan);
        //    //Debug.DrawLine(transform.position, (Vector2)transform.position + distanceToClosestFormationPosition, Color.green);
        //    //Debug.DrawRay(transform.position, Vector3.Normalize(distanceToTargetPosition), Color.blue);
        //    //Debug.DrawLine(transform.position, GetWorldCoordinatesOfFormationPosition(m_parentTransform.transform.position));

        //    RotateTowards((Vector2)m_shipTransform.position + directionToMove);
        //}
        //else
        //{
        //    RotateTowards(GetWorldCoordinatesOfFormationPosition(m_parentTransform.position));
        //}
        RotateTowards(moveTarget_);
        MoveForward(momentum_);
    }

    public float GetDistanceFromFormation()
    {
        return Vector2.Distance(m_shipTransform.position, GetWorldCoordinatesOfFormationPosition(m_cacheParent.transform.position));
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
        return Vector2.Distance(m_shipTransform.position, GetWorldCoordinatesOfFormationPosition(m_cacheParent.transform.position)) < distance;
    }

    /// <summary>
    /// Gets the local formation position if the parent position were at the targetLocation position
    /// </summary>
    /// <param name="targetLocation"></param>
    /// <returns></returns>
    public Vector2 GetWorldCoordinatesOfFormationPosition(Vector2 targetLocation)
    {
        return (Vector2)(m_cacheParent.transform.rotation * m_formationPosition) + targetLocation;
    }

    /// <summary>
    /// Returns the formation position with regards to the rotation of the parent
    /// </summary>
    /// <returns></returns>
    public Vector2 GetLocalFormationPosition()
    {
        return m_cacheParent.transform.rotation * m_formationPosition;
    }

    public bool CancelOrder()
    {
        m_target = null;
        m_currentOrder = AIShipOrder.Idle;
        return true;
    }

    public virtual void MoveForward(float momentum_)
    {
        Debug.Log ("AI is attempting to move with momentum of: " + momentum_);
        rigidbody.AddForce(m_shipTransform.up * momentum_ * Time.deltaTime);
    }

    public virtual void RotateTowards(Vector3 targetPosition_)
    {
        Vector2 targetDirection = targetPosition_ - transform.position;
        float idealAngle = Mathf.Rad2Deg * (Mathf.Atan2(targetDirection.y, targetDirection.x) - Mathf.PI / 2);
        float currentAngle = transform.rotation.eulerAngles.z;

        float nextAngle = Mathf.MoveTowardsAngle(currentAngle, idealAngle, GetRotateSpeed() * Time.deltaTime);

        if (Mathf.Abs(Mathf.DeltaAngle(idealAngle, currentAngle)) > 5f && true) /// turn to false to use old rotation movement
        {
            transform.rotation = Quaternion.Euler(new Vector3(0.0f, 0.0f, nextAngle));
        }
        else
        {
            Quaternion rotate = Quaternion.LookRotation(targetDirection, Vector3.back);
            rotate.x = 0;
            rotate.y = 0;

            transform.rotation = Quaternion.Slerp(transform.rotation, rotate, GetRotateSpeed() / 50 * Time.deltaTime);
        }
    }

    public virtual void RotateTowards(Quaternion target_)
    {
        transform.rotation = Quaternion.RotateTowards(transform.rotation, target_, GetRotateSpeed() * Time.deltaTime);
    }

    /// <summary>
    /// Gets the radius of the ship based on the angle you are looking at it
    /// </summary>
    /// <param name="position_"></param>
    /// <returns></returns>
    public float GetCalculatedSizeByPosition(Vector2 position_)
    {

        Vector2 dir = (position_ - (Vector2)m_shipTransform.position).normalized;

        float dot = Vector2.Dot(transform.up, dir);

        float width = (1 - Mathf.Abs(dot)) * (m_shipWidth / 2.0f);
        float height = Mathf.Abs(dot) * (m_shipHeight / 2.0f);

        return width + height;

    }

    public bool CanFireAfterburners()
    {
        return !m_afterburnersFiring && m_afterburnersRecharged;
    }

    public void FireAfterburners()
    {
        networkView.RPC("PropagateFireAfterburners", RPCMode.All);
    }
    [RPC]
    void PropagateFireAfterburners()
    {
        if (!m_afterburnersFiring && m_afterburnersRecharged)
        {
            m_afterburnersFiring = true;
            m_afterburnersRecharged = false;
            m_afterburnersHolder.gameObject.SetActive(true);
        }
    }

    public void AfterburnerFinished()
    {
        networkView.RPC("PropagateAfterburnerFinished", RPCMode.All);
    }
    [RPC]
    void PropagateAfterburnerFinished()
    {
        m_currentAfterburnerTime = 0;
        m_afterburnersFiring = false;
        m_afterburnersRecharged = false;

        m_afterburnersHolder.gameObject.SetActive(false);
    }

    public bool AfterburnersRecharging()
    {
        return (m_afterburnersFiring == false && m_afterburnersRecharged == false);
    }

    void UpdateThrusters()
    {
        foreach (Thruster thruster in m_thrusters)
        {
            if (thruster != null)
                thruster.Calculate(m_maxThrusterVelocitySeen, m_currentAngularVelocity, m_maxAngularVelocitySeen);
        }
    }

    void UpdateCurrentAngularVelocity()
    {
        m_lastRotation = m_currentRotation;
        m_currentRotation = transform.rotation.eulerAngles.z;
        
        if (m_lastRotation - m_currentRotation > 180)
        {
            m_currentAngularVelocity = m_currentRotation - (m_lastRotation - 360);
        }
        else if (m_lastRotation - m_currentRotation < -179)
        {
            m_currentAngularVelocity = (m_currentRotation - 360) - m_lastRotation;
        }
        else
        {
            m_currentAngularVelocity = m_currentRotation - m_lastRotation;
        }
    }

    public void ResetThrusters()
    {
        //networkView.RPC("PropagateResetThrusters", RPCMode.All);
    }

    [RPC]
    void PropagateResetThrusters()
    {
        ResetThrusterObjects();
        m_maxThrusterVelocitySeen = 0;
        m_maxAngularVelocitySeen = 0;
    }

    private Transform GetThrusterHolder()
    {
        return RecursiveSearchForChild(m_shipTransform, "Thrusters");
    }

    static private Transform RecursiveSearchForChild(Transform object_, string name_)
    {
        // we look along all of the current child objects incase its on the top layer
        for (int i = 0; i < object_.childCount; ++i)
        {
            Transform child = object_.GetChild(i);
            if (child.name.Equals(name_))
            {
                return child;
            }
        }

        // we then recursively search each child
        for (int i = 0; i < object_.childCount; ++i)
        {
            Transform child = object_.GetChild(i);
            Transform returnee = RecursiveSearchForChild(child, name_);
            if (returnee != null)
            {
                return returnee;
            }
        }

        return null;
    }

    /// <summary>
    /// Resets the thrusters if the ship has been changed.
    /// </summary>
    /// <returns></returns>
    public void ResetThrusterObjects()
    {
        m_thrustersHolder = GetThrusterHolder();
        m_afterburnersHolder = m_thrustersHolder.FindChild("Afterburners");
        Transform rcsholder = transform.FindChild("RCS");

        //if there are afterburners, take 1 away since the afterburner holder is a child but not a thruster itself
        int thrusterCount = m_thrustersHolder.childCount;
        if (m_afterburnersHolder != null)
        {
            thrusterCount--;
        }
        if (rcsholder != null)
        {
            thrusterCount += rcsholder.childCount;
        }

        m_thrusters = new Thruster[thrusterCount];
        int position = 0;

        for (int a = 0; position < m_thrusters.Length && a < m_thrustersHolder.childCount; )
        {
            GameObject child = m_thrustersHolder.GetChild(a).gameObject;
            ++a;
            if (child != null && !child.name.Equals("Afterburners"))
            {
                m_thrusters[position] = child.GetComponent<Thruster>();
                m_thrusters[position].SetParentShip(m_shipTransform);
                ++position;
            }
        }

        if (rcsholder != null)
        {
            for (int a = 0; position < m_thrusters.Length && a < rcsholder.childCount; )
            {
                GameObject child = rcsholder.GetChild(a).gameObject;
                ++a;
                if (child != null)
                {
                    m_thrusters[position] = child.GetComponent<Thruster>();
                    m_thrusters[position].SetParentShip(m_shipTransform);
                    ++position;
                }
            }
        }

        if (m_afterburnersHolder != null)
        {
            m_afterburners = new Thruster[m_afterburnersHolder.childCount];

            for (int i = 0; i < m_afterburners.Length; ++i)
            {
                m_afterburners[i] = m_afterburnersHolder.GetChild(i).GetComponent<Thruster>();
                m_afterburners[i].SetParentShip(m_shipTransform);
            }
        }
    }
    
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
        if (shaderCounter >= 4)
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
        if (shaderCounter >= 4)
            shaderCounter = 0;
    }
    
    System.Collections.IEnumerator ReduceShieldEffectOverTime(int i)
    {
        float t = 0;
        //coroutineIsRunning = true;
        while (t <= 1.0f)
        {
            t += Time.deltaTime;
            GameObject shield = GetShield();

            shield.renderer.material.SetFloat("_ImpactTime" + (i + 1).ToString(), 1.0f - t);
            yield return 0;
        }

        //coroutineIsRunning = false;
    }

    public GameObject GetShield()
    {
        if (!m_shieldCache || m_shieldCache.tag != "Shield")
        {
            // Search child objects for the shield.
            Transform result = m_shipTransform.Find(m_pathToShieldObject);
            m_shieldCache = result ? result.gameObject : null;

            if (!m_shieldCache || m_shieldCache.tag != "Shield")
            {
                // Fall back to old method and search
                foreach (Transform child in m_shipTransform)
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

    public virtual bool ReceiveOrder(int orderID_, object[] listOfParameters)
    {
        switch((AIShipOrder)orderID_)
        {
            case(AIShipOrder.Move):
                {
                    return false;
                }
            default:
                {
                    return false;
                }
        }
    }

    public virtual bool GiveOrder(int orderID_, object[] listOfParameters)
    {
        m_node.OrderChildren(orderID_, listOfParameters);
        return true;
    }

    public virtual bool ConsiderOrder(int orderID_, object[] listOfParameters)
    {
        switch ((AIShipOrder)orderID_)
        {
            default:
                {
                    return false;
                }
        }
    }

    public virtual bool RequestOrder(IEntity entity_)
    {
        return false;
    }

    public virtual object[] RequestInformation(int informationID_)
    {
        switch((AIShipRequestInfo)informationID_)
        {
            case(AIShipRequestInfo.Ship):
                {
                    return new object[] { this };
                }
            default:
                {
                    return null;
                }
        }
    }

    public virtual bool Notify(int informationID_, object[] listOfParameters)
    {
        switch ((AIShipNotifyInfo)informationID_)
        {
            case (AIShipNotifyInfo.ChildAdded):
            case (AIShipNotifyInfo.ChildRemoved):
                {
                    List<AINode> children = GetAINode().GetChildren();
                    List<Ship> ships = new List<Ship>();
                    children.ForEach(
                            x =>
                            {
                                ships.Add((Ship)x.GetEntity());
                            }
                        );

                    List<Vector2> formationPositions = Formations.GenerateCircleFormation(ships);

                    for (int i = 0; i < children.Count; ++i)
                    {
                        children[i].ReceiveOrder((int)AIShipOrder.StayInFormation, new object[] { formationPositions[i] });
                    }

                    return true;
                }
            case (AIShipNotifyInfo.ParentChanged):
                {
                    m_cacheParent = (Ship)listOfParameters[0];
                    return true;
                }
            case (AIShipNotifyInfo.SetFormationPosition):
                {
                    m_formationPosition = (Vector2)listOfParameters[0];
                    return true;
                }
            case(AIShipNotifyInfo.Fire):
                {

                    EnemyWeaponScript weaponScript;
                    if((weaponScript = GetComponent<EnemyWeaponScript>()) != null)
                    {
                        weaponScript.MobRequestsFire();
                        return true;
                    }
                    
                    return false;
                }
            default:
                {
                    return false;
                }
        }
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
    /// Orders a move to position
    /// </summary>
    /// <param name="position">Position to move to</param>
    void OrderMove(Vector2 position)
    {
        Vector2 fromPosition = transform.position;
        Collider collidedObject;
        bool pathFound = CheckCanMoveTo(fromPosition, position, out collidedObject);

        List<Vector2> moveOrderPositions = new List<Vector2>();

        int count = 0;

        while (!pathFound)
        {
            fromPosition = GetPositionForAvoidance(collidedObject, position, fromPosition, 20.0f, 10.0f);

            moveOrderPositions.Add(fromPosition);
            pathFound = CheckCanMoveTo(fromPosition, position, out collidedObject);

            count++;
            if (count > 20)
                break;
        }

        moveOrderPositions.Add(position);

        // uncomment to show movement paths
        //Debug.DrawLine(transform.position, moveOrderPositions[0], Color.red, 999);

        //for (int i = 0; i < moveOrderPositions.Count - 1; ++i)
        //{
        //    Debug.DrawLine(moveOrderPositions[i], moveOrderPositions[i + 1], Color.red, 999);
        //}

        m_waypoints = moveOrderPositions;

    }

    bool CheckCanMoveTo(Vector2 from, Vector2 target, out Collider collidedObject)
    {
        collidedObject = null;

        float distanceToTarget = Vector2.Distance(from, target) + 10;
        RaycastHit hit;

        bool collidedWithSomething = Physics.Raycast(new Ray(from, (target - from).normalized), out hit, distanceToTarget, 1 << Layers.environmentalDamage);

        if (collidedWithSomething)
        {
            Debug.Log ("Move order hit object: " + hit.collider.name);
            collidedObject = hit.collider;
        }

        return !collidedWithSomething;
    }

    Vector2 GetPositionForAvoidance(Collider collidedObject_, Vector2 targetLocation, Vector2 currentLocation, float closestDistanceFromGroupToObject, float radiusOfFormation)
    {
        //Vector2 directionFromObjectToThis = currentLocation - (Vector2)objectToAvoid.transform.position;
        float radiusOfObject = Mathf.Sqrt(Mathf.Pow(collidedObject_.transform.localScale.x, 2) + Mathf.Pow(collidedObject_.transform.localScale.y, 2)) * ((SphereCollider)collidedObject_).radius;
        float radius = radiusOfObject + closestDistanceFromGroupToObject + radiusOfFormation;

        Vector2[] returnee = new Vector2[2];
        returnee[0] = new Vector2(radius, 0);
        returnee[1] = new Vector2(-radius, 0);

        Vector2 dir = (targetLocation - currentLocation).normalized;
        Quaternion rotation = Quaternion.Euler(new Vector3(0, 0, (Mathf.Atan2(dir.y, dir.x) - Mathf.PI / 2) * Mathf.Rad2Deg));

        returnee[0] = (rotation * returnee[0]) + collidedObject_.transform.position;
        returnee[1] = (rotation * returnee[1]) + collidedObject_.transform.position;

        if (Vector2.SqrMagnitude(currentLocation - returnee[0]) < Vector2.SqrMagnitude(currentLocation - returnee[1]))
        {
            return returnee[0];
        }
        return returnee[1];

    }

    public virtual GameObject Clone()
    {
        GameObject obj = (GameObject)Instantiate(gameObject);
        obj.SetActive(true);
        return obj;
    }

}

using UnityEngine;
using System.Collections;

public enum AIShipOrder
{
    Idle = 0,
    Move = 1,
    Attack = 2,
    StayInFormation = 3
}

public enum AIShipRequestInfo
{
    Transform = 0,
    Move = 1,
    Target = 2
}

public enum AIShipNotifyInfo
{
    ParentChanged = 0,
    ChildAdded = 1,
    ChildRemoved = 2,
    SetFormationPosition = 3
}

[RequireComponent(typeof(MeshFilter))]
public class Ship : MonoBehaviour, IEntity
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

    [SerializeField] string m_pathToShieldObject = "Composite Collider/Shield";




    bool m_afterburnersFiring = false, m_afterburnersRecharged = true;
    float m_currentAfterburnerTime = 0.0f, m_currentAfterburnerRechargeTime = 0.0f;

    float m_currentAngularVelocity = 0;
    float m_currentRotation = 0, m_lastRotation = 0;
    float m_maxThrusterVelocitySeen = 0, m_maxAngularVelocitySeen = 0;

    int shaderCounter = 0;


    //bool coroutineIsRunning = false;
    //bool coroutineForceStopped = false;



    protected Transform m_parentTransform = null;
    protected Vector2 m_formationPosition;

    Transform m_thrustersHolder = null, m_afterburnersHolder = null;
    Thruster[] m_thrusters = null, m_afterburners = null;

    protected NetworkPlayer m_owner;
    
    protected Transform m_shipTransform;
    protected Rigidbody m_shipRigidbody;

    GameObject m_shieldCache = null;

    AINode m_node;

    //this was mainly for testing, may be deleted eventually
#pragma warning disable 0414
    [SerializeField] int shipID = -1;
    static int ids = 0;
#pragma warning restore 0414

    #region getset
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
        m_node.AddChild(child_.GetAINode());
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
    }

    protected virtual void Start()
    {
        object[] info = GetAINode().GetParent().RequestInformation((int)AIShipRequestInfo.Transform);
        if(info != null)
        {
            m_parentTransform = (Transform)info[0];
        }
        
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


        // TODO: move to fixed update
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

        //Debug.Log("Ship id = " + shipID + " has " + maxThrusterVelocitySeen + " __ " + maxAngularVelocitySeen + " __ " + currentAngularVelocity);

        //UpdateThrusters();

    }

    /// <summary>
    /// Clean up AINode. DO NOT LET THE DEAD SUFFER ETERNAL LIFE, HAVE THEY NOT BEEN HURT ENOUGH?!
    /// </summary>
    void OnDestroy()
    {
        m_node.Destroy();
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

    public virtual void RotateTowards(Vector3 targetPosition)
    {
        Vector2 targetDirection = targetPosition - transform.position;
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
    
    IEnumerator ReduceShieldEffectOverTime(int i)
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
            case(AIShipRequestInfo.Transform):
                {
                    return new object[] { m_shipTransform };
                }
            case(AIShipRequestInfo.Move):
                {
                    return null;
                }
            case(AIShipRequestInfo.Target):
                {
                    return null;
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
            case (AIShipNotifyInfo.ParentChanged):
                {
                    m_parentTransform = (Transform)listOfParameters[0];
                    return true;
                }
            case (AIShipNotifyInfo.SetFormationPosition):
                {
                    m_formationPosition = (Vector2)listOfParameters[0];
                    return true;
                }
            default:
                {
                    return false;
                }
        }
    }

}

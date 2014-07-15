using UnityEngine;
using System.Collections;

[RequireComponent(typeof(MeshFilter))]
public class Ship : MonoBehaviour
{

    public Transform shipTransform;
    public Rigidbody shipRigidbody;

    [SerializeField]
    float m_maxShipSpeed;

    [SerializeField]
    float m_currentShipSpeed = 0.0f;

    bool afterburnersFiring = false, afterburnersRecharged = true;
    float currentAfterburnerTime = 0.0f, currentAfterburnerRechargeTime = 0.0f;
    [SerializeField]
    float afterburnerIncreaseOfSpeed;
    [SerializeField]
    float afterburnerLength;
    [SerializeField]
    float afterburnerRechargeTime;
    //[SerializeField]
    //float decreaseInTurnRateWithAfterburner;

    [SerializeField]
    float m_rotateSpeed = 5.0f;

    [SerializeField]
    float m_ramDamageMultiplier = 2.5f;

    [SerializeField]
    bool maunuallySetWidthAndHeight = false;

    [SerializeField]
    protected float weaponRange = 0.0f;

    [SerializeField]
    float m_shipWidth;
    [SerializeField]
    float m_shipHeight;

    float maxThrusterVelocitySeen = 0;
    Transform thrustersHolder = null, afterburnersHolder = null;
    Thruster[] thrusters = null, afterburners = null;

    public float GetMaxShipSpeed()
    {
        return m_maxShipSpeed;
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

    protected virtual void Awake()
    {
        Init();
    }

    [SerializeField]
    public int shipID = -1;
    static int ids = 0;

    protected void Init()
    {
        shipID = ids++;

        shipTransform = transform;
        shipRigidbody = rigidbody;
        SetShipSizes();
        //ResetThrusters();
    }

    protected virtual void Update()
    {
        if (afterburnersFiring == true)
        {
            currentAfterburnerTime += Time.deltaTime;
            if (currentAfterburnerTime >= afterburnerLength)
            {
                AfterburnerFinished();
            }
        }

        if (afterburnersFiring == false)
        {
            if (afterburnersRecharged == false)
            {
                currentAfterburnerRechargeTime += Time.deltaTime;
                if (currentAfterburnerRechargeTime >= afterburnerRechargeTime)
                {
                    afterburnersRecharged = true;
                    currentAfterburnerRechargeTime = 0;
                }
            }
        }

        // we cant calculate the max velocity neatly so we check to see if its larger
        if (maxThrusterVelocitySeen < shipRigidbody.velocity.magnitude)
        {
            maxThrusterVelocitySeen = shipRigidbody.velocity.magnitude;
        }

        if (thrustersHolder != null && maxThrusterVelocitySeen > 0)
        {

            float ratio = shipRigidbody.velocity.magnitude / maxThrusterVelocitySeen;
            float clampedDot = Mathf.Clamp(Vector2.Dot(shipTransform.up, shipTransform.rigidbody.velocity.normalized), 0, 1);
            foreach (Thruster thruster in thrusters)
            {
                thruster.SetPercentage(clampedDot * ratio);
            }

        }

    }

    public void SetShipMomentum(float currentSpeed)
    {
        m_currentShipSpeed = Mathf.Min(m_maxShipSpeed, currentSpeed / rigidbody.mass);
    }

    public void SetMaxShipSpeed(float maxSpeed_)
    {
        m_maxShipSpeed = maxSpeed_;
    }

    public void SetCurrentShipSpeed(float currentSpeed)
    {
        m_currentShipSpeed = Mathf.Clamp(currentSpeed, 0, m_maxShipSpeed);
    }

    public float GetCurrentShipSpeed()
    {
        return afterburnersFiring == true ? m_currentShipSpeed + afterburnerIncreaseOfSpeed : m_currentShipSpeed;
    }

    public void ResetShipSpeed()
    {
        m_currentShipSpeed = m_maxShipSpeed;
    }

    public void SetRotateSpeed(float rotateSpeed_)
    {
        m_rotateSpeed = rotateSpeed_;
    }

    public float GetRotateSpeed()
    {
        return m_rotateSpeed;
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
    /// Gets the max distance by pythagorean theorem
    /// </summary>
    /// <returns></returns>
    public float GetMaxSize()
    {
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

    public virtual void RotateTowards(Vector3 position)
    {
        Vector3 dir = Vector3.Normalize(position - shipTransform.position);
        Quaternion lookRotation = Quaternion.Euler(new Vector3(0, 0, (Mathf.Atan2(dir.y, dir.x) - Mathf.PI / 2) * Mathf.Rad2Deg));
        rigidbody.MoveRotation(Quaternion.Slerp(shipTransform.rotation, lookRotation, GetRotateSpeed() * Time.deltaTime));
    }

    public float GetCalculatedSizeByPosition(Vector2 position_)
    {

        Vector2 dir = (position_ - (Vector2)shipTransform.position).normalized;

        float dot = Vector2.Dot(transform.up, dir);

        float width = (1 - Mathf.Abs(dot)) * (m_shipWidth / 2.0f);
        float height = Mathf.Abs(dot) * (m_shipHeight / 2.0f);

        return width + height;

    }

    public bool CanFireAfterburners()
    {
        return !afterburnersFiring && afterburnersRecharged;
    }

    public void FireAfterburners()
    {
        if (!afterburnersFiring && afterburnersRecharged)
        {
            afterburnersFiring = true;
            afterburnersRecharged = false;
            afterburnersHolder.gameObject.SetActive(true);
        }
    }

    public void AfterburnerFinished()
    {
        currentAfterburnerTime = 0;
        afterburnersFiring = false;
        afterburnersRecharged = false;

        afterburnersHolder.gameObject.SetActive(false);
    }

    public virtual float GetMinimumWeaponRange()
    {
        return weaponRange;
    }

    public void ResetThrusters()
    {
        ResetThrusterObjects();
        maxThrusterVelocitySeen = 0;
    }

    private Transform GetThrusterHolder()
    {
        return RecursiveSearchForChild(shipTransform, "Thrusters");
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
        thrustersHolder = GetThrusterHolder();
        afterburnersHolder = thrustersHolder.transform.FindChild("Afterburners");

        //if there are afterburners, take 1 away since the afterburner holder is a child but not a thruster itself
        thrusters = new Thruster[afterburnersHolder != null ? thrustersHolder.transform.childCount - 1 : thrustersHolder.transform.childCount];



        for (int i = 0, a = 0; i < thrusters.Length; )
        {
            GameObject child = thrustersHolder.transform.GetChild(a).gameObject;
            ++a;
            if (child != null && !child.name.Equals("Afterburners"))
            {
                thrusters[i] = child.GetComponent<Thruster>();
                ++i;
            }
        }


        if (afterburnersHolder != null)
        {
            afterburners = new Thruster[afterburnersHolder.childCount];

            for (int i = 0; i < afterburners.Length; ++i)
            {
                afterburners[i] = afterburnersHolder.GetChild(i).GetComponent<Thruster>();
            }
        }


    }

    /// <summary>
    /// Returns thrusters. Do not use if the engine has changed as it will not return the updated thrusters. Use FindThrusters instead
    /// </summary>
    /// <returns></returns>
    public Thruster[] GetThrusters()
    {
        return thrusters;
    }

    public Thruster[] GetAfterburners()
    {
        return afterburners;
    }
}

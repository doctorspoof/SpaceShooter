using UnityEngine;
using System.Collections;

[RequireComponent(typeof(MeshFilter))]
public class Ship : MonoBehaviour
{

    protected Transform shipTransform;

    [SerializeField]
    float m_maxShipSpeed;

    [SerializeField]
    float m_currentShipSpeed = 0.0f;

    [SerializeField]
    float m_rotateSpeed = 5.0f;

    [SerializeField]
    float m_ramDamageMultiplier = 2.5f;

    [SerializeField]
    bool maunuallySetWidthAndHeight = false;

    [SerializeField]
    float m_shipWidth;
    [SerializeField]
    float m_shipHeight;

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
        return m_currentShipSpeed * rigidbody.mass;
    }

    public float GetRamDam()
    {
        return m_ramDamageMultiplier;
    }

    void Awake()
    {
        Init();
    }

    protected void Init()
    {
        shipTransform = transform;
        SetShipSizes();
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
        return m_currentShipSpeed;
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
}

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum ShipManoeuvre
{
    None = 0,
    TurnRight = 1,
    TurnLeft = 2
}

public class Thruster : MonoBehaviour
{
    [SerializeField] ShipManoeuvre m_firesWithTurn;

    Transform m_thrusterTransform, m_parentShipTransform;
    Vector3 m_originalScale, m_originalPosition;

    public void SetParentShip(Transform parentShip_)
    {
        m_parentShipTransform = parentShip_;
    }

    void Start()
    {
        m_thrusterTransform = transform;
        m_originalScale = m_thrusterTransform.localScale;
        m_originalPosition = m_thrusterTransform.localPosition;
    }

    void SetPercentage(float percentage_)
    {
        float clamped = Mathf.Clamp(percentage_, 0, 1);

        Vector3 newScale = m_originalScale * clamped;
        newScale.z = 1;
        m_thrusterTransform.localScale = newScale;

        Vector3 changeInScale = newScale - m_originalScale;

        m_thrusterTransform.localPosition = m_originalPosition - (m_thrusterTransform.localRotation * new Vector3(0, changeInScale.y / 2, 0));

        //Debug.DrawLine(thrusterTransform.parent.position + originalPosition, thrusterTransform.parent.position + thrusterTransform.localPosition, Color.red);
    }

    public void Calculate(float maxVelocitySeen_, float currentAngularAcceleration_, float maxAngularAccelerationSeen_)
    {
        if(m_parentShipTransform == null || m_thrusterTransform == null)
        {
            return;
        }

        float percentFromVelocity = 0;

        if (maxVelocitySeen_ > 0)
        {
            float ratio = m_parentShipTransform.rigidbody.velocity.magnitude / maxVelocitySeen_;
            float clampedDot = Mathf.Clamp(Vector2.Dot(m_thrusterTransform.up, m_parentShipTransform.rigidbody.velocity.normalized), 0, 1);

            percentFromVelocity = ratio * clampedDot;
        }

        float percentFromAngular = 0;

        if (maxAngularAccelerationSeen_ > 0)
        {
            if (m_firesWithTurn == ShipManoeuvre.TurnLeft && currentAngularAcceleration_ > 0)
            {
                percentFromAngular = Mathf.Abs(currentAngularAcceleration_) / maxAngularAccelerationSeen_;
            }
            else if (m_firesWithTurn == ShipManoeuvre.TurnRight && currentAngularAcceleration_ < 0)
            {
                percentFromAngular = Mathf.Abs(currentAngularAcceleration_) / maxAngularAccelerationSeen_;
            }
        }

        float passedPercentage = Mathf.Max(percentFromVelocity, percentFromAngular);

        SetPercentage(passedPercentage);

    }


}

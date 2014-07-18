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
    [SerializeField]
    //List<ShipManoeuvre> directionsToFireWith;

    //int manoeuvreToFireWith = 0;

    ShipManoeuvre firesWithTurn;

    Transform thrusterTransform;
    Vector3 originalScale, originalPosition;

    // Use this for initialization
    void Start()
    {
        thrusterTransform = transform;
        originalScale = thrusterTransform.localScale;
        originalPosition = thrusterTransform.localPosition;

        //foreach(ShipManoeuvre mano in directionsToFireWith)
        //{
        //    manoeuvreToFireWith = manoeuvreToFireWith | (int)mano;
        //}
    }

    // Update is called once per frame
    void Update()
    {

    }

    void SetPercentage(float percentage_)
    {
        float clamped = Mathf.Clamp(percentage_, 0, 1);

        Vector3 newScale = originalScale * clamped;
        newScale.z = 1;
        thrusterTransform.localScale = newScale;

        Vector3 changeInScale = newScale - originalScale;

        thrusterTransform.localPosition = originalPosition - (thrusterTransform.localRotation * new Vector3(0, changeInScale.y / 2, 0));

        //Debug.DrawLine(thrusterTransform.parent.position + originalPosition, thrusterTransform.parent.position + thrusterTransform.localPosition, Color.red);
    }

    public void Calculate(/*Vector2 velocity_,*/ float maxVelocitySeen_, float currentAngularAcceleration_, float maxAngularAccelerationSeen_)
    {

        float percentFromVelocity = 0;

        if (maxVelocitySeen_ > 0)
        {
            float ratio = transform.root.rigidbody.velocity.magnitude / maxVelocitySeen_;
            float clampedDot = Mathf.Clamp(Vector2.Dot(thrusterTransform.up, transform.root.rigidbody.velocity.normalized), 0, 1);

            percentFromVelocity = ratio * clampedDot;
        }

        float percentFromAngular = 0;

        if (maxAngularAccelerationSeen_ > 0)
        {
            if (firesWithTurn == ShipManoeuvre.TurnLeft && currentAngularAcceleration_ > 0)
            {
                percentFromAngular = Mathf.Abs(currentAngularAcceleration_) / maxAngularAccelerationSeen_;
            }
            else if (firesWithTurn == ShipManoeuvre.TurnRight && currentAngularAcceleration_ < 0)
            {
                percentFromAngular = Mathf.Abs(currentAngularAcceleration_) / maxAngularAccelerationSeen_;
            }
        }

        SetPercentage(percentFromVelocity + percentFromAngular);

    }

}

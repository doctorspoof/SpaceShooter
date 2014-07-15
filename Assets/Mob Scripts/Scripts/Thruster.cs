using UnityEngine;
using System.Collections;

public enum ShipManoeuvre
{
    Forward = 1,
    Backward = 2,
    Left = 4,
    Right = 8,
    TurnRight = 16,
    TurnLeft = 32
}

public class Thruster : MonoBehaviour
{

    int manoeuvreToFireWith = 0;

    Transform thrusterTransform;
    Vector3 originalScale, originalPosition;

    // Use this for initialization
    void Start()
    {
        thrusterTransform = transform;
        originalScale = thrusterTransform.localScale;
        originalPosition = thrusterTransform.localPosition;
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void SetPercentage(float percentage_)
    {
        float clamped = Mathf.Clamp(percentage_, 0, 1);

        Vector3 newScale = originalScale * clamped;
        newScale.z = 1;
        thrusterTransform.localScale = newScale;

        thrusterTransform.localPosition = originalPosition - (new Vector3(0, (newScale.y - originalScale.y) / 2, 0));
        //Debug.DrawLine(thrusterTransform.parent.position + originalPosition, thrusterTransform.parent.position + thrusterTransform.localPosition, Color.red);
    }

}

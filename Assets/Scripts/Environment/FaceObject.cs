using UnityEngine;



/// <summary>
/// Causes an object to make sure that it is facing a particular star at all times. This allows for correct shadowing of planets which have an orbit.
/// </summary>
public sealed class FaceObject : MonoBehaviour
{
	[SerializeField] Transform m_pointToFace;   //!< A reference to the star which the object should always look at.

	Vector3 m_previousPoint = Vector3.zero;     //!< The previous point that the object was at. Stops the rotation being calculated every frame.


    /// <summary>
    /// Rotates the object correctly so that it is facing an object every fixed update.
    /// </summary>
	void FixedUpdate()
    {
        if (m_pointToFace != null && m_pointToFace.position != m_previousPoint)
        {   
            // Keep a copy of the last known position
            m_previousPoint = m_pointToFace.position;

            // Obtain the direction to face
            Vector3 direction = (transform.position - m_pointToFace.position).normalized;

            // Calculate the correct rotation values
            Quaternion newRotation = transform.rotation * Quaternion.FromToRotation (transform.up, direction);
            newRotation.x = 0f;
            newRotation.y = 0f;

            // Assign the rotation
            transform.rotation = newRotation;
        }
	}
}

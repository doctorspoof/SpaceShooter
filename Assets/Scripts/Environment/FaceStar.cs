using UnityEngine;



/// <summary>
/// Causes an object to make sure that it is facing a particular star at all times. This allows for correct shadowing of planets which have an orbit.
/// </summary>
public sealed class FaceStar : MonoBehaviour
{
	[SerializeField] GameObject m_SunRef;   //!< A reference to the star which the object should always look at.
	

    /// <summary>
    /// Rotates the object correctly so that it is facing an object every fixed update.
    /// </summary>
	void FixedUpdate()
    {
        if (m_SunRef != null)
        {   
            // Obtain the direction to face
            Vector3 direction = (this.transform.position - m_SunRef.transform.position).normalized;

            // Assign the correct rotation values
            transform.rotation *= Quaternion.FromToRotation (transform.up, direction);
            transform.rotation.x = 0;
            transform.rotation.y = 0;
        }
	}
}

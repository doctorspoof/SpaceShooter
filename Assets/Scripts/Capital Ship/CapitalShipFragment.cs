using UnityEngine;



/// <summary>
/// A CapitalShipFragment is a piece of the ship which is created upon explosion of the ship. This class causes the fragment to randomly rotate and move away from
/// its starting position as if it were being forced away.
/// </summary>
public sealed class CapitalShipFragment : MonoBehaviour 
{
    #region Unity modifiable variables

	[SerializeField] Vector3 m_travelDirection = Vector3.zero;  //!< The direction in which the fragment should move.
	[SerializeField] bool m_clockwiseSpin = true;               //!< Whether the fragment should spin clockwise or anti-clockwise.

    #endregion Unity modifable variables


    #region Internal data
    
    float m_movementSpeed = 0f; //!< A randomly chosen speed for the fragment to move at.
	float m_rotationSpeed = 0f; //!< A randomly chosen speed for the fragment to rotate at.

    #endregion Internal data


    #region Behaviour functions

    /// <summary>
    /// Generates the random movement and rotation speed. Also introduces an element of randomness to the travel direction specfied
    /// in the Unity editor.
    /// </summary>
	void Awake() 
	{
        m_movementSpeed = Random.Range (0.0f, 0.1f);
        m_rotationSpeed = m_clockwiseSpin ? Random.Range (0.0f, 2.5f) : Random.Range (-2.5f, 0.0f);
		
		m_travelDirection = new Vector3 (Random.Range (m_travelDirection.x - 0.025f, m_travelDirection.x + 0.025f), 
                                         Random.Range (m_travelDirection.y - 0.025f, m_travelDirection.y + 0.025f), 
                                         0f);
	}


    /// <summary>
    /// Keeps the fragment moving and rotating at each interval.
    /// </summary>
	void FixedUpdate()
	{
		this.transform.localPosition += m_travelDirection * Time.deltaTime * m_movementSpeed;
		this.transform.RotateAround(this.transform.position, this.transform.forward, m_rotationSpeed * Time.deltaTime);
	}

    #endregion Behaviour functions
}

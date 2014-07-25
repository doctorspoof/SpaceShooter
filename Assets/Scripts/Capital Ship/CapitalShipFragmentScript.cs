using UnityEngine;
using System.Collections;

public class CapitalShipFragmentScript : MonoBehaviour 
{
    /* Serializable members */
	[SerializeField] Vector3 m_travelDirection;
	[SerializeField] bool m_clockwiseSpin;

    /* Internal members */
	float m_rotationAmount;
	Vector3 m_direction;
	float m_speed;

	/* Unity functions */
	void Start () 
	{
		m_rotationAmount = m_clockwiseSpin ? Random.Range(0.0f, 2.5f) : Random.Range (-2.5f, 0.0f);
		m_speed = Random.Range(0.0f, 0.1f);
		m_direction = new Vector3(Random.Range(m_travelDirection.x - 0.025f, m_travelDirection.x + 0.025f), Random.Range(m_travelDirection.y - 0.025f, m_travelDirection.y + 0.025f), 0);
	}
	void FixedUpdate () 
	{
		this.transform.localPosition += m_direction * Time.deltaTime * m_speed;
		this.transform.RotateAround(this.transform.position, this.transform.forward, m_rotationAmount * Time.deltaTime);
	}
}

using UnityEngine;
using System.Collections;

public class CapitalShipFragmentScript : MonoBehaviour 
{
	[SerializeField]
	Vector3 m_travelDirection;

	[SerializeField]
	bool m_clockwiseSpin;

	float rotationAmount;
	Vector3 direction;
	float speed;

	// Use this for initialization
	void Start () 
	{
		rotationAmount = m_clockwiseSpin ? Random.Range(0.0f, 2.5f) : Random.Range (-2.5f, 0.0f);
		Debug.Log ("Rotationamount: " + rotationAmount);
		speed = Random.Range(0.0f, 0.1f);
		direction = new Vector3(Random.Range(m_travelDirection.x - 0.025f, m_travelDirection.x + 0.025f), Random.Range(m_travelDirection.y - 0.025f, m_travelDirection.y + 0.025f), 0);
	}
	
	// Update is called once per frame
	void FixedUpdate () 
	{
		this.transform.localPosition += direction * Time.deltaTime * speed;
		
		this.transform.RotateAround(this.transform.position, this.transform.forward, rotationAmount * Time.deltaTime);
	}
}

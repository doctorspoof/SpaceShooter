using UnityEngine;
using System.Collections;

public class CapitalShipFragmentScript : MonoBehaviour 
{
	[SerializeField]
	Vector3 m_travelDirection;

	[SerializeField]
	bool m_clockwiseSpin;

	// Use this for initialization
	void Start () 
	{
	
	}
	
	// Update is called once per frame
	void FixedUpdate () 
	{
		this.transform.localPosition += m_travelDirection * Time.deltaTime * 0.1f;

		float amount = m_clockwiseSpin ? Random.Range(0.0f, 0.05f) : Random.Range (-0.05f, 0.0f);
		this.transform.RotateAround(this.transform.position, this.transform.forward, amount * Time.deltaTime);
	}
}

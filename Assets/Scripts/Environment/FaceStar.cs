using UnityEngine;
using System.Collections;

public class FaceStar : MonoBehaviour
{

	[SerializeField] GameObject m_SunRef;
	
	void LateUpdate () 
    {
		Vector3 direction = this.transform.position - m_SunRef.transform.position;
		direction.Normalize ();
		Quaternion rotation = this.transform.rotation * Quaternion.FromToRotation (this.transform.up, direction);
		rotation.x = 0;
		rotation.y = 0;
		this.transform.rotation = rotation;
	}
}

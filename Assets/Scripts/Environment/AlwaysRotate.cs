using UnityEngine;
using System.Collections;

public class AlwaysRotate : MonoBehaviour {

	[SerializeField]
	float m_rotationAmount = 1.0f;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () 
	{
		this.transform.rotation = Quaternion.Euler(new Vector3(0, 0, this.transform.rotation.eulerAngles.z + (m_rotationAmount * Time.deltaTime)));
	}
}

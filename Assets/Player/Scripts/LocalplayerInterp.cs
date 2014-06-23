using UnityEngine;
using System.Collections;

public class LocalPlayerInterp : MonoBehaviour 
{
	Vector3 p;
	Vector3 v;
	Quaternion r;
	int m = 0;

	// Use this for initialization
	void Start () 
	{
		networkView.observed = this;
	}

	void OnSerializeNetworkView(BitStream stream)
	{
		//p = this.transform.position;
		p = rigidbody.position;
		//v = this.GetComponent<PlayerControlScript>().m_currentVelocity;
		v = rigidbody.velocity;
		//r = this.transform.rotation;
		r = rigidbody.rotation;
		m = 0;
		stream.Serialize(ref p);
		stream.Serialize(ref v);
		stream.Serialize(ref r);
		stream.Serialize(ref m);
	}

	// Update is called once per frame
	void Update () 
	{
		
	}
}

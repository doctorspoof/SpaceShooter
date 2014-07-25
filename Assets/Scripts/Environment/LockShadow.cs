using UnityEngine;
using System.Collections;

public class LockShadow : MonoBehaviour {
	[SerializeField]
	Vector3 Rotation;

	// Use this for initialization
	void Start () {
		Rotation = transform.rotation.eulerAngles;
	}
	
	// Update is called once per frame
	void Update () {
		this.transform.rotation = Quaternion.Euler (Rotation);

	
	}
}

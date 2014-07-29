using UnityEngine;
using System.Collections;

public class LockShadow : MonoBehaviour
{
    Transform transf;
	Quaternion rotation;

	void Start () {
        transf = transform;
        rotation = new Quaternion(transf.rotation.x, transf.rotation.y, transf.rotation.z, transf.rotation.w);
	}
	
	// Update is called once per frame
	void Update () {
        transf.rotation = new Quaternion(rotation.x, rotation.y, rotation.z, rotation.w);
	}
}

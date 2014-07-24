using UnityEngine;
using System.Collections;

enum RotationAxis
{
	xRot = 0,
	yRot = 1,
	zRot = 2
}

public class ObjectSpin : MonoBehaviour {
	public float SpinRate = 0;

	[SerializeField]
	RotationAxis rotAxis = RotationAxis.zRot;	

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void FixedUpdate () 
	{
		switch(rotAxis)
		{
			case RotationAxis.xRot:
			{
				//this.transform.rotation = Quaternion.Euler ( new Vector3 (this.transform.rotation.eulerAngles.x + (SpinRate * Time.deltaTime), 0, 0));
				this.transform.RotateAround(this.transform.position, this.transform.right, SpinRate * Time.deltaTime);
				break;
			}
			case RotationAxis.yRot:
			{
				//this.transform.rotation = Quaternion.Euler ( new Vector3 (0, this.transform.rotation.eulerAngles.y + (SpinRate * Time.deltaTime), 0));
				this.transform.RotateAround(this.transform.position, this.transform.up, SpinRate * Time.deltaTime);
				break;
			}
			case RotationAxis.zRot:
			{
				//this.transform.rotation = Quaternion.Euler ( new Vector3 (0, 0, this.transform.rotation.eulerAngles.z + (SpinRate * Time.deltaTime)));
				this.transform.RotateAround(this.transform.position, this.transform.forward, SpinRate * Time.deltaTime);
				break;
			}
		}

        if(this.tag == "Star")
            this.transform.localPosition = new Vector3(0, 0, 15.0f);
        else
            this.transform.localPosition = Vector3.zero;
	}
}


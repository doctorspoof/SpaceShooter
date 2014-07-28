using UnityEngine;
using System.Collections;

enum RotationAxis
{
    xRot = 0,
    yRot = 1,
    zRot = 2
}

public class ObjectSpin : MonoBehaviour
{
    [SerializeField] float SpinRate = 0;

    [SerializeField] RotationAxis rotAxis = RotationAxis.zRot;

    Transform transf;

    void Awake()
    {
        transf = transform;
    }

    void FixedUpdate()
    {
        switch (rotAxis)
        {
            case RotationAxis.xRot:
                {
                    //this.transform.rotation = Quaternion.Euler ( new Vector3 (this.transform.rotation.eulerAngles.x + (SpinRate * Time.deltaTime), 0, 0));
                    transf.RotateAround(transf.position, transf.right, SpinRate * Time.deltaTime);
                    break;
                }
            case RotationAxis.yRot:
                {
                    //this.transform.rotation = Quaternion.Euler ( new Vector3 (0, this.transform.rotation.eulerAngles.y + (SpinRate * Time.deltaTime), 0));
                    transf.RotateAround(transf.position, transf.up, SpinRate * Time.deltaTime);
                    break;
                }
            case RotationAxis.zRot:
                {
                    //this.transform.rotation = Quaternion.Euler ( new Vector3 (0, 0, this.transform.rotation.eulerAngles.z + (SpinRate * Time.deltaTime)));
                    transf.RotateAround(transf.position, transf.forward, SpinRate * Time.deltaTime);
                    break;
                }
        }

        if (this.tag == "Star")
            transf.localPosition = new Vector3(0, 0, 15.0f);
        else
            transf.localPosition = Vector3.zero;
    }
}


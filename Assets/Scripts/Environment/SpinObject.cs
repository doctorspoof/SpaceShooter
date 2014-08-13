using UnityEngine;



/// <summary>
/// Quite literally just spins an object around an axis every FixedUpdate().
/// </summary>
public sealed class SpinObject : MonoBehaviour
{
    /// <summary>
    /// Determines the axis than an object should spin on.
    /// </summary>
    enum RotationAxis
    {
        xRot = 0,
        yRot = 1,
        zRot = 2
    }


    #region Unity modifiable variables
    
    [SerializeField] RotationAxis m_axisToRotateAround = RotationAxis.zRot; //!< Which axis should the object rotate around.
    [SerializeField] float m_spinRate = 0f;                                  //!< How fast should the object spin.

    #endregion


    #region Internal data

    Transform transf = null;    //!< A cache of the transform for the sake of efficiency.

    #endregion


    #region Behaviour functions

    /// <summary>
    /// Caches the transform.
    /// </summary>
    void Awake()
    {
        transf = transform;
       
        transf.localPosition = CompareTag ("Star") ? new Vector3 (0, 0, 15.0f) : Vector3.zero;
    }


    /// <summary>
    /// Rotates around the desired axis.
    /// </summary>
    void FixedUpdate()
    {
        switch (m_axisToRotateAround)
        {
            case RotationAxis.xRot:
            {
                transf.RotateAround (transf.position, transf.right, m_spinRate * Time.deltaTime);
                break;
            }
            case RotationAxis.yRot:
            {
                transf.RotateAround (transf.position, transf.up, m_spinRate * Time.deltaTime);
                break;
            }
            case RotationAxis.zRot:
            {
                transf.RotateAround (transf.position, transf.forward, m_spinRate * Time.deltaTime);
                break;
            }
        }
    }

    #endregion
}


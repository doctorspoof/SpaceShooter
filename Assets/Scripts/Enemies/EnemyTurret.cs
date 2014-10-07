using UnityEngine;
using System.Collections;

public class EnemyTurret : MonoBehaviour
{
    [SerializeField] float m_rotateSpeed = 2.0f;

    [SerializeField] GameObject m_bulletRef;
    GameObject target;
    float weaponRange = -1;



    #region getset

    public void SetTarget(GameObject target)
    {
        this.target = target;
    }

    public float GetRange()
    {
        return GetComponent<EquipmentTypeWeapon>().GetBulletRange();
    }

    #endregion getset

    void Update()
    {
        if (target != null)
        {
            float timeTakenToTravel = Vector3.Distance(target.transform.position, this.transform.position) / m_bulletRef.GetComponent<BasicBulletScript>().GetBulletSpeed();
            Vector3 predictedTargetPos = target.transform.position + (target.rigidbody.velocity * timeTakenToTravel);

            Vector3 dir = predictedTargetPos - this.transform.position;
            Quaternion targetR = Quaternion.Euler(new Vector3(0, 0, (Mathf.Atan2(dir.y, dir.x) - Mathf.PI / 2) * Mathf.Rad2Deg));
            this.transform.rotation = Quaternion.Slerp(transform.rotation, targetR, m_rotateSpeed * Time.deltaTime);

            if (Network.isServer)
            {
                //Try to fire
                //float distance = Vector3.Distance(target.transform.position, transform.position);
                float dist = Vector3.SqrMagnitude(target.transform.position - transform.position);

                Ship targetShip = target.GetComponent<Ship>();
                if (targetShip != null)
                {
                    dist -= targetShip.GetMaxSize();
                }

                // dist is squared so GetRange needs squaring too
                if (dist < GetRange() * GetRange())
                {
                    Fire();
                }
            }
        }
        else
        {
            //Rotate to face forwards
            Vector3 dir = transform.parent.forward;
            Quaternion targetR = Quaternion.Euler(new Vector3(0, 0, (Mathf.Atan2(dir.y, dir.x) - Mathf.PI / 2) * Mathf.Rad2Deg));
            this.transform.rotation = Quaternion.Slerp(transform.rotation, targetR, m_rotateSpeed * Time.deltaTime);
        }
    }

    void Fire()
    {
        GetComponent<EquipmentTypeWeapon>().MobRequestsFire();
    }

    
}

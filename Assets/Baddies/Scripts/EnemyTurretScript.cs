using UnityEngine;
using System.Collections;

public class EnemyTurretScript : MonoBehaviour 
{
	[SerializeField]
	float m_recoilTime;
	float m_currentRecoilTime;

	[SerializeField]
	int m_shotsPerVolley = 1;
	[SerializeField]
	float m_spreadFactor = 0.0f;
	[SerializeField]
	bool m_evenSpread = true;

	[SerializeField]
	float m_rotateSpeed = 2.0f;

	[SerializeField]
	GameObject m_bulletRef;

	GameObject target;

	// Use this for initialization
	void Start () 
	{
		m_currentRecoilTime = m_recoilTime;
	}
	
	// Update is called once per frame
	void Update () 
	{
		if(target != null)
		{
			float timeTakenToTravel = Vector3.Distance(target.transform.position, this.transform.position) / m_bulletRef.GetComponent<BasicBulletScript>().GetBulletSpeed();
			Vector3 predictedTargetPos = target.transform.position + (target.rigidbody.velocity * timeTakenToTravel);
			//Vector3 targetPos = target.transform.position;

			Vector3 dir = predictedTargetPos - this.transform.position;
			//Vector3 dir = targetPos - this.transform.position;
			Quaternion targetR = Quaternion.Euler(new Vector3(0, 0, (Mathf.Atan2 (dir.y, dir.x) - Mathf.PI /2) * Mathf.Rad2Deg));
			this.transform.rotation = Quaternion.Slerp (transform.rotation, targetR, m_rotateSpeed * Time.deltaTime);

			if(Network.isServer)
			{
				//Try to fire
				float distance = Vector3.Distance(target.transform.position, transform.position);
				if(distance < GetRange())
				{
					if(m_currentRecoilTime >= m_recoilTime)
						AttemptFire();
				}
			}
		}
		else
		{
			//Rotate to face forwards
			Vector3 dir = transform.parent.forward;
			Quaternion targetR = Quaternion.Euler(new Vector3(0, 0, (Mathf.Atan2 (dir.y, dir.x) - Mathf.PI /2) * Mathf.Rad2Deg));
			this.transform.rotation = Quaternion.Slerp (transform.rotation, targetR, m_rotateSpeed * Time.deltaTime);
		}

		if(m_currentRecoilTime < m_recoilTime)
			m_currentRecoilTime += Time.deltaTime;
	}

	void AttemptFire()
	{
		float spreadIncrement = (m_spreadFactor * 2) / m_shotsPerVolley;
		for(int i = 0; i < m_shotsPerVolley; i++)
		{
			float randAngle;
			if(m_evenSpread)
				randAngle = -m_spreadFactor + (spreadIncrement * i);
			else
				randAngle = Random.Range(-m_spreadFactor, m_spreadFactor + 1.0f);
			
			Quaternion bulletRot = this.transform.rotation * Quaternion.Euler(0, 0, randAngle);
			
			Vector3 pos = this.transform.position;
			pos.z += 0.1f;
			GameObject bullet = (GameObject)Network.Instantiate(m_bulletRef, pos, bulletRot, 0);
			bullet.GetComponent<BasicBulletScript>().firer = this.transform.parent.gameObject;
		}

		m_currentRecoilTime = 0;
	}

	public void SetTarget(GameObject target)
	{
		this.target = target;
	}

	public float GetRange()
	{
		BasicBulletScript script = m_bulletRef.GetComponent<BasicBulletScript>();
		return script.CalculateMaxDistance();
	}
}

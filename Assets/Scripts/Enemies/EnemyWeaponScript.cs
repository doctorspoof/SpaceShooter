using UnityEngine;
using System.Collections;

public class EnemyWeaponScript : MonoBehaviour {

	[SerializeField]
	float m_recoilTime;
	float m_currentRecoil;
	[SerializeField]
	GameObject m_bulletRef;

	int m_currentFirePoint = 0;
	[SerializeField]
	GameObject[] m_firePoints;

	[SerializeField]
	float m_burstFireTime = 2.0f;
	[SerializeField]
	bool m_firesInBursts = false;
	[SerializeField]
	bool m_firesSequentially = false;
	[SerializeField]
	int m_shotsFiredInBurst = 1;

	[SerializeField]
	bool m_fireNow = false;

	// Use this for initialization
	void Start () 
	{
		
	}
	
	// Update is called once per frame
	void Update () 
	{
		if(m_currentRecoil < m_recoilTime)
		{
			m_currentRecoil += Time.deltaTime;
		}

		if(m_fireNow)
		{
			m_fireNow = false;
			MobRequestsFire();
		}
	}
	
	public void MobRequestsFire()
	{
		if(m_currentRecoil >= m_recoilTime)
		{
			if(!m_firesInBursts)
			{
				//Fire!
				GameObject bullet = (GameObject)Network.Instantiate(m_bulletRef, this.transform.position + new Vector3(0, 0, 0.0f), this.transform.rotation, 0);
				bullet.GetComponent<BasicBulletScript>().firer = this.gameObject;

			}
			else
			{
				if(m_firesSequentially)
				{

				}
				else
					StartCoroutine(FireAlternatingPorts());	
			}

			m_currentRecoil = 0;
		}
	}

	IEnumerator FireBurstRoutine()
	{
		float t = 0;

		//Fire initial set
		FireAllPorts();

		while(t < m_burstFireTime)
		{
			t += Time.deltaTime;
			yield return 0;
		}

		FireAllPorts();
	}

	void FireAllPorts()
	{
		for(int i = 0; i < m_firePoints.Length; i++)
		{
			GameObject bullet = (GameObject)Network.Instantiate(m_bulletRef, m_firePoints[i].transform.position, m_firePoints[i].transform.rotation, 0);
			bullet.GetComponent<BasicBulletScript>().firer = this.gameObject;
			bullet.GetComponent<BasicBulletScript>().m_homingTarget = GameObject.FindGameObjectWithTag("Capital");
		}
	}
	IEnumerator FireAlternatingPorts()
	{
		float t = 0.0f;
		float miniT = 0.0f;
		int counter = 0;

		while(t < (m_burstFireTime * m_firePoints.Length) + 1.0f)
		{
			t += Time.deltaTime;
			miniT += Time.deltaTime;

			if(miniT >= m_burstFireTime)
			{
				if(counter == 0 || counter == 2)
				{
					GameObject bullet = (GameObject)Network.Instantiate(m_bulletRef, m_firePoints[0].transform.position, m_firePoints[0].transform.rotation, 0);
					bullet.GetComponent<BasicBulletScript>().firer = this.gameObject;
					bullet.GetComponent<BasicBulletScript>().m_homingTarget = GameObject.FindGameObjectWithTag("Capital");

					GameObject bullet2 = (GameObject)Network.Instantiate(m_bulletRef, m_firePoints[2].transform.position, m_firePoints[2].transform.rotation, 0);
					bullet2.GetComponent<BasicBulletScript>().firer = this.gameObject;
					bullet2.GetComponent<BasicBulletScript>().m_homingTarget = GameObject.FindGameObjectWithTag("Capital");
				}
				else if(counter == 1 || counter == 3)
				{
					GameObject bullet = (GameObject)Network.Instantiate(m_bulletRef, m_firePoints[1].transform.position, m_firePoints[1].transform.rotation, 0);
					bullet.GetComponent<BasicBulletScript>().firer = this.gameObject;
					bullet.GetComponent<BasicBulletScript>().m_homingTarget = GameObject.FindGameObjectWithTag("Capital");

					GameObject bullet2 = (GameObject)Network.Instantiate(m_bulletRef, m_firePoints[3].transform.position, m_firePoints[3].transform.rotation, 0);
					bullet2.GetComponent<BasicBulletScript>().firer = this.gameObject;
					bullet2.GetComponent<BasicBulletScript>().m_homingTarget = GameObject.FindGameObjectWithTag("Capital");
				}

				miniT = 0.0f;
				counter++;
			}

			yield return 0;
		}
	}

	public float GetRange()
	{
		BasicBulletScript script = m_bulletRef.GetComponent<BasicBulletScript>();
		return script.CalculateMaxDistance();
	}
	public float GetBulletSpeed()
	{
		return m_bulletRef.GetComponent<BasicBulletScript>().GetBulletSpeed();
	}
}

using UnityEngine;
using System.Collections;

public class BroadsideTurretScript : MonoBehaviour 
{
	[SerializeField]
	float m_activationRange = 40.0f;

	bool m_hasTarget = false;

	[SerializeField]
	GameObject[] m_firePoints;
	int m_currentFP = 0;

	[SerializeField]
	float m_sequentialFireTime = 3.5f;
	[SerializeField]
	int m_shotsInSequence = 7;

	[SerializeField]
	GameObject m_bulletRef;

	[SerializeField]
	float m_maxReload = 5.0f;
	float m_currentReload = 0.0f;

	// Use this for initialization
	void Start () 
	{
	
	}
	
	// Update is called once per frame
	void Update () 
	{
		if(Network.isServer)
		{
			if(m_currentReload < m_maxReload)
				m_currentReload += Time.deltaTime;

			int mask = 1 << 11;
			m_hasTarget = Physics.CheckSphere(this.transform.position, m_activationRange,mask); 

			if(m_hasTarget)
			{
				//Attempt fire
				if(m_coroutineHasFinished)
				{
					if(m_currentReload >= m_maxReload)
					{
						m_currentReload = 0.0f;
						StartCoroutine(SequentialFireLoop());
					}
				}
			}
		}
	}

	bool m_coroutineHasFinished = true;
	public float t = 0;
	public int i = 0;
	public bool isDone = false;
	IEnumerator SequentialFireLoop()
	{
		m_coroutineHasFinished = false;
		t = 0;
		isDone = false;
		i = 0;
		while(!isDone)
		{
			t += Time.deltaTime;
			
			if(t >= (m_sequentialFireTime / m_shotsInSequence))
			{
				t = 0;
				Vector3 pos = m_firePoints[m_currentFP].transform.position;
				pos.z += 0.05f;
				GameObject bullet = (GameObject)Network.Instantiate(m_bulletRef, pos + m_firePoints[m_currentFP].transform.up * 0.2f, m_firePoints[m_currentFP].transform.rotation, 0);
				bullet.GetComponent<BasicBulletScript>().firer = this.transform.parent.gameObject;

				m_currentFP++;
				if(m_currentFP >= m_firePoints.Length)
					m_currentFP = 0;

				i++;
				if(i >= m_shotsInSequence)
					isDone = true;
			}

			yield return 0;
		}

		m_coroutineHasFinished = true;
	}
}

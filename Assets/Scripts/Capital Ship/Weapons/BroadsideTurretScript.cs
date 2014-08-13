using UnityEngine;
using System.Collections;

public class BroadsideTurretScript : MonoBehaviour 
{
    /* Serializable members */
	[SerializeField,Range(10.0f, 100.0f)]       float m_activationRange = 40.0f;        // The range at which the broadside will begin firing
    [SerializeField]                            GameObject[] m_firePoints;              // The gameObjects representing the transform where the bullets will be spawned
	
    [SerializeField]                            float m_maxReload = 5.0f;               // The time it takes the weapon to reload between shots
	[SerializeField]                            float m_sequentialFireTime = 3.5f;      // The time it takes for the firing sequence to complete
	[SerializeField]                            int m_shotsInSequence = 7;              // The number of shots fired in a sequence

	[SerializeField]                            GameObject m_bulletRef;                 // A reference to the bullet to be fired

    /* Internal members */
    // Weapon members
    bool m_hasTarget = false;
    int m_currentFP = 0;
    float m_currentReload = 0.0f;

    // Sequence members
    bool m_coroutineHasFinished = true;
    float m_t = 0.0f;
    int m_i = 0;
    bool m_isDone = false;

	/* Unity default functions */
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

    /* Custom functions */

	IEnumerator SequentialFireLoop()
	{
		m_coroutineHasFinished = false;
		m_t = 0.0f;
		m_isDone = false;
		m_i = 0;
		while(!m_isDone)
		{
            m_t += Time.deltaTime;
			
            if(m_t >= (m_sequentialFireTime / m_shotsInSequence))
			{
                m_t = 0;
				Vector3 pos = m_firePoints[m_currentFP].transform.position;
				pos.z += 0.05f;
				GameObject bullet = (GameObject)Network.Instantiate(m_bulletRef, pos + m_firePoints[m_currentFP].transform.up * 0.2f, m_firePoints[m_currentFP].transform.rotation, 0);
				bullet.GetComponent<BasicBulletScript>().SetFirer(this.transform.parent.gameObject);

				m_currentFP++;
				if(m_currentFP >= m_firePoints.Length)
					m_currentFP = 0;

				m_i++;
				if(m_i >= m_shotsInSequence)
					m_isDone = true;
			}

			yield return 0;
		}

		m_coroutineHasFinished = true;
	}
}

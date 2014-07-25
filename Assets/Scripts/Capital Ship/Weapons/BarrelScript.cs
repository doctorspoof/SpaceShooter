using UnityEngine;
using System.Collections;

public class BarrelScript : MonoBehaviour 
{
	[SerializeField] float m_minY;
	[SerializeField] float m_idleY;

	float m_recoilTime;
	int m_numCannons;

	float m_totalTimeToRecoil;
    float m_recoilSequenceTimer;

	void Start () 
	{
		CapitalWeaponScript cwsc = transform.parent.GetComponent<CapitalWeaponScript>();
		m_recoilTime = cwsc.GetRecoilTime();
        m_numCannons = cwsc.GetNumCannons();

        m_totalTimeToRecoil = m_recoilTime * m_numCannons;
	}

	void Update () 
	{
        
	}

	public void Recoil()
	{
		StartCoroutine(BeginRecoilAnimation());
	}
	
    /// <summary>
    /// Handles the animation sequence for the barrel of the weapon.
    /// Will automatically calculate the time required to animate by looking at the time to fire and number of barrels.
    /// </summary>
	IEnumerator BeginRecoilAnimation()
	{
        m_recoilSequenceTimer = 0;
        while(m_recoilSequenceTimer < m_totalTimeToRecoil)
		{
            m_recoilSequenceTimer += Time.deltaTime;

            if(m_recoilSequenceTimer <= (m_totalTimeToRecoil * 0.25f))
			{
				//Recoiling
				//Get the 0->(1/4TTR) to 0->1
                float t = m_recoilSequenceTimer / (m_totalTimeToRecoil * 0.25f);
				float newY = Mathf.Lerp(m_idleY, m_minY, t);
				Vector3 localPos = new Vector3(transform.localPosition.x, newY, transform.localPosition.z);
				transform.localPosition = localPos;
			}
			else
			{
				//Extending
				//Get the 1/4TTR->TTR to 0->1
                float t = (m_recoilSequenceTimer - (m_totalTimeToRecoil * 0.25f)) / (m_totalTimeToRecoil / 3);
				float newY = Mathf.Lerp(m_minY, m_idleY, t);
				Vector3 localPos = new Vector3(transform.localPosition.x, newY, transform.localPosition.z);
				transform.localPosition = localPos;
			}

			yield return 0;
		}
	}
}

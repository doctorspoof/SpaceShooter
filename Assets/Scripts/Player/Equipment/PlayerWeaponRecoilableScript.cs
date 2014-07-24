using UnityEngine;
using System.Collections;

public class PlayerWeaponRecoilableScript : MonoBehaviour 
{
	[SerializeField]
	GameObject m_firePoint;

	public GameObject GetFirePoint()
	{
		return m_firePoint;
	}

	[SerializeField]
	float m_maxYReduction;
	[SerializeField]
	float m_idleYLevel;


	// Use this for initialization
	void Start () 
	{
	
	}
	
	// Update is called once per frame
	void Update () 
	{
	
	}

	public void RecoilThisBarrel(float timeToRecoil)
	{
		StartCoroutine(BeginRecoilAnimation(timeToRecoil));
	}

	float timer;
	IEnumerator BeginRecoilAnimation(float m_totalTimeToRecoil)
	{
		timer = 0;
		while(timer < m_totalTimeToRecoil)
		{
			timer += Time.deltaTime;
			
			if(timer <= (m_totalTimeToRecoil * 0.25f))
			{
				//Recoiling
				//Get the 0->(1/4TTR) to 0->1
				//float t = timer * 4;
				float t = timer / (m_totalTimeToRecoil * 0.25f);
				float newY = Mathf.Lerp(m_idleYLevel, m_maxYReduction, t);
				Vector3 localPos = new Vector3(transform.localPosition.x, newY, transform.localPosition.z);
				transform.localPosition = localPos;
			}
			else
			{
				//Extending
				//Get the 1/4TTR->TTR to 0->1
				//float t = (timer - (m_totalTimeToRecoil * 0.25f)) * 1.33333333333333333333333333333333333f;
				float t = (timer - (m_totalTimeToRecoil * 0.25f)) / (m_totalTimeToRecoil / 3);
				float newY = Mathf.Lerp(m_maxYReduction, m_idleYLevel, t);
				Vector3 localPos = new Vector3(transform.localPosition.x, newY, transform.localPosition.z);
				transform.localPosition = localPos;
			}
			
			yield return 0;
		}
	}
}

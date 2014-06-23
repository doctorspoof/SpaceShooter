using UnityEngine;
using System.Collections;

public class BarrelScript : MonoBehaviour 
{

	[SerializeField]
	float m_minY;
	[SerializeField]
	float m_idleY;

	float m_recoilTime;
	int numCannons;

	float m_totalTimeToRecoil;

	// Use this for initialization
	void Start () 
	{
		CapitalWeaponScript cwsc = transform.parent.GetComponent<CapitalWeaponScript>();
		m_recoilTime = cwsc.GetRecoilTime();
		numCannons = cwsc.GetNumCannons();

		m_totalTimeToRecoil = m_recoilTime * numCannons;
	}

	void Update () 
	{

	}

	public void Recoil()
	{
		StartCoroutine(BeginRecoilAnimation());
	}

	float timer;
	IEnumerator BeginRecoilAnimation()
	{
		timer = 0;
		while(timer < m_totalTimeToRecoil)
		{
			timer += Time.deltaTime;

			if(timer <= (m_totalTimeToRecoil * 0.25f))
			{
				//Recoiling
				//Get the 0->(1/4TTR) to 0->1
				float t = timer / (m_totalTimeToRecoil * 0.25f);
				float newY = Mathf.Lerp(m_idleY, m_minY, t);
				Vector3 localPos = new Vector3(transform.localPosition.x, newY, transform.localPosition.z);
				transform.localPosition = localPos;
			}
			else
			{
				//Extending
				//Get the 1/4TTR->TTR to 0->1
				float t = (timer - (m_totalTimeToRecoil * 0.25f)) / (m_totalTimeToRecoil / 3);
				float newY = Mathf.Lerp(m_minY, m_idleY, t);
				Vector3 localPos = new Vector3(transform.localPosition.x, newY, transform.localPosition.z);
				transform.localPosition = localPos;
			}

			yield return 0;
		}
	}
}

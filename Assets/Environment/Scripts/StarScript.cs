using UnityEngine;
using System.Collections;

public class StarScript : MonoBehaviour 
{
	[SerializeField]
	int m_damagePerTick = 1;

	[SerializeField]
	float m_tickTime = 0.05f;

	// Use this for initialization
	void Start () 
	{
	
	}
	
	// Update is called once per frame
	void Update () 
	{

	}

	float m_damageDelay = 0.0f;
	void OnTriggerStay(Collider other)
	{
		if(other.tag == "Capital" || other.tag == "Enemy" || other.tag == "Player")
		{
			m_damageDelay += Time.deltaTime;
			if(m_damageDelay >= m_tickTime)
			{
				other.gameObject.GetComponent<HealthScript>().DamageMobHullDirectly(m_damagePerTick);
				m_damageDelay = 0.0f;
			}
		}
	}
}

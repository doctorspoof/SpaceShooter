using UnityEngine;
using System.Collections;

public class EnemySupportShieldScript : MonoBehaviour 
{

	[SerializeField]
	float m_maxShield;
	[SerializeField]
	float m_currentShield;

	[SerializeField]
	float m_shieldRechargeRate;
	[SerializeField]
	float m_shieldRechargeDelay;
	float m_currentRechargeDelay;

	// Use this for initialization
	void Start () 
	{
		m_currentShield = m_maxShield;
	}
	
	// Update is called once per frame
	void Update () 
	{
		if(m_currentRechargeDelay <= 0.0f)
		{
			if(!this.renderer.enabled)
				this.renderer.enabled = true;

			m_currentShield += m_shieldRechargeRate * Time.deltaTime;

			if(m_currentShield > m_maxShield)
				m_currentShield = m_maxShield;
		}
		else
		{
			m_currentRechargeDelay -= Time.deltaTime;
		}
	}

	void DamageShield(int amount)
	{
		m_currentShield -= amount;
		if(m_currentShield <= 0)
		{
			m_currentShield = 0;
			this.renderer.enabled = false;
		}
	}

	public bool OnTriggerEnter(Collider other)
	{
		float distance = Vector3.Distance(this.transform.position, other.transform.position);
		if(distance > 11.0f)
		{
			if(m_currentShield > 0)
			{
				GameObject bullet = other.gameObject;

				DamageShield(bullet.GetComponent<BasicBulletScript>().GetDamage());

				bullet.GetComponent<BasicBulletScript>().DetonateBullet();
				BeginShaderCoroutine(other.transform.position);
				m_currentRechargeDelay = 0.0f;
				return true;
			}
			else
				return false;
		}
		else
			return false;
	}
	void OnCollisionEnter(Collision collision)
	{
		//If the collider is too close, they're already inside the shield area
		float distance = Vector3.Distance(this.transform.position, collision.collider.attachedRigidbody.position);
		Debug.Log ("Bullet collided with a distance of: " + distance);
		if(distance > 4.5f)
		{
			if(m_currentShield > 0)
			{
				GameObject bullet = collision.collider.attachedRigidbody.gameObject;

				//Collide with the bullet
				m_currentShield -= bullet.GetComponent<BasicBulletScript>().GetDamage();

				bullet.GetComponent<BasicBulletScript>().DetonateBullet();
				BeginShaderCoroutine(collision.contacts[0].point);
			}
		}
	}

	//Do shield fizzle wizzle
	int shaderCounter = 0;
	public void BeginShaderCoroutine(Vector3 position)
	{
		//Debug.Log ("Bullet collision, beginning shader coroutine");
		Vector3 pos = this.transform.InverseTransformPoint(position);
		pos = new Vector3(pos.x * this.transform.localScale.x, pos.y * this.transform.localScale.y, pos.z);
		this.renderer.material.SetVector("_ImpactPos" + (shaderCounter + 1).ToString(), new Vector4(pos.x, pos.y, pos.z, 1));
		this.renderer.material.SetFloat("_ImpactTime" + (shaderCounter + 1).ToString(), 1.0f);
		
		/*if(coroutineIsRunning)
        {
            coroutineIsRunning = false;
            StartCoroutine(AwaitCoroutineStopped());
        }
        else*/
		StartCoroutine(ReduceShieldEffectOverTime(shaderCounter));
		
		++shaderCounter;
		if (shaderCounter >= 4)
			shaderCounter = 0;
	}
	
	bool coroutineIsRunning = false;
	bool coroutineForceStopped = false;
	IEnumerator ReduceShieldEffectOverTime(int i)
	{
		float t = 0;
		coroutineIsRunning = true;
		//while(t <= 1.0f && coroutineIsRunning)
		while (t <= 1.0f)
		{
			t += Time.deltaTime;
			float time = this.renderer.material.GetFloat("_ImpactTime" + (i + 1).ToString());
			
			//oldImp.w = 1.0f - t;
			
			this.renderer.material.SetFloat("_ImpactTime" + (i + 1).ToString(), 1.0f - t);
			yield return 0;
		}
		
		/*if(!coroutineIsRunning)
                coroutineForceStopped = true;*/
		
		coroutineIsRunning = false;
	}
}

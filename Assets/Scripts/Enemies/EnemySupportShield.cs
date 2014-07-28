using UnityEngine;
using System.Collections;

public class EnemySupportShield : MonoBehaviour 
{
	/// Unity modifiable variables
	// Shield capacity
	[SerializeField, Range (0f, 10000f)] float m_maxShield = 1000f;			// The capacity of the shield
	[SerializeField, Range (0f, 100f)] float m_shieldSize = 1f;				// How large the shield is

	// Recharge rate
	[SerializeField, Range (0f, 1000f)] float m_shieldRechargeRate = 10f;	// How quickly the shield recharges
	[SerializeField, Range (0f, 120f)] float m_shieldRechargeDelay = 10f;	// How long to wait before the shield starts recharging


	/// Internal data
	float m_currentShield = 0f;				// The current shield value
	float m_currentRechargeDelay = 0f;		// The current delay
	float m_vulnerableMagnitude = 0f;		// The distance used in determining whether bullets will be able to effect the ship itself
	
	int shaderCounter = 0;					// A counter for how many times the shield is wibbling

	bool m_coroutineIsRunning = false;		// Used to regulate the shield wibble
	bool m_coroutineForceStopped = false;	// Used to regulate the shield wibble



	/// Functions
	// Perform preliminary shield setup
	void Start() 
	{
		m_currentShield = m_maxShield;

		ResetShieldScale();

		// Calculate the vulnerability distance
		float distanceModifier = (collider as SphereCollider).radius * 2f;
		m_vulnerableMagnitude = (m_shieldSize * distanceModifier * 0.9f).Squared();
	}
	

	// Handle the shield recharging functionality
	void Update() 
	{
		if (m_currentRechargeDelay <= 0f)
		{
			if (!this.renderer.enabled)
			{
				this.renderer.enabled = true;
				this.collider.enabled = true;
			}

			// Recharge the shield
			m_currentShield += m_shieldRechargeRate * Time.deltaTime;

			// Clamp the shield value
			m_currentShield = Mathf.Min (m_currentShield, m_maxShield);
		}

		else
		{
			m_currentRechargeDelay -= Time.deltaTime;
		}
	}


	// Absorb bullet damage if possible
	public bool OnTriggerEnter (Collider other)
	{
		float sqrMagnitude = (transform.position - other.transform.position).sqrMagnitude;

		if (sqrMagnitude > m_vulnerableMagnitude && other.attachedRigidbody != null)
		{
			if (m_currentShield > 0f)
			{

				// Don't handle beam damage
				BasicBulletScript bullet = other.attachedRigidbody.GetComponent<BasicBulletScript>();

				if (bullet != null)
				{
					// Handle the bullet damage
					DamageShield (bullet.GetDamage());
					bullet.DetonateBullet();

					// Wibble that shield baby!
					BeginShaderCoroutine (other.transform.position);
					return true;
				}
			}
		}

		return false;
	}
	
	
	// Reset the shield scale so that it is indepentant of the root scale
	void ResetShieldScale()
	{
		// Calculate the modifier whilst avoiding divide by zero errors
		float scaleModifier = Mathf.Max (Mathf.Max (transform.root.localScale.x, transform.root.localScale.y), 0.1f);
		
		// Calculate the shield size
		float shieldSize = m_shieldSize / scaleModifier * 2f;
		
		// Set the scale
		Vector3 scale = new Vector3 (shieldSize, shieldSize, transform.localScale.z);
		transform.localScale = scale;
	}
	
	
	// Handle damage to the shield
	public void DamageShield (int amount)
	{
		m_currentShield -= amount;
		m_currentRechargeDelay = m_shieldRechargeDelay;
		
		if (m_currentShield <= 0f)
		{
			m_currentShield = 0f;
			this.renderer.enabled = false;
			this.collider.enabled = false;
		}
	}
	

	public void BeginShaderCoroutine (Vector3 position)
	{
		Vector3 pos = this.transform.InverseTransformPoint (position);
		pos = new Vector3 (pos.x * this.transform.localScale.x * 6.0f, pos.y * this.transform.localScale.y * 6.0f, pos.z);

		this.renderer.material.SetVector ("_ImpactPos" + (shaderCounter + 1).ToString(), new Vector4 (pos.x, pos.y, pos.z, 1f));
		this.renderer.material.SetFloat ("_ImpactTime" + (shaderCounter + 1).ToString(), 1.0f);

		StartCoroutine (ReduceShieldEffectOverTime (shaderCounter));
		
		++shaderCounter;
		if (shaderCounter >= 4)
		{
			shaderCounter = 0;
		}
	}


	IEnumerator ReduceShieldEffectOverTime (int i)
	{
		float t = 0;
		m_coroutineIsRunning = true;
		//while(t <= 1.0f && coroutineIsRunning)
		while (t <= 1.0f)
		{
			t += Time.deltaTime;
			//float time = this.renderer.material.GetFloat ("_ImpactTime" + (i + 1).ToString());
			
			//oldImp.w = 1.0f - t;
			
			this.renderer.material.SetFloat ("_ImpactTime" + (i + 1).ToString(), 1.0f - t);
			yield return 0;
		}
		
		/*if(!coroutineIsRunning)
                coroutineForceStopped = true;*/
		
		m_coroutineIsRunning = false;
	}
}

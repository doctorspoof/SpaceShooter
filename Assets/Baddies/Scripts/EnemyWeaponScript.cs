using UnityEngine;
using System.Collections;

public class EnemyWeaponScript : MonoBehaviour {

	[SerializeField]
	float m_recoilTime;
	float m_currentRecoil;
	[SerializeField]
	GameObject m_bulletRef;
	
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
	}
	
	public void MobRequestsFire()
	{
		if(m_currentRecoil >= m_recoilTime)
		{
			//Fire!
			GameObject bullet = (GameObject)Network.Instantiate(m_bulletRef, this.transform.position + new Vector3(0, 0, 0.1f), this.transform.rotation, 0);
			bullet.GetComponent<BasicBulletScript>().firer = this.gameObject;
			m_currentRecoil = 0;
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

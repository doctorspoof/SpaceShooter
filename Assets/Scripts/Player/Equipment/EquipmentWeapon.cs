using UnityEngine;
using System.Collections;

public class EquipmentWeapon : MonoBehaviour
{

    [SerializeField] Vector3 m_requiredOffset;

    [SerializeField] GameObject[] m_barrels;
    [SerializeField] bool m_barrelsShouldRecoil;
    [SerializeField] bool m_singleFirePointPerShot = true;
    [SerializeField] bool m_fireAllShotsAtOnce = true;
    [SerializeField] float m_sequentialFireTime = 0.5f;

    [SerializeField] float m_spreadFactor = 0;
    [SerializeField] bool m_evenSpread = true;

    [SerializeField] int m_shotsPerFire = 1;

    [SerializeField] bool m_isBeam;

    [SerializeField] float m_beamRechargeDelay = 1.0f;
    [SerializeField] float m_currentRechargeDelay = 0.0f;

    [SerializeField] float m_recoilTime;
    [SerializeField] float m_currentRecoil;
    [SerializeField] GameObject m_bulletRef;

    [SerializeField] Vector3 m_bulletOffset = Vector3.zero;

    [SerializeField] bool m_needsLockon = false;




    int m_currentBarrelNum = 0;

    GameObject[] m_currentBeams = null;

    GameObject m_currTarget = null;

    bool m_isBeaming = false;

    bool m_coroutineHasFinished = true;



    #region getset

    public Vector3 GetOffset()
    {
        return m_requiredOffset;
    }

    public bool GetNeedsLockon()
    {
        return m_needsLockon;
    }

    public void SetNeedsLockon(bool flag_)
    {
        m_needsLockon = flag_;
    }

    public float GetReloadPercentage()
    {
        return m_currentRecoil / m_recoilTime;
    }

    public float GetBulletMaxDistance()
    {
        return m_bulletRef.GetComponent<BasicBulletScript>().CalculateMaxDistance();
    }

    public bool GetIsBeam()
    {
        return m_isBeam;
    }

    #endregion getset

    void Start()
    {
        m_currentBeams = new GameObject[m_shotsPerFire];
    }

    void Update()
    {
        if (m_isBeaming)
        {
            m_currentRecoil -= Time.deltaTime;
            if (m_currentRecoil < 0.0f)
            {
                //Stop the beam!
                networkView.RPC("StopFiringBeamAcrossNetwork", RPCMode.All);
            }
        }

        if (!m_isBeaming)
        {
            if (m_currentRechargeDelay < m_beamRechargeDelay)
            {
                m_currentRechargeDelay += Time.deltaTime;
            }
            else if (m_currentRecoil < m_recoilTime)
            {
                m_currentRecoil += Time.deltaTime;
            }
        }
    }

    public void SetTarget(GameObject target)
    {
        if (target != null)
        {
            m_currTarget = target;
            networkView.RPC("PropagateTarget", RPCMode.Others, target.networkView.viewID, false);
        }
        else
        {
            UnsetTarget();
        }
    }

    public void UnsetTarget()
    {
        m_currTarget = null;
        networkView.RPC("PropagateTarget", RPCMode.Others, networkView.viewID, true);
    }

    public bool CheckCanFire()
    {
        if (m_isBeam)
        {
            if (m_currentRecoil > 1.0f)
                return true;
            else
                return false;
        }
        else
        {
            if (m_currentRecoil >= m_recoilTime)
                return true;
            else
                return false;
        }
    }
    
    public void ActAsFired()
    {
        if (m_isBeam)
        {
            m_isBeaming = true;
        }
        else
            m_currentRecoil = 0;
    }

    [RPC] void StopFiringBeamAcrossNetwork()
    {
        AlertBeamWeaponNotFiring();
    }

    public void AlertBeamWeaponNotFiring()
    {
        m_isBeaming = false;
        //Network.Destroy(currentBeam);
        for (int i = 0; i < m_currentBeams.Length; i++)
            Network.Destroy(m_currentBeams[i]);

        m_currentBeams = new GameObject[m_shotsPerFire];
        m_currentRechargeDelay = 0.0f;

        networkView.RPC("StopPlayingSoundOverNetwork", RPCMode.All);
    }

    public void PlayerRequestsFire()
    {
        if (m_isBeam)
        {
            //Different recoil/reload system, different script to access on bullet
            if (m_currentRecoil > 1.0f)
            {
                ShootBeam();
            }
            /*else
            {
                isBeaming = false;
                for(int i = 0; i < currentBeams.Length; i++)
                    Network.Destroy (currentBeams[i]);
				
                currentBeams = new GameObject[m_shotsPerFire];
            }*/
        }
        else
        {
            if (m_currentRecoil >= m_recoilTime)
            {
                ShootBasic();
            }
        }
    }

    public void PlayerRequestsFireNoRecoilCheck()
    {
        if (m_isBeam)
        {
            ShootBeam();
        }
        else
        {
            ShootBasic();
        }
    }

    void ShootBeam()
    {
        float spreadIncrement = (m_spreadFactor * 2) / m_shotsPerFire;
        for (int i = 0; i < m_shotsPerFire; i++)
        {
            float randAngle;
            if (m_evenSpread)
                randAngle = -m_spreadFactor + (spreadIncrement * i);
            else
                randAngle = Random.Range(-m_spreadFactor, m_spreadFactor + 1.0f);



            if (!m_isBeaming && m_currentBeams[i] == null)
            {
                Quaternion bulletRot = this.transform.rotation * Quaternion.Euler(0, 0, -randAngle);
                //Quaternion bulletRot = this.transform.rotation;

                GameObject bullet = (GameObject)Network.Instantiate(m_bulletRef, this.transform.position, bulletRot, 0);
                bullet.transform.parent = this.transform;
                bullet.transform.localScale = new Vector3(bullet.transform.localScale.x, 0f, bullet.transform.localScale.z);
                bullet.GetComponent<BeamBulletScript>().SetOffset(m_bulletOffset);
                bullet.GetComponent<BeamBulletScript>().SetFirer(transform.gameObject);

                GameStateController gsc = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameStateController>();
                bullet.GetComponent<BeamBulletScript>().ParentBeamToFirer(gsc.GetNameFromNetworkPlayer(transform.parent.GetComponent<PlayerControlScript>().GetOwner()));
                m_currentBeams[i] = bullet;

                if (i >= (m_shotsPerFire - 1))
                    m_isBeaming = true;

                networkView.RPC("PlaySoundOverNetwork", RPCMode.All);
            }
        }
    }

    void ShootBasic()
    {
        float spreadIncrement = (m_spreadFactor * 2) / m_shotsPerFire;
        if (m_fireAllShotsAtOnce)
        {
            for (int i = 0; i < m_shotsPerFire; i++)
            {
                //SpawnBasicBullet(i, spreadIncrement);
                NetworkViewID id = Network.AllocateViewID();
                networkView.RPC("SpawnBasicBullet", RPCMode.All, i, spreadIncrement, id);

                if (!m_singleFirePointPerShot)
                {
                    //Increment barrel num
                    ++m_currentBarrelNum;
                    if (m_currentBarrelNum >= m_barrels.Length)
                    {
                        m_currentBarrelNum = 0;
                    }
                }
            }

            if (m_singleFirePointPerShot)
            {
                //Increment barrel num
                ++m_currentBarrelNum;
                if (m_currentBarrelNum >= m_barrels.Length)
                {
                    m_currentBarrelNum = 0;
                }
            }
            m_currentRecoil = 0;
        }
        else
        {
            if (m_coroutineHasFinished)
            {
                m_currentRecoil = 0;
                StartCoroutine(SequentialFireLoop(m_sequentialFireTime, m_shotsPerFire, spreadIncrement));
            }
        }

        networkView.RPC("PlaySoundOverNetwork", RPCMode.All);
    }

    [RPC] void SpawnBasicBullet(int bulletNum, float spreadInc, NetworkViewID id)
    {
        float randAngle;
        if (m_evenSpread)
            randAngle = -m_spreadFactor + (spreadInc * bulletNum);
        else
            randAngle = Random.Range(-m_spreadFactor, m_spreadFactor + 1.0f);
        Quaternion bulletRot = this.transform.rotation * Quaternion.Euler(0, 0, randAngle);
        //Debug.Log ("Firing bullet with additional angle of: " + randAngle);

        Vector3 bulletPos;
        if (m_barrelsShouldRecoil)
        {
            bulletPos = m_barrels[m_currentBarrelNum].GetComponent<Barrel>().GetFirePoint().transform.position;
        }
        else
        {
            bulletPos = m_barrels[m_currentBarrelNum].transform.position;
        }

        GameObject bullet = (GameObject)Instantiate(m_bulletRef, bulletPos + m_bulletOffset, bulletRot);
        if (m_currTarget != null)
        {
            bullet.GetComponent<BasicBulletScript>().SetHomingTarget(m_currTarget);
        }

        bullet.networkView.viewID = id;

        if (m_barrelsShouldRecoil)
        {
            networkView.RPC("RecoilBarrelOverNetwork", RPCMode.All, m_currentBarrelNum);
        }

        BasicBulletScript bulletScript = bullet.GetComponent<BasicBulletScript>();
        bulletScript.SetFirer(transform.parent.gameObject);

        // If the attached GameObject has a rigidbody use its magnitude.
        if (transform.parent.rigidbody)
        {
            bulletScript.SetBulletSpeedModifier(Vector3.Dot(transform.parent.transform.up, transform.parent.rigidbody.velocity));
        }
    }

    
    IEnumerator SequentialFireLoop(float m_time, int m_shots, float m_spreadIncrement)
    {
        m_coroutineHasFinished = false;

        float t = 0;
        bool isDone = false;
        int i = 0;
        while (!isDone)
        {
            t += Time.deltaTime;

            if (t >= (m_time / m_shots))
            {
                //Fire a shot
                NetworkViewID id = Network.AllocateViewID();
                networkView.RPC("SpawnBasicBullet", RPCMode.All, i, 0.0f, id);

                //Increment barrel num
                if (!m_singleFirePointPerShot)
                {
                    ++m_currentBarrelNum;
                    if (m_currentBarrelNum >= m_barrels.Length)
                    {
                        m_currentBarrelNum = 0;
                    }
                }
                i++;
                t = 0;

                if (i >= m_shots)
                    isDone = true;
            }

            yield return 0;
        }

        m_coroutineHasFinished = true;
    }

    [RPC] void RecoilBarrelOverNetwork(int barrelID)
    {
        float timeToRecoil = m_recoilTime * m_barrels.Length;
        m_barrels[barrelID].GetComponent<Barrel>().Recoil(timeToRecoil);
    }

    [RPC] void PropagateTarget(NetworkViewID id, bool unset)
    {
        if (!unset)
        {
            // Search for the target based on NetworkViewID's
            NetworkView found = NetworkView.Find(id);
            m_currTarget = found ? found.gameObject : null;
        }

        else
        {
            m_currTarget = null;
        }
    }

    [RPC] void PlaySoundOverNetwork()
    {
        if (this.audio != null)
        {
            if (m_isBeam)
            {
                this.audio.volume = PlayerPrefs.GetFloat("EffectVolume");
                this.audio.Play();
            }
            else
            {
                this.audio.PlayOneShot(this.audio.clip, PlayerPrefs.GetFloat("EffectVolume"));
            }
        }
    }

    [RPC] void StopPlayingSoundOverNetwork()
    {
        if (this.audio != null)
        {
            this.audio.Stop();
        }
    }

    public void ParentWeaponToOwner(string player)
    {
        //Debug.Log ("Passing name: " + player + " through to parent weapon process.");
        networkView.RPC("TellWeaponParentToPlayerThroughNetworkPlayer", RPCMode.Others, player);
    }

    [RPC] void TellWeaponParentToPlayerThroughNetworkPlayer(string player)
    {
        GameStateController gsc = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameStateController>();

        GameObject playerGO = gsc.GetPlayerFromNetworkPlayer(gsc.GetNetworkPlayerFromID(gsc.GetIDFromName(player)));
        Debug.Log("Attaching weapon: " + this.name + " to gameObject: " + playerGO.name + ", through name: " + player);
        this.transform.parent = playerGO.transform;
        this.transform.localPosition = m_requiredOffset;
    }
}

using UnityEngine;
using System.Collections;

public class RunFinalSpawnEffect : MonoBehaviour
{
    [SerializeField] GameObject finalEffectPrefab;

    GameObject m_finalEffect;
    // Use this for initialization
    void Start()
    {
        m_finalEffect = (GameObject)Instantiate(finalEffectPrefab, transform.position, Quaternion.identity);
        m_finalEffect.transform.localScale = transform.localScale;
    }

    /// <summary>
    /// Makes the effect happen after a specified amount of time
    /// </summary>
    /// <param name="time_">Time in seconds</param>
    public void Run(float time_)
    {
        Invoke("Fire", time_);
    }

    void Fire()
    {
        if (m_finalEffect != null)
        {
            m_finalEffect.GetComponent<MeshRenderer>().enabled = true;
            m_finalEffect.GetComponent<SpriteSheet>().enabled = true;
            m_finalEffect.GetComponent<SpriteSheet>().SetCurrentFrame(0);
        }
    }

}

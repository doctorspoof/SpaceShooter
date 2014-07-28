
using UnityEngine;
using System.Collections;

public class Delay : MonoBehaviour
{

    float m_timeToDelay = 0, m_currentTime = 0;

    MeshRenderer m_renderer;
    SpriteSheet m_spriteSheet;

    void Start()
    {
        m_renderer = GetComponent<MeshRenderer>();
        m_renderer.enabled = false;

        m_spriteSheet = GetComponent<SpriteSheet>();
        m_spriteSheet.enabled = false;
    }

    void Update()
    {

        m_currentTime += Time.deltaTime;
        if (m_currentTime >= m_timeToDelay)
        {
            m_renderer.enabled = true;
            m_spriteSheet.enabled = true;
            m_spriteSheet.m_frameOffset = 0;

            if (audio != null)
            {
                audio.volume = PlayerPrefs.GetFloat("EffectVolume", 1.0f);
                audio.Play();
            }

        }

    }

    /// <summary>
    /// Time until script activates the renderer and spritesheet
    /// </summary>
    /// <param name="delay_">Time in seconds</param>
    public void SetDelay(float delay_)
    {
        m_timeToDelay = delay_;
    }
}


using UnityEngine;
using System.Collections;

public class FadeOut : MonoBehaviour
{

    float m_timeBeforeFadeOutStarts = 0, m_fadeOutTime = 0;
    float m_currentTime = 0;

    Material m_mat;

    bool m_started = false;

    void Start()
    {
        m_mat = GetComponent<MeshRenderer>().material;
    }

    void Update()
    {
        if (m_started == true)
        {
            m_currentTime += Time.deltaTime;
            if (m_currentTime >= m_timeBeforeFadeOutStarts)
            {
                if (m_mat != null)
                {
                    float t = ((m_timeBeforeFadeOutStarts + m_fadeOutTime - m_currentTime) / m_fadeOutTime);
                    m_mat.color = new Color(m_mat.color.r, m_mat.color.g, m_mat.color.b, t);
                }

            }

            if (m_currentTime >= m_timeBeforeFadeOutStarts + m_fadeOutTime)
            {
                Destroy(gameObject);
            }

        }
    }

    /// <summary>
    /// Set the times for how long the fade out takes
    /// </summary>
    /// <param name="fadeOutTime_">How long the actual fading takes in seconds</param>
    /// <param name="timeBeforeFadeOutStarts_">Time before the fading starts in seconds</param>
    public void SetTimes(float fadeOutTime_, float timeBeforeFadeOutStarts_)
    {
        m_fadeOutTime = fadeOutTime_;
        m_timeBeforeFadeOutStarts = timeBeforeFadeOutStarts_;
    }

    public void StartFadeOut()
    {
        m_started = true;
    }
}

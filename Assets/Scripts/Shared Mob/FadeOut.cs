using UnityEngine;



/// <summary>
/// Fade out is used to create an alpha fade out effect on queue. This is useful when we want objects to fade out instead of just disappearing.
/// </summary>
[RequireComponent (typeof (MeshRenderer))]
public class FadeOut : MonoBehaviour
{
    #region Internal data
    
    bool m_started = false;                 //!< Used by Update() to check if the fading process has started or not.

    float m_currentTime = 0f;               //!< Used to keep track of the desired alpha value for the fade.
    float m_timeBeforeFadeOutStarts = 0f;   //!< How long to wait before actually fading out.
    float m_fadeOutTime = 0f;               //!< How long it should take for the fade to take.

    Material m_mat;                         //!< A reference to the material which will be faded out.

    #endregion Internal data


    #region Behavior functions

    /// <summary>
    /// Awake obtains the material reference needed to fade out the alpha value.
    /// </summary>
    void Awake()
    {
        m_mat = GetComponent<MeshRenderer>().material;
    }


    /// <summary>
    /// Performs the fading out process once started which is triggered by StartFadeOut().
    /// </summary>
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
                Debug.Log ("FadeOut destroyed: " + gameObject.name);
                Destroy(gameObject);
            }

        }
    }

    #endregion Behavior functions


    #region Set up functions

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


    /// <summary>
    /// Starts the fade out. Also resets the current timer to 0f.
    /// </summary>
    public void StartFadeOut()
    {
        m_started = true;
        m_currentTime = 0f;
    }

    #endregion
}

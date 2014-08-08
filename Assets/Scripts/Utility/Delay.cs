using UnityEngine;



/// <summary>
/// Delay is used in the creation of explosions and other effects. Using Delay allows explosions to be triggered at different, random intervals.
/// </summary>
[RequireComponent (typeof (MeshRenderer))]
[RequireComponent (typeof (SpriteSheet))]
public class Delay : MonoBehaviour
{
    #region Internal data

    MeshRenderer m_renderer;    //!< A reference to the MeshRenderer used for the delay.
    SpriteSheet m_spriteSheet;  //!< A reference to the SpriteSheet used for the delay.

    #endregion


    #region Behavior functions

    /// <summary>
    /// Obtains a reference to the MeshRenderer and the SpriteSheet so the delay can function properly.
    /// </summary>
    void Awake()
    {
        m_renderer = GetComponent<MeshRenderer>();
        m_renderer.enabled = false;

        m_spriteSheet = GetComponent<SpriteSheet>();
        m_spriteSheet.enabled = false;
    }

    #endregion


    #region Delay functionality
    
    /// <summary>
    /// Time until script activates the renderer and spritesheet
    /// </summary>
    /// <param name="delay_">Time in seconds</param>
    public void SetDelay (float delay_)
    {
        if (delay_ >= 0f)
        {
            CancelInvoke ("ShowSprites");
            Invoke ("ShowSprites", delay_);
        }
    }


    /// <summary>
    /// Called to enable the MeshRenderer and SpriteSheet.
    /// </summary>
    void ShowSprites()
    {
        m_renderer.enabled = true;
        m_spriteSheet.enabled = true;
        m_spriteSheet.SetCurrentFrame(0);

        if (audio != null)
        {
            audio.volume = PlayerPrefs.GetFloat("EffectVolume", 1.0f);
            audio.Play();
        }
    }

    #endregion
}

using UnityEngine;
using System.Collections;

/// <summary>
/// Mimics the behavour in SpriteSheet that kills the object after a single run through all frames
/// </summary>
public class FakeSpriteSheetDeath : MonoBehaviour 
{
    float m_fps = 30;
    int m_frameNum = 0;
    
    float m_timeToDeath = 0.0f;
    float m_currentLifetime = 0.0f;

	// Use this for initialization
	void Start () 
    {
        m_fps = renderer.material.GetFloat("_FPS");
        m_frameNum = renderer.material.GetInt("_FrameNumX") * renderer.material.GetInt("_FrameNumY");
        
        m_timeToDeath = (float)m_frameNum / m_fps;
        m_currentLifetime = 0.0f;
	}
	
	// Update is called once per frame
	void Update () 
    {
	    m_currentLifetime += Time.deltaTime;
        
        if(m_currentLifetime >= m_timeToDeath)
        {
            Destroy (this.gameObject);
        }
	}
}

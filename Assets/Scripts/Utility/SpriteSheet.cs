using UnityEngine;
using System.Collections;

public class SpriteSheet : MonoBehaviour
{
    [SerializeField] int m_uvTieX = 1;
    [SerializeField] int m_uvTieY = 1;
    [SerializeField] int m_fps = 10;
    [SerializeField] int m_frameOffset = 0;

    [SerializeField] bool m_shouldReverse = false;

    [SerializeField] bool m_isForwards = true;

    [SerializeField] bool shouldDieAfterFirstRun = false;

    Vector2 m_size;
    Renderer m_myRenderer;
    int m_lastIndex = -1;

    float m_floatCatch = 0.0f;
    int m_currentIndex = 0;

    #region getset

    public void SetShouldDieAfterFirstRun(bool flag_)
    {
        shouldDieAfterFirstRun = flag_;
    }

    public void SetCurrentFrame(int index_)
    {
        if (m_myRenderer == null)
        {
            Start();
        }

        m_currentIndex = index_;

        int uIndex = m_currentIndex % m_uvTieX;
        int vIndex = m_currentIndex / m_uvTieY;

        // build offset
        // v coordinate is the bottom of the image in opengl so we need to invert.
        Vector2 offset = new Vector2(uIndex * m_size.x, 1.0f - m_size.y - vIndex * m_size.y);

        m_myRenderer.material.SetTextureOffset("_MainTex", offset);
        m_myRenderer.material.SetTextureScale("_MainTex", m_size);
    }

    public int GetFrameOffset()
    {
        return m_frameOffset;
    }

    public void SetFrameOffset(int offset_)
    {
        m_frameOffset = offset_;
    }

    #endregion getset

    void Start()
    {
        m_size = new Vector2(1.0f / m_uvTieX, 1.0f / m_uvTieY);
        m_myRenderer = renderer;
        if (m_myRenderer == null)
            enabled = false;

        m_currentIndex = m_frameOffset;
    }


    //My way vars
    

    // Update is called once per frame
    void Update()
    {
        if (m_isForwards)
        {
            // Calculate index
            //int index = (int)(Time.timeSinceLevelLoad * _fps) % (_uvTieX * _uvTieY);
            m_floatCatch += Time.deltaTime;
            //if(index != _lastIndex)
            if (m_floatCatch >= 1.0f / m_fps)
            {
                m_floatCatch = 0.0f;
                m_currentIndex++;
                // split into horizontal and vertical index
                int uIndex = m_currentIndex % m_uvTieX;
                int vIndex = m_currentIndex / m_uvTieY;

                // build offset
                // v coordinate is the bottom of the image in opengl so we need to invert.
                Vector2 offset = new Vector2(uIndex * m_size.x, 1.0f - m_size.y - vIndex * m_size.y);

                m_myRenderer.material.SetTextureOffset("_MainTex", offset);
                m_myRenderer.material.SetTextureScale("_MainTex", m_size);

                m_lastIndex = m_currentIndex;

                if (m_currentIndex > ((m_uvTieX * m_uvTieY) - 2))
                {
                    if (m_shouldReverse)
                    {
                        m_isForwards = false;
                        //m_currentIndex = m_currentIndex;
                    }
                    else if (shouldDieAfterFirstRun)
                    {
                        Destroy(this.gameObject);
                    }
                }
            }
        }
        else
        {
            m_floatCatch += Time.deltaTime;
            if (m_floatCatch >= 1.0f / m_fps)
            {
                m_floatCatch = 0.0f;

                //Time to increment frame
                m_currentIndex--;

                // split into horizontal and vertical index
                int uIndex = m_currentIndex % m_uvTieX;
                int vIndex = m_currentIndex / m_uvTieY;

                // build offset
                // v coordinate is the bottom of the image in opengl so we need to invert.
                Vector2 offset = new Vector2(uIndex * m_size.x, 1.0f - m_size.y - vIndex * m_size.y);

                m_myRenderer.material.SetTextureOffset("_MainTex", offset);
                m_myRenderer.material.SetTextureScale("_MainTex", m_size);

                if (m_currentIndex == 0)
                {
                    //Reached the start again!
                    m_isForwards = true;
                }
            }
        }
    }

    
}
using UnityEngine;
using System.Collections;

public class SpriteSheet : MonoBehaviour 
{
	public int _uvTieX = 1;
	public int _uvTieY = 1;
	public int _fps = 10;
	public int _frameOffset = 0;

	private Vector2 _size;
	private Renderer _myRenderer;
	private int _lastIndex = -1;

	[SerializeField]
	bool m_shouldReverse = false;

	public bool m_isForwards = true;

	[SerializeField]
	bool shouldDieAfterFirstRun = false;

	void Start () 
	{
		_size = new Vector2 (1.0f / _uvTieX , 1.0f / _uvTieY);
		_myRenderer = renderer;
		if(_myRenderer == null)
			enabled = false;

		m_currentIndex = _frameOffset;
	}


	//My way vars
	float m_floatCatch = 0.0f;
	int m_currentIndex = 0;

	// Update is called once per frame
	void Update()
	{
		if(m_isForwards)
		{
			// Calculate index
			//int index = (int)(Time.timeSinceLevelLoad * _fps) % (_uvTieX * _uvTieY);
			m_floatCatch += Time.deltaTime;
			//if(index != _lastIndex)
			if(m_floatCatch >= 1.0f / _fps)
			{
				m_floatCatch = 0.0f;
				m_currentIndex++;
				// split into horizontal and vertical index
				int uIndex = m_currentIndex % _uvTieX;
				int vIndex = m_currentIndex / _uvTieY;
				
				// build offset
				// v coordinate is the bottom of the image in opengl so we need to invert.
				Vector2 offset = new Vector2 (uIndex * _size.x, 1.0f - _size.y - vIndex * _size.y);
				
				_myRenderer.material.SetTextureOffset ("_MainTex", offset);
				_myRenderer.material.SetTextureScale ("_MainTex", _size);
				
				_lastIndex = m_currentIndex;

				if(m_currentIndex > ((_uvTieX * _uvTieY) - 2))
				{
					if(m_shouldReverse)
					{
						m_isForwards = false;
						//m_currentIndex = m_currentIndex;
					}
					else if(shouldDieAfterFirstRun)
					{
						Destroy(this.gameObject);
					}
				}
			}
		}
		else
		{
			m_floatCatch += Time.deltaTime;
			if(m_floatCatch >= 1.0f / _fps)
			{
				m_floatCatch = 0.0f;

				//Time to increment frame
				m_currentIndex--;

				// split into horizontal and vertical index
				int uIndex = m_currentIndex % _uvTieX;
				int vIndex = m_currentIndex / _uvTieY;
				
				// build offset
				// v coordinate is the bottom of the image in opengl so we need to invert.
				Vector2 offset = new Vector2 (uIndex * _size.x, 1.0f - _size.y - vIndex * _size.y);
				
				_myRenderer.material.SetTextureOffset ("_MainTex", offset);
				_myRenderer.material.SetTextureScale ("_MainTex", _size);

				if(m_currentIndex == 0)
				{
					//Reached the start again!
					m_isForwards = true;
				}
			}
		}
	}

    public void SetShouldDieAfterFirstRun(bool flag_)
    {
        shouldDieAfterFirstRun = flag_;
    }

    public void SetCurrentFrame(int index_)
    {
        if(_myRenderer == null)
        {
            Start();
        }

        m_currentIndex = index_;

        int uIndex = m_currentIndex % _uvTieX;
        int vIndex = m_currentIndex / _uvTieY;

        // build offset
        // v coordinate is the bottom of the image in opengl so we need to invert.
        Vector2 offset = new Vector2(uIndex * _size.x, 1.0f - _size.y - vIndex * _size.y);

        _myRenderer.material.SetTextureOffset("_MainTex", offset);
        _myRenderer.material.SetTextureScale("_MainTex", _size);
    }
}
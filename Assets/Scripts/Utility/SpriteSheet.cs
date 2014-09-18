using UnityEngine;



/// <summary>
/// SpriteSheet is the central script used for sprite sheets used in the game. Given correct co-ordinates it will go through each sprite in
/// sequence at the desired speed.
/// </summary>
[RequireComponent (typeof (Renderer))]
public sealed class SpriteSheet : MonoBehaviour
{
    #region Unity modifiable variables

    [SerializeField, Range (1, 100)]    int m_tilesX = 1;                       //!< How many tiles in the X axis.
    [SerializeField, Range (1, 100)]    int m_tilesY = 1;                       //!< How many tiles in the Y axis.
    [SerializeField, Range (1, 120)]    int m_fps = 10;                         //!< How often the sprite should be updated.
    [SerializeField, Range (0, 10000)]  int m_startingIndex = 0;                //!< The frame which the sprite should start on.
    
    [SerializeField]                    bool m_isForwards = true;               //!< Is the sprite sheet supposed to work its way forwards?
    [SerializeField]                    bool m_shouldReverse = false;           //!< Whether the sprite should go back to its starting point in reverse.
    [SerializeField]                    bool m_shouldDieAfterFirstRun = false;  //!< Will destroy the object upon completion of the first sequence.

    #endregion Unity modifiable variables


    #region Internal data
    
    int m_currentIndex = 0;         //!< The current index of the sprite sheet.
    int m_totalTiles = 0;           //!< The total number of tiles in the sprite sheet.

    float m_currentTime = 0f;       //!< The current time.
    float m_frameTime = 0f;         //!< How long between each frame change.

    Vector2 m_size = Vector2.zero;  //!< Stores the dimensions of each frame on the sprite sheet.

    Renderer m_myRenderer = null;   //!< A reference to the Renderer used on the GameObject.


    #endregion Internal data


    #region Getters & setters

    /// <summary>
    /// If the SpriteSheet is set to die after the first run then it will destroy the entire GameObject after a full sequence.
    /// </summary>
    /// <param name="shouldDie">Should the object destroy itself?</param>
    public void SetShouldDieAfterFirstRun (bool shouldDie)
    {
        m_shouldDieAfterFirstRun = shouldDie;
    }


    /// <summary>
    /// Sets the current frame
    /// </summary>
    /// <param name="index_">Index_.</param>
    public void SetCurrentFrame (int index)
    {
        // Set the object up if necessary
        if (m_myRenderer != null && enabled)
        {
            Setup();
        }

        // Check the index given and whether there is actually any point in performing the action
        if (enabled && index >= 0 && index < m_totalTiles)
        {
            m_currentIndex = index;
            m_currentTime = 0f;
            UpdateMaterialToCurrentIndex();
        }
    }

    #endregion Getters & setters


    #region Behaviour functions

    /// <summary>
    /// Simply ensures that the object is set up correctly when it loads.
    /// </summary>
    void Awake()
    {
        Setup();
    }


    /// <summary>
    /// Update checks the time to see if it is ready to move on and if so, how it should move on.
    /// </summary>
    void Update()
    {
        // Update the time
        m_currentTime += Time.deltaTime;

        if (m_currentTime >= m_frameTime)
        {
            m_currentTime -= m_frameTime;

            // Update the index
            IncrementCurrentIndex();

            // Update the material
            UpdateMaterialToCurrentIndex();

            // Make sure the sprite is moving in the correct direction along the sequence
            if (m_currentIndex == m_totalTiles - 1)
            {
                if (m_shouldReverse)
                {
                    m_isForwards = false;
                }

                if (m_shouldDieAfterFirstRun)
                {
                    Debug.Log ("Spritesheet destroyed: " + gameObject.name);
                    Destroy (gameObject);
                }
            }

            else if (m_currentIndex == 0)
            {
                m_isForwards = true;

                if (m_shouldDieAfterFirstRun)
                {
                    Debug.Log ("Spritesheet destroyed: " + gameObject.name);
                    Destroy (gameObject);
                }
            }
        }
    }

    #endregion


    #region Material functionality

    /// <summary>
    /// Calculates all of the values required by different parts of the class. It will disable the script if the renderer doesn't exist or there is only one tile.
    /// This is to save on performance.
    /// </summary>
    void Setup()
    {
        // Calculate the size
        m_totalTiles = m_tilesX * m_tilesY;
        m_size = new Vector2 (1.0f / m_tilesX, 1.0f / m_tilesY);
        
        // Calculate other miscellaneous information
        m_frameTime = 1f / m_fps;
        m_currentTime = 0f;
        m_currentIndex = m_startingIndex < m_totalTiles ? m_startingIndex : 0;

        // Obtain a reference to the Renderer
        m_myRenderer = renderer;

        // Disable the script if the renderer doesn't exist
        if (m_myRenderer == null)
        {
            enabled = false;
        }

        // If it does there's no point wasting CPU time if there's only one tile
        else if (m_totalTiles == 1)
        {
            enabled = false;
            m_currentIndex = 0;
            UpdateMaterialToCurrentIndex();
        }
    }


    /// <summary>
    /// Will either increment or decrement the index depending on whether it is as a forward sequence or not.
    /// </summary>
    void IncrementCurrentIndex()
    {
        if (m_isForwards)
        {
            ++m_currentIndex;
        }

        else
        {
            m_currentIndex = Mathf.Max (0, --m_currentIndex);
        }
    }


    /// <summary>
    /// Using the current index the function will update the material to show the correct image.
    /// </summary>
    void UpdateMaterialToCurrentIndex()
    {
        // Determine the vertical and horizontal indexes
        int xIndex = m_currentIndex % m_tilesX;
        int yIndex = m_currentIndex / m_tilesY;
        
        // Create the offset, y is the bottom of the image.
        Vector2 offset = new Vector2 (xIndex * m_size.x, 1.0f - m_size.y - yIndex * m_size.y);

        // Correct the material
        m_myRenderer.material.SetTextureOffset ("_MainTex", offset);
        m_myRenderer.material.SetTextureScale ("_MainTex", m_size);
    }

    #endregion Material functionality
}
using UnityEngine;
using System.Collections;

public class GUIOptionsScreen : BaseGUIScreen 
{
    /* Serializable Members */
    // Textures
    [SerializeField]        Texture m_menuBackground;

    /* Internal Members */
    Resolution m_newResolution;
    bool m_shouldFullscreen = false;
    bool m_resoDropdown = false;
    bool m_useController = false;
    
    // Cached members
    GameStateController m_gscCache;

    /* Unity functions */	
	void Start () 
    {
	    m_priorityValue = 2;
        
        int control = PlayerPrefs.GetInt("UseControl");
        if (control == 1)
            m_useController = true;
            
        m_newResolution = Screen.currentResolution;
            
        m_gscCache = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameStateController>();
	}
    
    /* Custom Functions */
    public override void ManualGUICall (bool shouldRecieveInput)
    {
        GUI.DrawTexture(new Rect(512, 130, 290, 620), m_menuBackground);
        
        GUI.Label(new Rect(515, 131, 288, 50), "OPTIONS", "Big No Box");
        /* Graphics options */
        
        //Reso + FS options
        GUI.Label(new Rect(515, 220, 288, 50), "RESOLUTION", "Big No Box");
        if (GUI.Button(new Rect(513, 270, 288, 50), m_newResolution.width + "x" + m_newResolution.height, "Shared") && shouldRecieveInput)
        {
            m_resoDropdown = !m_resoDropdown;
        }
        
        if (m_resoDropdown)
        {
            Resolution[] possibleResos = Screen.resolutions;
            
            //Draw each reso as a button, on click, set it and unset resoDropdown
            for (int i = 0; i < possibleResos.Length; i++)
            {
                if (GUI.Button(new Rect(815, 270 + (i * 50), 288, 50), possibleResos[i].width + "x" + possibleResos[i].height, "Shared") && shouldRecieveInput)
                {
                    m_newResolution = possibleResos[i];
                    m_resoDropdown = false;
                }
            }
        }
        
        if (m_shouldFullscreen)
        {
            if (GUI.Button(new Rect(540, 330, 238, 50), "Fullscreen On", "Highlighted") && shouldRecieveInput)
            {
                m_shouldFullscreen = !m_shouldFullscreen;
            }
        }
        else
        {
            if (GUI.Button(new Rect(540, 330, 238, 50), "Fullscreen Off", "Shared") && shouldRecieveInput)
            {
                m_shouldFullscreen = !m_shouldFullscreen;
            }
        }
        
        if (GUI.Button(new Rect(540, 390, 238, 50), "Apply Resolution Changes", "Shared") && shouldRecieveInput)
        {
            Screen.SetResolution(m_newResolution.width, m_newResolution.height, m_shouldFullscreen);
        }
        
        if (m_useController)
        {
            if (GUI.Button(new Rect(540, 450, 238, 50), "Use Controller", "Highlighted") && shouldRecieveInput)
            {
                m_useController = false;
                PlayerPrefs.SetInt("UseControl", 0);
            }
        }
        else
        {
            if (GUI.Button(new Rect(540, 450, 238, 50), "Use Controller", "Shared") && shouldRecieveInput)
            {
                if (Input.GetJoystickNames().Length > 0)
                {
                    m_useController = true;
                    PlayerPrefs.SetInt("UseControl", 1);
                }
            }
        }
        
        //Quality settings
        
        /* Audio options */
        GUI.Label(new Rect(515, 540, 288, 50), "SOUND", "Big No Box");
        
        GUI.Label(new Rect(515, 590, 288, 50), "Music", "No Box");
        PlayerPrefs.SetFloat("MusicVolume", GUI.HorizontalSlider(new Rect(520, 640, 278, 20), PlayerPrefs.GetFloat("MusicVolume"), 0.0f, 1.0f));
        
        GUI.Label(new Rect(515, 660, 288, 50), "Effects", "No Box");
        PlayerPrefs.SetFloat("EffectVolume", GUI.HorizontalSlider(new Rect(520, 710, 278, 20), PlayerPrefs.GetFloat("EffectVolume"), 0.0f, 1.0f));
        
        if(GUI.Button (new Rect(222, 130, 290, 620), "", "label"))
        {
            m_gscCache.CloseOptionMenu();
        }
    }
}

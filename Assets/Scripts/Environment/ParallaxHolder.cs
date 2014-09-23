using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ParallaxHolder : MonoBehaviour
{
    [SerializeField]        Texture         m_standardTexture;
    [SerializeField]        Texture         m_transitionTexture;

    List<ParallaxLayer> layers = new List<ParallaxLayer>();

    void Start()
    {
        SetLayers();
    }

    public void ResetLayers()
    {
        foreach (ParallaxLayer layer in layers)
        {
            layer.transform.localPosition = Vector3.zero;
        }
    }

    void SetLayers()
    {
        layers.Clear();
        for (int i = 0; i < transform.childCount; i++)
        {
            ParallaxLayer layer = transform.GetChild(i).GetComponent<ParallaxLayer>();
            if (layer != null)
            {
                layers.Add(layer);
            }
        }
    }

    public void Move(Vector2 distance, Vector3 cameraPos)
    {
        this.transform.position = cameraPos + new Vector3(0, 0, 220);
        foreach (ParallaxLayer layer in layers)
        {
            layer.Move(distance);
        }
    }
    
    #region Transition Functions
    public void SwitchToTransitionState()
    {
        for(int i = 0; i < layers.Count; i++)
        {
            layers[i].gameObject.SetActive(false);
        }
        
        this.renderer.material.mainTexture = m_transitionTexture;
        GetComponent<SpriteSheet>().enabled = true;
    }
    
    public void SwitchToNormalState()
    {
        for(int i = 0; i < layers.Count; i++)
        {
            layers[i].gameObject.SetActive(true);
        }
        
        this.renderer.material.mainTexture = m_standardTexture;
        this.renderer.material.SetTextureOffset("_MainTex", Vector2.zero);
        GetComponent<SpriteSheet>().enabled = false;
    }
    #endregion
}

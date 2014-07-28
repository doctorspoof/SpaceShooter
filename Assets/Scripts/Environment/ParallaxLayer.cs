using UnityEngine;
using System.Collections;

public class ParallaxLayer : MonoBehaviour
{
    [SerializeField] Vector2 parallaxFactor = Vector2.one;

    public void Move(Vector2 distance)
    {
        Vector2 magnitude = new Vector2(distance.x * parallaxFactor.x, distance.y * parallaxFactor.y);
        Vector2 currOffset = renderer.material.GetTextureOffset("_MainTex");
        renderer.material.SetTextureOffset("_MainTex", currOffset - magnitude);
    }
}

using UnityEngine;
using System.Collections;

public class ParallaxLayer : MonoBehaviour 
{
	[SerializeField]
	Vector2 parallaxFactor = Vector2.one;

	// Use this for initialization
	void Start () 
	{
	
	}
	
	// Update is called once per frame
	void Update () 
	{
	
	}

	public void Move(Vector2 distance)
	{
		Vector2 magnitude = new Vector2(distance.x * parallaxFactor.x, distance.y * parallaxFactor.y);
		Vector2 currOffset = renderer.material.GetTextureOffset("_MainTex");
		renderer.material.SetTextureOffset("_MainTex", currOffset - magnitude);
		/*Vector3 newPos = transform.localPosition;
		newPos.x -= distance.x * parallaxFactor.x;
		newPos.y -= distance.y * parallaxFactor.y;

		transform.localPosition = newPos;*/
	}
}

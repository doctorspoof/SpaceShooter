using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ParallaxHolder : MonoBehaviour 
{
	List<ParallaxLayer> layers = new List<ParallaxLayer>();

	// Use this for initialization
	void Start () 
	{
		SetLayers();
	}

	public void ResetLayers()
	{
		foreach(ParallaxLayer layer in layers)
		{
			layer.transform.localPosition = Vector3.zero;
		}
	}
	void SetLayers()
	{
		layers.Clear();
		for(int i = 0; i < transform.childCount; i++)
		{
			ParallaxLayer layer = transform.GetChild (i).GetComponent<ParallaxLayer>();
			if(layer != null)
			{
				layers.Add(layer);
			}
		}
	}

	public void Move(Vector2 distance, Vector3 cameraPos)
	{
		this.transform.position = cameraPos + new Vector3(0, 0, 220);
		foreach(ParallaxLayer layer in layers)
		{
			layer.Move(distance);
		}
	}
}

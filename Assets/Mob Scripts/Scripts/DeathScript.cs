using UnityEngine;
using System.Collections;

public class DeathScript : MonoBehaviour 
{

	// Use this for initialization
	void Start () 
	{
		this.audio.volume = PlayerPrefs.GetFloat("EffectVolume", 1.0f);
		this.audio.Play ();
	}
	
	// Update is called once per frame
	void Update () 
	{
		if(!this.audio.isPlaying)
		{
			Destroy (this.gameObject);
		}
	}
}

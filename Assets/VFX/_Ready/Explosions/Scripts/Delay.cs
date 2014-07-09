
using UnityEngine;
using System.Collections;

public class Delay : MonoBehaviour {

    float timeToDelay = 0, currentTime = 0;

    MeshRenderer renderer;
    SpriteSheet spriteSheet;

    void Start()
    {
        renderer = GetComponent<MeshRenderer>();
        renderer.enabled = false;

        spriteSheet = GetComponent<SpriteSheet>();
        spriteSheet.enabled = false;
        //gameObject.SetActive(false);
    }

	// Update is called once per frame
	void Update () {

        currentTime += Time.deltaTime;
        if(currentTime >= timeToDelay)
        {
            renderer.enabled = true;
            spriteSheet.enabled = true;
            spriteSheet._frameOffset = 0;
        }

	}

    public void SetDelay(float delay_)
    {
        timeToDelay = delay_;
    }
}

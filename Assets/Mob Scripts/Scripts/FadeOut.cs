
using UnityEngine;
using System.Collections;

public class FadeOut : MonoBehaviour
{

    float timeBeforeFadeOutStarts = 0, fadeOutTime = 0;
    float currentTime = 0;

    Material mat;

    // Use this for initialization
    void Start()
    {
        mat = GetComponent<MeshRenderer>().material;
    }

    // Update is called once per frame
    void Update()
    {

        currentTime += Time.deltaTime;
        if (currentTime >= timeBeforeFadeOutStarts)
        {
            if (mat != null)
            {
                float t = ((timeBeforeFadeOutStarts + fadeOutTime - currentTime) / fadeOutTime);
                mat.color = new Color(mat.color.r, mat.color.g, mat.color.b, t);
            }

        }

        if(currentTime >= timeBeforeFadeOutStarts + fadeOutTime)
        {
            Destroy(gameObject);
        }


    }

    public void SetTimes(float fadeOutTime_, float timeBeforeFadeOutStarts_)
    {
        fadeOutTime = fadeOutTime_;
        timeBeforeFadeOutStarts = timeBeforeFadeOutStarts_;
    }
}

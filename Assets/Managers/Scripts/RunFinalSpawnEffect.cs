using UnityEngine;
using System.Collections;

public class RunFinalSpawnEffect : MonoBehaviour
{
    [SerializeField]
    GameObject finalEffectPrefab;

    GameObject finalEffect;
    // Use this for initialization
    void Start()
    {
        finalEffect = (GameObject)Instantiate(finalEffectPrefab, transform.position, Quaternion.identity);
        finalEffect.transform.localScale = transform.localScale;
    }

    public void Run(float time_)
    {
        Invoke("Fire", time_);
    }

    void Fire()
    {
        if (finalEffect)
        {
            finalEffect.GetComponent<MeshRenderer>().enabled = true;
            finalEffect.GetComponent<SpriteSheet>().enabled = true;
            finalEffect.GetComponent<SpriteSheet>().SetCurrentFrame(0);
        }
    }

}

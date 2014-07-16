using UnityEngine;
using System.Collections;

[System.Serializable]
public class Explosion
{
    [SerializeField]
    public GameObject[] explosionPrefabs;

    [SerializeField]
    public Vector3 localPosition;

    [SerializeField]
    public Vector3 localScale;

    [SerializeField]
    public float timeAfterDeathThatExplosionStarts;
}

[System.Serializable]
public class Fragment
{
    public Material material;

    public Vector3 directionToApplyForce;

}

public class Explode : MonoBehaviour
{

    [SerializeField]
    bool explodeOnDeath;
    [SerializeField]
    float removeShipAfterSeconds = -1;

    [SerializeField]
    Explosion[] explosions;

    bool exploding = false;

    [SerializeField]
    Fragment[] fragments;

    void OnDestroy()
    {
        if (explodeOnDeath)
        {
            Fire();
        }
    }

    public void Fire()
    {
        if (!exploding)
        {
            exploding = true;
            if (!explodeOnDeath)
            {
                DisableAllNonEssentialComponents();
            }

            StartExplosionSequence();

        	Invoke("DestroyEntity", removeShipAfterSeconds >= 0 ? removeShipAfterSeconds : GetTimeUntilLastExplosionStarts());
        }
    }

    private void DisableAllNonEssentialComponents()
    {
        Ship shipComponent = GetComponent<Ship>();
        if(shipComponent)
        {
            shipComponent.enabled = false;
        }

        for(int i = 0; i < transform.childCount; ++i)
        {
            transform.GetChild(i).gameObject.SetActive(false);
        }
        //MonoBehaviour[] monoBehaviours = GetComponents<MonoBehaviour>();
        //foreach(MonoBehaviour monoBehaviour in monoBehaviours)
        //{
        //    System.Type type = monoBehaviour.GetType();
        //    if(typeof(MeshFilter) != type && typeof(MeshRenderer) != type)
        //    {
        //        monoBehaviour.enabled = false;
        //    }
        //}
    }

    private void StartExplosionSequence()
    {
        transform.Translate(new Vector3(0, 0, 2));

        foreach (Explosion explosion in explosions)
        {

            GameObject obj = (GameObject)Instantiate(explosion.explosionPrefabs[Random.Range(0, explosion.explosionPrefabs.Length)], this.transform.position + explosion.localPosition, Quaternion.identity);
            obj.transform.localScale = explosion.localScale;

            Delay delay = obj.GetComponent<Delay>();
            delay.SetDelay(explosion.timeAfterDeathThatExplosionStarts);

            SpriteSheet sheet = obj.GetComponent<SpriteSheet>();
            sheet.SetShouldDieAfterFirstRun(true);
        }
    }

    private void StartFragmentSequence()
    {
        if(fragments != null)
        {
            GameObject fragmentOriginal = GameObject.CreatePrimitive(PrimitiveType.Quad);
            MeshCollider col = fragmentOriginal.GetComponent<MeshCollider>();
            DestroyImmediate(col);

            fragmentOriginal.transform.localScale = transform.localScale;

            Rigidbody body = fragmentOriginal.AddComponent<Rigidbody>();
            body.useGravity = false;

            // clean up fragments
            

            foreach(Fragment frag in fragments)
            {

                GameObject fragment = (GameObject)Instantiate(fragmentOriginal, transform.position, transform.rotation);
                
                MeshRenderer renderer = fragment.GetComponent<MeshRenderer>();
                renderer.material = frag.material;

                FadeOut fade = fragment.AddComponent<FadeOut>();
                fade.SetTimes(4, 5);

                Rigidbody rigidbody = fragment.GetComponent<Rigidbody>();

                Vector3 force = frag.directionToApplyForce * Random.Range(0.5f, 1.0f) * 50;
                rigidbody.AddForce(force);

                rigidbody.AddTorque(new Vector3(0, 0, Random.Range(-10f, 10f)));

            }

            Destroy(fragmentOriginal);

        }

    }

    private void DestroyEntity()
    {
        StartFragmentSequence();
		if(Network.isServer)
        	Network.Destroy(gameObject);
    }

    public float GetTimeUntilLastExplosionStarts()
    {
        float returnee = 0;
        foreach (Explosion explosion in explosions)
        {
            if (explosion.timeAfterDeathThatExplosionStarts > returnee)
            {
                returnee = explosion.timeAfterDeathThatExplosionStarts;
            }
        }

        return returnee;
    }

}

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

    public GameObject fragmentObject;

}

public class Explode : MonoBehaviour
{
    [SerializeField]
    GameObject primitiveQuad;

    [SerializeField]
    float removeShipAfterSeconds = 0;

    [SerializeField]
    Explosion[] explosions;

    bool exploding = false;

    [SerializeField]
    Fragment[] fragments;

    public void Fire()
    {
        if (!exploding)
        {
            exploding = true;
            if (removeShipAfterSeconds > 0)
            {
                DisableAllNonEssentialComponents();
            }

            StartExplosionSequence();
            CreateFragments();

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

        MeshRenderer render = GetComponent<MeshRenderer>();
        if(render)
        {
            render.enabled = false;
        }

        Rigidbody rigidbody = GetComponent<Rigidbody>();
        if(rigidbody)
        {
            rigidbody.Sleep();
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
        foreach (Explosion explosion in explosions)
        {
            Vector3 newPosition = transform.position;
            newPosition.z = 10.1f;

            GameObject obj = (GameObject)Instantiate(explosion.explosionPrefabs[Random.Range(0, explosion.explosionPrefabs.Length)], newPosition + explosion.localPosition, Quaternion.identity);
            obj.transform.localScale = explosion.localScale;

            Delay delay = obj.GetComponent<Delay>();
            delay.SetDelay(explosion.timeAfterDeathThatExplosionStarts);

            SpriteSheet sheet = obj.GetComponent<SpriteSheet>();
            sheet.SetShouldDieAfterFirstRun(true);
        }
    }

    void CreateFragments()
    {
        if (fragments.Length > 0)
        {
            GameObject fragmentOriginal = (GameObject)Instantiate(primitiveQuad);
            fragmentOriginal.transform.localScale = transform.localScale;

            foreach (Fragment frag in fragments)
            {
                Vector3 newPosition = transform.position;
                newPosition.z = 10.1f;

                frag.fragmentObject = (GameObject)Instantiate(fragmentOriginal, newPosition, transform.rotation);

                MeshRenderer renderer = frag.fragmentObject.GetComponent<MeshRenderer>();
                renderer.material = frag.material;

                FadeOut fade = frag.fragmentObject.AddComponent<FadeOut>();
                fade.SetTimes(4, 5);
            }

            Destroy(fragmentOriginal);

        }
    }

    void MoveFragements()
    {
        foreach (Fragment frag in fragments)
        {
            Rigidbody rigidbody = frag.fragmentObject.GetComponent<Rigidbody>();

            Vector3 force = transform.rotation * frag.directionToApplyForce * Random.Range(0.5f, 1.0f) * 50;
            rigidbody.AddForce(force);

            rigidbody.AddTorque(new Vector3(0, 0, Random.Range(-10f, 10f)));

            FadeOut fade = frag.fragmentObject.GetComponent<FadeOut>();
            fade.StartFadeOut();

        }
    }

    private void DestroyEntity()
    {
        MoveFragements();
        Destroy(gameObject);
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

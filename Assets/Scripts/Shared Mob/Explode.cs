using UnityEngine;
using System.Collections;

[System.Serializable]
public class Explosion
{
    public GameObject[] explosionPrefabs;

    public Vector3 localPosition;

    public Vector3 localScale;

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
    [SerializeField] GameObject m_primitiveQuad;

    [SerializeField] float m_removeShipAfterSeconds = 0;

    [SerializeField] Explosion[] m_explosions;

    [SerializeField] Fragment[] m_fragments;

    bool m_exploding = false;

    public void Fire()
    {
        if (!m_exploding)
        {
            m_exploding = true;
            if (m_removeShipAfterSeconds > 0)
            {
                DisableAllNonEssentialComponents();
            }

            StartExplosionSequence();
            CreateFragments();

            Invoke("DestroyEntity", m_removeShipAfterSeconds >= 0 ? m_removeShipAfterSeconds : GetTimeUntilLastExplosionStarts());
        }
    }

    private void DisableAllNonEssentialComponents()
    {
        Ship shipComponent = GetComponent<Ship>();
        if (shipComponent != null)
        {
            shipComponent.enabled = false;
        }

        MeshRenderer render = GetComponent<MeshRenderer>();
        if (render != null)
        {
            render.enabled = false;
        }

        Rigidbody rigidbody = GetComponent<Rigidbody>();
        if (rigidbody != null)
        {
            rigidbody.Sleep();
        }

        for (int i = 0; i < transform.childCount; ++i)
        {
            transform.GetChild(i).gameObject.SetActive(false);
        }
    }

    private void StartExplosionSequence()
    {
        foreach (Explosion explosion in m_explosions)
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
        if (m_fragments.Length > 0)
        {
            GameObject fragmentOriginal = (GameObject)Instantiate(m_primitiveQuad);
            fragmentOriginal.transform.localScale = transform.localScale;

            foreach (Fragment frag in m_fragments)
            {
                Vector3 newPosition = transform.position;
                newPosition.z = 10.1f;

                frag.fragmentObject = (GameObject)Instantiate(fragmentOriginal, newPosition, transform.rotation);

                MeshRenderer renderer = frag.fragmentObject.GetComponent<MeshRenderer>();
                renderer.material = frag.material;

                FadeOut fade = frag.fragmentObject.AddComponent<FadeOut>();
                fade.SetTimes(4, 5);
            }

            Debug.Log ("Explode destroyed: " + fragmentOriginal.name);
            Destroy(fragmentOriginal);

        }
    }

    void MoveFragments()
    {
        foreach (Fragment frag in m_fragments)
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
        MoveFragments();
        Destroy(gameObject);
    }

    public float GetTimeUntilLastExplosionStarts()
    {
        float returnee = 0;
        foreach (Explosion explosion in m_explosions)
        {
            if (explosion.timeAfterDeathThatExplosionStarts > returnee)
            {
                returnee = explosion.timeAfterDeathThatExplosionStarts;
            }
        }

        return returnee;
    }

}

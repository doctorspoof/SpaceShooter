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

public class Explode : MonoBehaviour
{

    [SerializeField]
    bool explodeOnDeath;
    [SerializeField]
    float removeShipAfterSeconds = -1;

    [SerializeField]
    Explosion[] explosions;

    bool exploding = false;

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

            if (Network.isServer)
            {
                Invoke("DestroyEntity", removeShipAfterSeconds >= 0 ? removeShipAfterSeconds : GetTimeUntilLastExplosionStarts());
            }
        }
    }

    private void DisableAllNonEssentialComponents()
    {
        Ship shipComponent = GetComponent<Ship>();
        if(shipComponent)
        {
            shipComponent.enabled = false;
        }

        Transform shield = this.tag.Equals("Player") == true ? transform.FindChild("Shield") : transform.FindChild("Composite Collider").FindChild("Shield");
        if(shield)
        {
            shield.gameObject.SetActive(false);
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

            GameObject obj = (GameObject)Instantiate(explosion.explosionPrefabs[Random.Range(0, explosion.explosionPrefabs.Length)], this.transform.position + explosion.localPosition, Quaternion.identity);
            obj.transform.localScale = explosion.localScale;

            Delay delay = obj.GetComponent<Delay>();
            delay.SetDelay(explosion.timeAfterDeathThatExplosionStarts);

            SpriteSheet sheet = obj.GetComponent<SpriteSheet>();
            sheet.SetShouldDieAfterFirstRun(true);
        }
    }

    private void DestroyEntity()
    {
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

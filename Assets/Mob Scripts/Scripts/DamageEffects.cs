using UnityEngine;
using System.Collections;

// this class is used for caching. allows us to only test against the next set of damage effects that need applying.
public class DamageContainer
{
    public GameObject container;

    public GameObject[] effects;

    public float healthPercentageToShowAt;

    public DamageContainer(GameObject container_, GameObject[] effects_, float healthPercentageToShowAt_)
    {
        container = container_;
        effects = effects_;
        healthPercentageToShowAt = healthPercentageToShowAt_;

        container.SetActive(false);
    }

    public void Activate()
    {
        container.SetActive(true);
    }
}

public class DamageEffects : MonoBehaviour {

    GameObject effectsHolder;
    float currentHealthPercentage = 1;

    [SerializeField]


    DamageContainer[] containers;

	// Use this for initialization
	void Start () {
        effectsHolder = new GameObject("ShipDamageEffects");
        effectsHolder.transform.parent = transform;
	}
	
	// Update is called once per frame
	void Update () {
	
	}

    public void SetCurrentHealthPercentage(float percentage_)
    {
        currentHealthPercentage = percentage_;

    }

    public void CheckEffects()
    {

    }
}

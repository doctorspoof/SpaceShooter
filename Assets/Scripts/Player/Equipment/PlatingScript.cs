using UnityEngine;
using System.Collections;

public class PlatingScript : MonoBehaviour
{

    [SerializeField] Vector3 m_requiredOffset;

    [SerializeField] int m_platingHealth;

    [SerializeField] float m_platingWeight;


    #region getset

    public Vector3 GetOffset()
    {
        return m_requiredOffset;
    }

    public int GetPlatingHealth()
    {
        return m_platingHealth;
    }

    public float GetPlatingWeight()
    {
        return m_platingWeight;
    }

    #endregion getset

    //TODO: Add special immunities or reductions here

    public void ParentToPlayer(string name)
    {
        networkView.RPC("ParentToPlayerOverNetwork", RPCMode.Others, name);
    }

    [RPC] void ParentToPlayerOverNetwork(string name)
    {
        GameStateController gsc = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameStateController>();

        GameObject playerGO = gsc.GetPlayerFromNetworkPlayer(gsc.GetNetworkPlayerFromID(gsc.GetIDFromName(name)));
        Debug.Log("Attaching plating: " + this.name + " to gameObject: " + playerGO.name + ", through name: " + name);
        this.transform.parent = playerGO.transform;
        this.transform.localPosition = m_requiredOffset;
        playerGO.GetComponent<HealthScript>().EquipNewPlating(m_platingHealth);
    }
}

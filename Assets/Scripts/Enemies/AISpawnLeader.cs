using UnityEngine;
using System.Collections.Generic;

public class AISpawnLeader : MonoBehaviour, IEntity
{
    AINode m_node;

    List<string> targetTags;
    GameObject target = null;

    #region getset

    public AINode GetAINode()
    {
        return m_node;
    }

    public void SetAINode(AINode node_)
    {
        m_node = node_;
    }

    //public List<string> GetTargetTags()
    //{
    //    return targetTags;
    //}

    public void AddTargetTag(string tag_)
    {
        targetTags.Add(tag_);
        Debug.Log("Adding tag_ = " + tag_ + " count now = " + targetTags.Count);
    }


    #endregion

    void Awake()
    {
        targetTags = new List<string>();
        m_node = new AINode(this);
    }

    public virtual bool ReceiveOrder(int orderID_, object[] listOfParameters)
    {
        switch ((AIShipOrder)orderID_)
        {
            default:
                {
                    return false;
                }
        }
    }

    public virtual bool GiveOrder(int orderID_, object[] listOfParameters)
    {
        GetAINode().OrderChildren(orderID_, listOfParameters);
        return true;
    }

    public virtual bool ConsiderOrder(int orderID_, object[] listOfParameters)
    {
        switch ((AIShipOrder)orderID_)
        {
            default:
                {
                    return false;
                }
        }
    }

    public virtual bool RequestOrder(IEntity entity_)
    {
        System.Type entityType = entity_.GetType();

        if (entityType.Equals(typeof(ShipEnemy)))
        {
            // if target is equal to null find a new target
            if (target == null)
            {
                // find a new target, but if one does not exist return false
                if ((target = GetClosestTarget()) == null)
                {
                    Debug.Log("Running return");
                    return false;
                }
            }

            Debug.Log("Running2");

            entity_.GiveOrder((int)AIShipOrder.Attack, new object[] { target });

            return true;
        }

        return false;
    }

    public virtual object[] RequestInformation(int informationID_)
    {
        switch ((AIShipRequestInfo)informationID_)
        {
            default:
                {
                    return null;
                }
        }
    }

    public virtual bool Notify(int informationID_, object[] listOfParameters)
    {
        switch ((AIShipNotifyInfo)informationID_)
        {
            default:
                {
                    return false;
                }
        }
    }

    GameObject GetClosestTarget()
    {
        Debug.Log("targetTag count = " + targetTags.Count);
        foreach (string tag in targetTags)
        {
            Debug.Log("Iterating targetTags");
            GameObject[] defaultTargets = GameObject.FindGameObjectsWithTag(tag);

            if (defaultTargets != null)
            {
                Debug.Log("defualtTargets not null");
                float closest = 0;
                GameObject closestTarget = null;
                foreach (GameObject potentialTarget in defaultTargets)
                {
                    if (closestTarget == null || Vector2.SqrMagnitude(transform.position - closestTarget.transform.position) < closest)
                    {
                        closestTarget = potentialTarget;
                        closest = Vector2.SqrMagnitude(transform.position - closestTarget.transform.position);
                    }
                }

                return closestTarget;
            }
        }

        //if no targets exist
        return null;

    }

}

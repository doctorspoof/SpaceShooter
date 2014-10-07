using UnityEngine;
using System.Collections.Generic;

public class AISpawnLeader : MonoBehaviour, IEntity, ICloneable
{
    AINode m_node;

    [SerializeField]
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
    }


    #endregion

    void Awake()
    {
        targetTags = new List<string>();
        m_node = new AINode(this);

        m_node.onChildAdded += x =>
        {
            this.Notify((int)AIShipNotifyInfo.ChildAdded, new object[] { x });
        };

        m_node.onChildRemoved += x =>
        {
            this.Notify((int)AIShipNotifyInfo.ChildRemoved, new object[] { x });
        };
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

    /// <summary>
    /// MAY WANT TO REMOVE THIS METHOD FROM ALL AS IT IS ONLY CONFUSING AND NOT BEING USED AS I THOUGHT IT MIGHT.
    /// </summary>
    /// <param name="orderID_"></param>
    /// <param name="listOfParameters"></param>
    /// <returns></returns>
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
                    return false;
                }
            }

            entity_.ReceiveOrder((int)AIShipOrder.Attack, new object[] { target });

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

    public virtual bool Notify(int informationID_, object[] listOfParameters_)
    {
        switch ((AIShipNotifyInfo)informationID_)
        {
            case(AIShipNotifyInfo.ChildAdded):
            case(AIShipNotifyInfo.ChildRemoved):
                {
                    if (target == null)
                    {
                        // find a new target, but if one does not exist return false
                        if ((target = GetClosestTarget()) == null)
                        {
                            return false;
                        }
                    }

                    ((AINode)listOfParameters_[0]).GetEntity().ReceiveOrder((int)AIShipOrder.Attack, new object[] { target });

                    return true;
                }
            default:
                {
                    return false;
                }
        }
    }

    /// <summary>
    /// Finds the closest target of the tags in target tags.
    /// </summary>
    /// <returns></returns>
    GameObject GetClosestTarget()
    {
        foreach (string tag in targetTags)
        {
            GameObject[] defaultTargets = GameObject.FindGameObjectsWithTag(tag);

            if (defaultTargets != null)
            {
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

    /// <summary>
    /// Clones this spawnleader. used for wave spawning.
    /// </summary>
    /// <returns></returns>
    public virtual GameObject Clone()
    {
        GameObject obj = (GameObject)Instantiate(gameObject);
        obj.SetActive(true);
        
        AISpawnLeader leadComponent = obj.GetComponent<AISpawnLeader>();
        leadComponent.targetTags = new List<string>(targetTags);
        
        return obj;
    }

}

using UnityEngine;
using System.Collections.Generic;

public class AINode
{

    List<AINode> m_children = new List<AINode>();
    AINode m_parent;

    IEntity m_entity;

    #region getset

    public void AddChild(AINode node_)
    {
        m_children.Add(node_);
    }

    public void RemoveChild(AINode node_)
    {
        m_children.Remove(node_);
    }
    
    public AINode GetParent()
    {
        return m_parent;
    }

    public void SetParent(AINode parent_)
    {
        m_parent = parent_;
    }

    #endregion getset

    public AINode(IEntity entity_)
    {
        m_entity = entity_;
    }

    /// <summary>
    /// Used for giving an order to the object it is called upon. Needs to be have distinction to GiveOrder
    /// </summary>
    /// <param name="orderID_">The unique id of the order</param>
    /// <param name="listOfParameters">parameters to be used by the order</param>
    /// <returns></returns>
    public bool ReceiveOrder(int orderID_, object[] listOfParameters)
    {
        return m_entity.ReceiveOrder(orderID_, listOfParameters);
    }

    /// <summary>
    /// Used for only giving orders to the children. Does not give the order to itself
    /// </summary>
    /// <param name="orderID_">The unique id of the order</param>
    /// <param name="listOfParameters">The parameters required for the order to run</param>
    public void OrderChildren(int orderID_, object[] listOfParameters)
    {
        foreach(AINode node in m_children)
        {
            node.ReceiveOrder(orderID_, listOfParameters);
        }
    }

    /// <summary>
    /// Returns information based on the unique id supplied
    /// </summary>
    /// <param name="informationID_">unique id for what kind of information to grab</param>
    /// <returns></returns>
    public object[] RequestInformation(int informationID_)
    {
        return m_entity.RequestInformation(informationID_);
    }

    public object[][] RequestInformationFromChildren(int informationID_)
    {
        object[][] info = new object[m_children.Count][];
        for(int i = 0; i < m_children.Count; ++i)
        {
            info[i] = m_children[i].RequestInformation(informationID_);
        }

        return info;
    }
}

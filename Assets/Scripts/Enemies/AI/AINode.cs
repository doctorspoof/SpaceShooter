using UnityEngine;

using System.Collections.Generic;

public enum AIHierarchyRelation
{
    Self = 0,
    Parent = 1,
    Children = 2
}

public class AINode
{

    List<AINode> m_children = new List<AINode>();
    AINode m_parent;



    IEntity m_entity;

    #region getset

    public void AddChild(AINode node_)
    {
        m_children.Add(node_);
        node_.SetParent(this);
    }

    public void RemoveChild(AINode node_)
    {
        if (m_children.Remove(node_))
        {
            node_.SetParent(null);
        }
    }

    public List<AINode> GetChildren()
    {
        return m_children;
    }

    public AINode GetParent()
    {
        return m_parent;
    }

    /// <summary>
    /// Private because you should only be adding to the hierarchy via AddChild. Prevents confusion and a cyclic circumstance. Keeps the hierarchy correct
    /// </summary>
    /// <param name="parent_"></param>
    void SetParent(AINode parent_)
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
        foreach (AINode node in m_children)
        {
            node.ReceiveOrder(orderID_, listOfParameters);
        }
    }

    /// <summary>
    /// This is an order we have to be able to receive, but do not have to act upon. Uses could include a member of a group gets attacked and wants to alert the leader.
    /// Primarily for going up the chain to the parent/leader since it would be MUTINY if the parent got ordered around by a subordinate. Plus, they are unlikely pirates,
    /// so there are no planks for any mutineers.
    /// </summary>
    /// <param name="who_">Who should receive the order</param>
    /// <param name="orderID_"></param>
    /// <param name="listOfParameters"></param>
    public void RequestConsiderationOfOrder(AIHierarchyRelation who_, int orderID_, object[] listOfParameters)
    {
        switch (who_)
        {
            case (AIHierarchyRelation.Self):
                {
                    m_entity.ConsiderOrder(orderID_, listOfParameters);
                    return;
                }
            case (AIHierarchyRelation.Parent):
                {
                    if (m_parent != null)
                    {
                        m_parent.RequestConsiderationOfOrder(AIHierarchyRelation.Self, orderID_, listOfParameters);
                    }
                    return;
                }
            case (AIHierarchyRelation.Children):
                {
                    foreach (AINode child in m_children)
                    {
                        child.RequestConsiderationOfOrder(AIHierarchyRelation.Self, orderID_, listOfParameters);
                    }
                    return;
                }
            default:
                {
                    return;
                }
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

    /// <summary>
    /// Returns the information of all children of this AINode
    /// </summary>
    /// <param name="informationID_">An array of information! WOW!</param>
    /// <returns></returns>
    public object[][] RequestInformationFromChildren(int informationID_)
    {
        object[][] info = new object[m_children.Count][];
        for (int i = 0; i < m_children.Count; ++i)
        {
            info[i] = m_children[i].RequestInformation(informationID_);
        }

        return info;
    }

    /// <summary>
    /// Notify this node of a change in information.
    /// </summary>
    /// <param name="informationID_"></param>
    /// <param name="listOfParameters_"></param>
    /// <returns></returns>
    public bool Notify(int informationID_, object[] listOfParameters_)
    {
        return m_entity.Notify(informationID_, listOfParameters_);
    }

    public bool SendNotify(AIHierarchyRelation who_, int informationID_, object[] listOfParameters_)
    {
        switch (who_)
        {
            case (AIHierarchyRelation.Self):
                {
                    return m_entity.Notify(informationID_, listOfParameters_);
                }
            case (AIHierarchyRelation.Parent):
                {
                    if (m_parent != null)
                    {
                        return m_parent.Notify(informationID_, listOfParameters_);
                    }
                    return false;
                }
            case (AIHierarchyRelation.Children):
                {
                    bool returnee = false;
                    foreach (AINode child in m_children)
                    {
                        bool flag = child.Notify(informationID_, listOfParameters_);
                        returnee = returnee == false ? flag : true;
                    }
                    return returnee;
                }
            default:
                {
                    return false;
                }
        }
    }

    /// <summary>
    /// Makes sure that the links of the hierarchy are not broken. Removes this node from the hierarchy, so if you call this,
    /// MAKE SURE YOU DO SOMETHING WITH IT. Otherwise it will float on the eternal oceans of memory.
    /// </summary>
    /// <returns></returns>
    public bool PromoteNewReplacement()
    {
        if (m_children.Count > 0)
        {
            // Check for any children WITHOUT children of their own. If one is found, make it the leader.
            for (int i = 0; i < m_children.Count; ++i)
            {
                if (m_children[i].GetChildren().Count > 0)
                {
                    m_children[i].ReceiveChildren(this);
                    m_children[i].SetParent(m_parent);
                    m_parent.RemoveChild(this);
                    m_parent = null;
                    return true;
                }
            }

            // we have no children that also have no children, therefor promote someone from a child to replace the replacement!
            AINode childReplacingThisOne = m_children[0];

            // recursive call here. DO NOT PUT ANY CYCLIC ELEMENTS INTO THE HIERARCHY, FUTURE PEOPLE. If you do, i will turn you into a cyclic element!
            childReplacingThisOne.PromoteNewReplacement();

            // the child should now have been replaced and removed from the hierarchy, therefor it is safe to replace this node
            childReplacingThisOne.ReceiveChildren(this);
            childReplacingThisOne.SetParent(m_parent);
            m_parent.RemoveChild(this);
            m_parent = null;
            return true;
        }

        // there are no children! =O
        return false;

    }

    /// <summary>
    /// Takes the children from the input node and sets their parents to itself
    /// </summary>
    /// <param name="node_">Node that is losing children</param>
    public void ReceiveChildren(AINode node_)
    {
        foreach (AINode child in node_.GetChildren())
        {
            child.SetParent(this);
            m_children.Add(child);
        }

        node_.GetChildren().Clear();
    }

    public void Destroy()
    {
        // make sure this node is replaced correctly so that the hierarchy is not broken.
        PromoteNewReplacement();

    }
}

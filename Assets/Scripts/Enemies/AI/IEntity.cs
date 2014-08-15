using UnityEngine;
using System.Collections.Generic;

public interface IEntity
{
    AINode GetAINode();

    void SetAINode(AINode node_);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="orderID_"></param>
    /// <param name="listOfParameters"></param>
    /// <returns>Returns true if the order was accepted. Used for inheritance mainly.</returns>
    bool ReceiveOrder(int orderID_, object[] listOfParameters);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="orderID_"></param>
    /// <param name="listOfParameters">Returns true if the order was given. Used for inheritance mainly.</param>
    bool GiveOrder(int orderID_, object[] listOfParameters);

    /// <summary>
    ///  Might be changed to being added to a list of diffent possibilities so it is not ignored, but rather placed in a queue.
    /// </summary>
    /// <param name="orderID_"></param>
    /// <param name="listOfParameters"></param>
    /// <returns>Returns true if the order was accepted. Used for inheritance mainly.</returns>
    bool ConsiderOrder(int orderID_, object[] listOfParameters);

    /// <summary>
    /// If a subordinate needs an order, this is what is used
    /// </summary>
    /// <param name="entity_">IEntity that needs an order</param>
    /// <returns></returns>
    bool RequestOrder(IEntity entity_);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="informationID_"></param>
    /// <returns>The information asked for.</returns>
    object[] RequestInformation(int informationID_);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="informationID_"></param>
    /// <param name="listOfParameters"></param>
    /// <returns>Returns true if the information was accepted. Used for inheritance mainly.</returns>
    bool Notify(int informationID_, object[] listOfParameters);

}

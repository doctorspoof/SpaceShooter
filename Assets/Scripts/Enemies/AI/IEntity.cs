using UnityEngine;
using System.Collections.Generic;

public interface IEntity
{

    bool ReceiveOrder(int orderID_, object[] listOfParameters);

    void GiveOrder(int orderID_, object[] listOfParameters);

    object[] RequestInformation(int informationID_);

}

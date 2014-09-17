using UnityEngine;
using System.Collections;

public class Cloneable : MonoBehaviour
{

    public GameObject Clone(Vector3 position_, Quaternion rotation_)
    {
        GameObject clone = CreateClone(position_, rotation_);

        NetworkView view = GetComponent<NetworkView>();

        // if there is a network view attached to this object, RPC that shit
        NetworkViewID newID;
        if (view != null)
        {
            newID = Network.AllocateViewID();
            clone.networkView.viewID = newID;

            networkView.RPC("PropagateClone", RPCMode.Others, position_, rotation_, newID);
        }

        return clone;
    }

    GameObject CreateClone(Vector3 position_, Quaternion rotation_)
    {
        GameObject clone = GetICloneable(gameObject).Clone();
        clone.transform.position = position_;
        clone.transform.rotation = rotation_;
        return clone;
    }

    [RPC] void PropagateClone(Vector3 position_, Quaternion rotation_, NetworkViewID id_)
    {
        GameObject obj = CreateClone(position_, rotation_);
        obj.networkView.viewID = id_;
    }

    public static ICloneable GetICloneable(GameObject object_)
    {
        foreach (Component comp in object_.GetComponents<Component>())
        {
            if (comp is ICloneable)
            {
                return comp as ICloneable;
            }
        }

        return null;
    }
}

public interface ICloneable
{
    GameObject Clone();
}
using UnityEngine;
using System.Collections.Generic;


public class PriorityList<T> where T : class
{

    // disable warning about private class PriorityNode having the same name 'T' for its generics as the outer class
#pragma warning disable 0693
    class PriorityNode<T>
#pragma warning restore 0693
    {
        public int priority;
        public T obj;

        public PriorityNode(int priority_, T obj_)
        {
            priority = priority_;
            obj = obj_;
        }
    }

    List<PriorityNode<T>> list;

    public PriorityList()
    {
        list = new List<PriorityNode<T>>(8);
    }

    public PriorityList(int initialCapacity_)
    {
        list = new List<PriorityNode<T>>(initialCapacity_);
    }

    public void Add(int priority_, T obj_)
    {

        for(int i = 0; i < list.Count; ++i)
        {
            if(priority_ < list[i].priority)
            {
                list.Insert(i, new PriorityNode<T>(priority_, obj_));
                return;
            }
        }

        list.Add(new PriorityNode<T>(priority_, obj_));

    }

    public bool Remove(T obj_)
    {
        for (int i = 0; i < list.Count; ++i)
        {
            if (obj_ == list[i].obj)
            {
                list.RemoveAt(i);
                return true;
            }
        }

        return false;
    }

    public T this[int key]
    {
        get
        {
            return list[key].obj;
        }
    }
	
}

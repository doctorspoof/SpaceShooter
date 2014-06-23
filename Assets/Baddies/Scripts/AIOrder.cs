using UnityEngine;
using System.Collections;

public class AIOrder<T>
{
	T m_orderee;
	public T Orderee
	{
		get { return m_orderee; }
		set { m_orderee = value; }
	}
	
	GameObject m_objectOfInterest;
	public GameObject ObjectOfInterest
	{
		get { return m_objectOfInterest; }
		set { m_objectOfInterest = value; }
	}
	
	Vector2 m_positionOfInterest;
	public Vector2 PositionOfInterest
	{
		get
		{
			if (m_positionOfInterest != null)
				return m_positionOfInterest;
			else
				return ObjectOfInterest.transform.position;
		}
		set { m_positionOfInterest = value; }
	}
	
	/// <summary>
	/// Checks whether order has been completed
	/// </summary>
	/// <returns></returns>
	public bool Completed()
	{
		return hasCompleted(Orderee, ObjectOfInterest, PositionOfInterest);
	}
	
	public delegate bool HasCompleted(T objectToActUpon, GameObject objectOfInterest, Vector3 positionOfInterest);
	HasCompleted hasCompleted;
	
	/// <summary>
	/// Attaches conditions to the class for checking whether it has completed
	/// </summary>
	/// <param name="func"></param>
	public void AttachCondition(HasCompleted func)
	{
		hasCompleted = func;
	}
	
	/// <summary>
	/// Applies function to variables
	/// </summary>
	public void Activate()
	{
		onActivate(Orderee, ObjectOfInterest, PositionOfInterest);
	}
	
	public delegate void OnActivate(T objectToActUpon, GameObject objectOfInterest, Vector3 positionOfInterest);
	OnActivate onActivate;
	
	/// <summary>
	/// Attaches generic delegates to the class for invokation
	/// </summary>
	/// <param name="func"></param>
	public void AttachAction(OnActivate func)
	{
		onActivate += func;
	}
	
}

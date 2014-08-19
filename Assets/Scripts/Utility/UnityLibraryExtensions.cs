using UnityEngine;
using System.Collections.Generic;


/// <summary>
/// This script is used to provide extension methods to the current unity library. This provides an easy way to access
/// general purpose functions which are used in a variety of different places.
/// </summary>
public static class UnityLibraryExtensions
{
	/// <summary>
	/// Returns the squared value.
	/// </summary>
	/// <param name="value">The value to square.</param>
	public static int Squared (this int value)
	{
		return value * value;
	}
	
	
	/// <summary>
	/// Returns the squared value.
	/// </summary>
	/// <param name="value">The value to square.</param>
	public static float Squared (this float value)
	{
		return value * value;
	}
	


	/// <summary>
	/// A simple generic swap function which will swap the values/pointers of two objects.
	/// </summary>
	/// <param name="lhs">The left hand value to swap with the right hand value.</param>
	/// <param name="rhs">The right hand value to swap with the left hand value.</param>
	/// <typeparam name="T">Ensures that the function applies to all types.</typeparam>
	public static void Swap<T> (this T t, ref T lhs, ref T rhs)
	{
		T temp = lhs;
		lhs = rhs;
		rhs = temp;
	}
	
	
	/// <summary>
	/// Returns whether the array contains the item passed using .Equals().
	/// </summary>
	/// <param name="array">The array of data.</param>
	/// <param name="check">The item to look for.</param>
	/// <typeparam name="T">Applies to any type</typeparam>
	public static bool Contains<T> (this T[] array, T check)
	{
		foreach (T item in array)
		{
			if (item.Equals (check))
			{
				return true;
			}
		}
		
		return false;
	}
	
	
	/// <summary>
	/// Filters the duplicates from the array and returns each unique values.
	/// </summary>
	/// <param name="duplicates">The array which contains duplicate items.</param>
	/// <typeparam name="T">Used in extending any array.</typeparam>
	public static T[] GetUniqueOnly<T> (this T[] duplicates)
	{
		// Pre-condition: "objects" can't be empty
		if (duplicates != null && duplicates.Length > 0)
		{
			// Create a temporary list 
			List<T> unique = new List<T>();
			
			// Loop through each item and check if it is already contained in "unique"
			foreach (T t in duplicates)
			{
				// Throw away null values
				if (t != null && !unique.Contains (t))
				{
					unique.Add (t);
				}
			}
			
			// Finally convert the unique to an array
			return unique.ToArray();
		}
		
		return new T[0];
	}


    /// <summary>
    /// Returns an array which doesn't contain nulls.
    /// </summary>
    /// <returns>A null-less array.</returns>
    /// <param name="array">The array to get information from.</param>
    public static T[] NoNulls<T> (this T[] array)
    {
        if (array != null)
        {
            List<T> toReturn = new List<T>(array.Length);

            for (int i = 0; i < array.Length; ++i)
            {
                if (array[i] != null)
                {
                    toReturn.Add (array[i]);
                }
            }

            return toReturn.ToArray();
        }

        return T[0];
    }


	/// <summary>
	/// Returns an array of GameObject objects which is gained through calling ".gameObject" on each Component.
	/// </summary>
	/// <returns>The results of ".gameObject" on each Component object.</returns>
	/// <param name="components">The array of Component objects to retrieve a GameObject from.</param>
	public static GameObject[] GetGameObjects (this Component[] components)
	{
		// Pre-condition: "components" must not be empty
		if (components != null && components.Length > 0)
		{
			GameObject[] objects = new GameObject[components.Length];
			
			for (int i = 0; i < components.Length; ++i)
			{
				// Make sure components[i] isn't null as accessing ".gameObject" will throw exceptions
				objects[i] = components[i] != null ? components[i].gameObject : null;
			}
			
			return objects;
		}
		
		return null;
	}



	public static void AddCustomExplosionForce (this Rigidbody self, Vector3 position, float range, float minForce, float maxForce, ForceMode mode = ForceMode.Force)
	{
		if (self != null)
		{
			// Get the difference/direciton
			Vector3 difference = self.position - position;

			// Use the magnitude to linearly reduce the force to apply
			float force = Mathf.Lerp (maxForce, minForce, difference.sqrMagnitude / range.Squared());

			// Apply the force to the rigidbody based on the direction from position to self
			self.AddForceAtPosition (difference.normalized * force, position);
		}
	}
	
	
	/// <summary>
	/// Given a Rigidbody it will compare the distances of each hit to find the closest hit of "target".
	/// This may not be incredibly efficient and only has a few select use-cases but it can be useful in determining
	/// the shortest distance between the raycast and a composite collider.
	/// </summary>
	/// <returns>The closest distance to the given Rigidbody, float.MaxValue if none exist.</returns>
	/// <param name="hits">Applies to all RaycastHit arrays.</param>
	/// <param name="target">The Rigidbody to look for in the array.</param>
	public static Vector3 GetClosestPointFromRigidbody (this Collider[] colliders, Rigidbody from, Vector3 to)
	{
		Vector3 closestPoint = Vector3.zero;
		float closestDistance = float.MaxValue;
		
		// Pre-condition: "hits" and "target" cannot be null.
		if (colliders != null && from)
		{
			// Use a cache for performance purposes
			Vector3 point;
			float sqrMagnitude = 0f;
			
			foreach (Collider collider in colliders)
			{
				// Pre-condition: Ensure all required attributes exist
                if (collider != null && collider.attachedRigidbody != null)
				{
					if (collider.attachedRigidbody == from)
					{
						point = collider.ClosestPointOnBounds (to);
						sqrMagnitude = (point - to).sqrMagnitude;
						
						if (sqrMagnitude <= closestDistance)
						{
							closestPoint = point;
							closestDistance = sqrMagnitude;
						}
					}
				}
			}
		}
		
		return closestPoint;
	}
	
	
	/// <summary>
	/// Returns an array of Rigidbody objects which is gained through calling ".attachedRigidbody" on each Collider.
	/// </summary>
	/// <returns>The results of ".attachedRigidbody" on each Collider object.</returns>
	/// <param name="colliders">The array of Collider objects to retrieve a Rigidbody from.</param>
	public static Rigidbody[] GetAttachedRigidbodies (this Collider[] colliders)
	{
		// Pre-condition: "colliders" must not be empty
		if (colliders != null && colliders.Length > 0)
		{
			Rigidbody[] rigidbodies = new Rigidbody[colliders.Length];
			
			for (int i = 0; i < colliders.Length; ++i)
			{
				// Make sure "colliders[i]" isn't null as accessing ".gameObject" will throw exceptions
                rigidbodies[i] = colliders[i] != null ? colliders[i].attachedRigidbody : null;
			}
			
			return rigidbodies;
		}
		
		return null;
	}
	
	
	/// <summary>
	/// Returns an array of Rigidbody objects which is gained through calling ".attachedRigidbody" on each ".collider".
	/// </summary>
	/// <returns>The results of ".attachedRigidbody" on each ".collider" object.</returns>
	/// <param name="hits">The array of RaycastHit objects to retrieve a Rigidbody from.</param>
	public static Rigidbody[] GetAttachedRigidbodies (this RaycastHit[] hits)
	{
		// Pre-condition: "hits" must not be empty
		if (hits != null && hits.Length > 0)
		{
			Rigidbody[] rigidbodies = new Rigidbody[hits.Length];
			
			for (int i = 0; i < hits.Length; ++i)
			{
				// Make sure "hits[i]" isn't null as accessing ".gameObject" will throw exceptions
				if (hits[i].collider == null || hits[i].collider.attachedRigidbody == null)
				{
					rigidbodies[i] = null;
				}
				
				else
				{
					rigidbodies[i] = hits[i].collider.attachedRigidbody;
				}
			}
			
			return rigidbodies;
		}
		
		return null;
	}
}

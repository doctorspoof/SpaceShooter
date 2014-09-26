using UnityEngine;
using System.Collections;
using System.Collections.Generic;



/// <summary>
/// Abilities is an Ability management class which stores all of the available abilities and provides an access point to them.
/// </summary>
public sealed class Abilities : MonoBehaviour 
{
    List<Ability> m_abilities = new List<Ability>(0); //!< The list of unlocked abilities available to the ship.


    #region Getters & setters

    /// <summary>
    /// Returns a read only version of the abilities list. Useful for the GUI.
    /// </summary>
    /// <returns>A read only copy of the available abilities.</returns>
    public IList<Ability> GetAbilities()
    {
        return m_abilities.AsReadOnly();
    }


    /// <summary>
    /// Finds the ability of the given type and returns it. Could be null if it doesn't exist.
    /// </summary>
    /// <returns>The ability.</returns>
    /// <typeparam name="T">The 1st type parameter.</typeparam>
    public T GetAbility<T>() where T : Ability
    {
        return FindAbilityOfType<T>();
    }


    public int GetCount()
    {
        return m_abilities.Count;
    }


    public Ability this[int index]
    {
        get 
        {
            try 
            {
                return m_abilities[index];
            }
            
            catch (System.Exception error)
            {
                Debug.LogError ("Exception occurred in " + name + ".Abilities: " + error.Message);            
                return null;
            }
        }
    }

    #endregion Getters & setters


    #region Locking/ability management

    /// <summary>
    /// Finds the ability of the given type.
    /// </summary>
    /// <returns>The results of the search.</returns>
    /// <typeparam name="T">The ability to look for.</typeparam>
    T FindAbilityOfType<T>() where T : Ability
    {
        // Determine T's type
        System.Type typeOfT = typeof (T);
        
        // Find the ability
        for (int i = 0; i < m_abilities.Count; ++i)
        {
            // Check if the types are the same.
            if (m_abilities[i] != null && Object.ReferenceEquals (m_abilities[i].GetType(), typeOfT))
            {
                return m_abilities[i] as T;
            }
        }
        
        return null;
    }
    
    
    /// <summary>
    /// Will lock the ability of the given type.
    /// </summary>
    /// <param name="deleteObject">If set to <c>true</c> delete the ability from the list.</param>
    /// <typeparam name="T">The ability type to be locked.</typeparam>
    public T Lock<T> (bool deleteObject = false) where T : Ability, new()
    {
        // Find the ability
        T ability = FindAbilityOfType<T>();
        
        // Ensure that what we are locking actually exists
        if (ability != null)
        {
            ability.SetLockState(true);
            
            // Delete the ability if necessary
            if (deleteObject)
            {
                m_abilities.Remove (ability);
                ability = null;
            }
        }
        
        // Daisy chain!
        return ability;
    }


    /// <summary>
    /// Unlock an ability so that a Ship may use it. Will create the ability if it doesn't exist.
    /// </summary>
    /// <typeparam name="T">The ability class to unlock.</typeparam>
    public T Unlock<T>() where T : Ability, new()
    {
        // Find the index
        T ability = FindAbilityOfType<T>();

        // Check if needs creating
        if (ability == null)
        {
            ability = new T();

            m_abilities.Add (ability);
        }

        // Unlock it and return it
        ability.SetLockState (false);

        return ability;
    }

    #endregion Locking/Ability management


    #region Cooldown management

    /// <summary>
    /// Finds the ability of the given type and starts the cooldown process on it.
    /// </summary>
    /// <param name="restartCooldown">Pass true to start the cooldown </param>
    /// <typeparam name="T">The 1st type parameter.</typeparam>
    public T Cool<T> (bool restartCooldown) where T : Ability
    {
        T ability = FindAbilityOfType<T>();

        if (ability != null)
        {
            // Check if we should start the cooldown again
            if (restartCooldown)
            {
                ability.ResetCooldown();
            }

            StartCoroutine (CoolAbility (ability));
        }

        return ability;
    }


    IEnumerator CoolAbility (Ability toCool)
    {
        if (!toCool.IsCooling())
        {
            // Start the cooling process
            toCool.SetCoolingState (true);

            do
            {
                // Reduce the cooldown
                yield return null;
                toCool.AlterCooldown (-Time.deltaTime);

            } while (!toCool.HasCooled());

            toCool.SetCoolingState (false);
        }

        else
        {
            Debug.LogWarning ("Attempt to cool an ability which is already cooling (" + toCool.GetType() + ").");
        }
    }

    #endregion Cooldown management

}
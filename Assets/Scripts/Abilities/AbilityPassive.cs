using UnityEngine;



/// <summary>
/// An ability which is always activated and can't be turned off.
/// </summary>
public abstract class AbilityPassive : Ability 
{
    public override bool IsActive()
    {
        return false;
    }
}

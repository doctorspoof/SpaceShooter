using UnityEngine;



/// <summary>
/// An ability which requires active triggering for it to be functional.
/// </summary>
public abstract class AbilityActive : Ability 
{
    public override bool IsActive()
    {
        return true;
    }
}

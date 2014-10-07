using UnityEngine;
using System.Collections;

public class AbilityShieldCollapseFlash : AbilityShieldCollapse 
{
    float m_stunDuration = 0.0f;
    int m_aoeMask = 0;

    #region Get/Set
    public void SetDuration(float duration)
    {
        m_stunDuration = duration;
    }
    public void SetAoeMask(int mask)
    {
        m_aoeMask = mask;
    }
    #endregion

    public override string GetGUIName()
    {
        return "Flashbang";
    }
    
    public override void ActivateAbility (GameObject caster)
    {
        if(HasCooled())
        {
            Collider[] colliders = Physics.OverlapSphere(caster.transform.position, m_effectRange, m_aoeMask);
            Rigidbody[] unique = colliders.GetAttachedRigidbodies().GetUniqueOnly();
            
            //TODO: Spawn an effect here
            
            foreach (Rigidbody mob in unique)
            {
                mob.GetComponent<Ship>().AddDebuff(new DebuffDisable(m_stunDuration, mob.gameObject));
            }
            
            ResetCooldown();
        }
    }
}

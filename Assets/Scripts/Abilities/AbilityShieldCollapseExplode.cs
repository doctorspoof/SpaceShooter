using UnityEngine;
using System.Collections;

public class AbilityShieldCollapseExplode : AbilityShieldCollapse 
{
    int m_aoeMask = 0;
    float m_maxDamageRange = 0.0f;
    float m_damage = 0;

    #region Get/Set
    public void SetAoEMask(int mask)
    {
        m_aoeMask = mask;
    }
    public void SetMaxDamRange(float maxDam)
    {
        m_maxDamageRange = maxDam;
    }
    public void SetDamage(float damage)
    {
        m_damage = damage;
    }
    #endregion

    public override string GetGUIName()
    {
        return "Firestorm";
    }
    
    public override void ActivateAbility (GameObject caster)
    {
        if(HasCooled())
        {
            Collider[] colliders = Physics.OverlapSphere(caster.transform.position, m_effectRange, m_aoeMask);
            Rigidbody[] unique = colliders.GetAttachedRigidbodies().GetUniqueOnly();
            
            //TODO: Spawn an effect here
            
            // Cache values for the sake of efficiency, also avoid Vector3.Distance by squaring ranges
            float   distance = 0f, damage = 0f,
            maxDistance = m_effectRange - m_maxDamageRange;
            
            foreach (Rigidbody mob in unique)
            {
                // Ensure the distance will equate to 0f - 1f for the Lerp function
                distance = (caster.transform.position - colliders.GetClosestPointFromRigidbody(mob, caster.transform.position)).magnitude - m_maxDamageRange;
                distance = Mathf.Clamp(distance, 0f, maxDistance);
                damage = Mathf.Lerp(m_damage, 1, distance / maxDistance);
                
                mob.GetComponent<HealthScript>().DamageMob((int)damage, caster);
            }
        
            ResetCooldown();
        }
    }
}

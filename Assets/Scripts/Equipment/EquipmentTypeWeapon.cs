using UnityEngine;



public sealed class EquipmentTypeWeapon : BaseEquipment 
{
    #region Serializable Properties

    // Base stats to reset to and start from
    [SerializeField]                        BulletProperties    m_baseBulletStats = null;
    [SerializeField, Range (0.001f, 10f)]   float               m_baseWeaponReloadTime = 0.7f;
    
    // Current stats (base + augment effects)
    public                                        BulletProperties    m_currentBulletStats = new BulletProperties();
    public                                        float               m_currentWeaponReloadTime = 0.0f;

    #endregion


    // Internal usage members
    float m_currentReloadCounter = 0.0f;


    #region BaseEquipment overrides
    
    /// <summary>
    /// Resets the bullet stats and reload time to their default values.
    /// </summary>
    protected override void ResetToBaseStats()
    {
        m_currentBulletStats.CloneProperties (m_baseBulletStats);
        m_currentWeaponReloadTime = m_baseWeaponReloadTime;
    }


    /// <summary>
    /// Calculates the current stats based on the equipped augments and their tier.
    /// </summary>
	protected override void CalculateCurrentStats()
    {
        for (int i = 0; i < m_augmentSlots.Length; i++)
        {
            if (m_augmentSlots[i] != null)
            {
                float scalar = ElementalValuesWeapon.TierScalar.GetScalar (m_augmentSlots[i].GetTier());
                Element element = m_augmentSlots[i].GetElement();

                switch (element)
                {
                    case Element.Fire:
                    {
                        ElementResponseFire (scalar);
                        break;
                    }

                    case Element.Ice:
                    {
                        ElementResponseIce (scalar);
                        break;
                    }

                    case Element.Earth:
                    {
                        ElementResponseEarth (scalar);
                        break;
                    }

                    case Element.Lightning:
                    {
                        ElementResponseLightning (scalar);
                        break;
                    }

                    case Element.Light:
                    {
                        ElementResponseLight (scalar);
                        break;
                    }

                    case Element.Dark:
                    {
                        ElementResponseDark (scalar);
                        break;
                    }

                    case Element.Spirit:
                    {
                        ElementResponseSpirit (scalar);
                        break;
                    }

                    case Element.Gravity:
                    {
                        ElementResponseGravity (scalar);
                        break;
                    }

                    case Element.Air:
                    {
                        ElementResponseAir (scalar);
                        break;
                    }

                    case Element.Organic:
                    {
                        ElementResponseOrganic (scalar);
                        break;
                    }
                }

                m_currentBulletStats.appliedElements.Add (element);
            }
        }
    }
   

    protected override void ElementResponseFire (float scalar)
    {
        // Change base effectiveness
        m_currentBulletStats.damage                     += (int) (m_baseBulletStats.damage * ElementalValuesWeapon.Fire.damageMulti * scalar);
        m_currentWeaponReloadTime                       += m_baseWeaponReloadTime * ElementalValuesWeapon.Fire.reloadTimeMulti * scalar;

        // AoE effectiveness
        m_currentBulletStats.aoe.isAOE                  = ElementalValuesWeapon.Fire.isAOE;
        m_currentBulletStats.aoe.aoeRange               += m_baseBulletStats.aoe.aoeRange * ElementalValuesWeapon.Fire.aoeRangeMulti * scalar;
        m_currentBulletStats.aoe.aoeMaxDamageRange      += m_baseBulletStats.aoe.aoeMaxDamageRange * ElementalValuesWeapon.Fire.aoeMaxDamageRangeMulti * scalar;
        m_currentBulletStats.aoe.aoeExplosiveForce      += m_baseBulletStats.aoe.aoeExplosiveForce * ElementalValuesWeapon.Fire.aoeExplosiveForceMulti * scalar;
        m_currentBulletStats.aoe.aoeMaxFalloff          += ElementalValuesWeapon.Fire.aoeMaxFalloffInc * scalar;
    }


    protected override void ElementResponseIce (float scalar)
    {
        // Change base effectiveness
        m_currentBulletStats.damage                     += (int) (m_baseBulletStats.damage * ElementalValuesWeapon.Ice.damageMulti * scalar);
        m_currentWeaponReloadTime                          += m_baseWeaponReloadTime * ElementalValuesWeapon.Ice.reloadTimeMulti * scalar;

        // Special effects
        m_currentBulletStats.special.slowDuration       += ElementalValuesWeapon.Ice.slowDurationInc * scalar;
    }


    protected override void ElementResponseEarth (float scalar)
    {
        // Change base effectiveness
        m_currentBulletStats.damage                     += (int) (m_baseBulletStats.damage * ElementalValuesWeapon.Earth.damageMulti * scalar);
        m_currentBulletStats.reach                      += m_baseBulletStats.reach * ElementalValuesWeapon.Earth.reachMulti * scalar;
        m_currentBulletStats.lifetime                   += m_baseBulletStats.lifetime * ElementalValuesWeapon.Earth.lifetimeMulti * scalar;
        m_currentWeaponReloadTime                          += m_baseWeaponReloadTime * ElementalValuesWeapon.Earth.reloadTimeMulti * scalar;       
    }


    protected override void ElementResponseLightning (float scalar)
    {
        // Change base effectiveness
        m_currentBulletStats.damage                     += (int) (m_baseBulletStats.damage * ElementalValuesWeapon.Lightning.damageMulti * scalar);
        
        // Special effects
        m_currentBulletStats.special.chanceToJump       += ElementalValuesWeapon.Lightning.chanceToJumpInc * scalar;
    }


    protected override void ElementResponseLight (float scalar)
    {
        // Change base effectiveness
        m_currentBulletStats.damage                     += (int) (m_baseBulletStats.damage * ElementalValuesWeapon.Light.damageMulti * scalar);
        m_currentWeaponReloadTime                       += m_baseWeaponReloadTime * ElementalValuesWeapon.Light.reloadTimeMulti * scalar;
        
        // Enable beam effectiveness
        m_currentBulletStats.isBeam                     = ElementalValuesWeapon.Light.isBeam;
    }


    protected override void ElementResponseDark (float scalar)
    {
        // Change base effectiveness
        m_currentBulletStats.damage                     += (int) (m_baseBulletStats.damage * ElementalValuesWeapon.Dark.damageMulti * scalar);

        // Increase disability functionality
        m_currentBulletStats.special.chanceToDisable    += ElementalValuesWeapon.Dark.chanceToDisableInc * scalar;
        m_currentBulletStats.special.disableDuration    += ElementalValuesWeapon.Dark.disableDurationInc * scalar;
    }


    protected override void ElementResponseSpirit (float scalar)
    {
        // Change base effectiveness
        m_currentBulletStats.damage                     += (int) (m_baseBulletStats.damage * ElementalValuesWeapon.Spirit.damageMulti * scalar);
        
        // Piercing effectiveness
        m_currentBulletStats.piercing.isPiercing        = ElementalValuesWeapon.Spirit.isPiercing;
        m_currentBulletStats.piercing.maxPiercings      += (int) (m_baseBulletStats.piercing.maxPiercings * ElementalValuesWeapon.Spirit.maxPiercingsMulti * scalar);
        m_currentBulletStats.piercing.pierceModifier    += ElementalValuesWeapon.Spirit.piercingModifierInc * scalar;
    }


    protected override void ElementResponseGravity (float scalar)
    {
        // Change base effectiveness
        m_currentBulletStats.damage                     += (int) (m_baseBulletStats.damage * ElementalValuesWeapon.Gravity.damageMulti * scalar);
        
        // Homing effectiveness
        m_currentBulletStats.homing.isHoming            = ElementalValuesWeapon.Gravity.isHoming;
        m_currentBulletStats.homing.homingRange         += m_baseBulletStats.homing.homingRange * ElementalValuesWeapon.Gravity.homingRangeMulti * scalar;
        m_currentBulletStats.homing.homingTurnRate      += m_baseBulletStats.homing.homingTurnRate * ElementalValuesWeapon.Gravity.homingTurnRateMulti * scalar;
    }


    protected override void ElementResponseAir (float scalar)
    {
        // Change base effectiveness
        m_currentBulletStats.reach                      += m_baseBulletStats.reach * ElementalValuesWeapon.Air.reachMulti * scalar;
        m_currentWeaponReloadTime                       += m_baseWeaponReloadTime * ElementalValuesWeapon.Air.reloadTimeMulti * scalar;
    }


    protected override void ElementResponseOrganic (float scalar)
    {
        // DoT effectiveness
        m_currentBulletStats.special.dotDuration        += ElementalValuesWeapon.Organic.dotDurationInc * scalar;
        m_currentBulletStats.special.dotEffect          += (int) (m_baseBulletStats.damage * ElementalValuesWeapon.Organic.dotEffectInc * scalar);
    }
    
    #endregion
}

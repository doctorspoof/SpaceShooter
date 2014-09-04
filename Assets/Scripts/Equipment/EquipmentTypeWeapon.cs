using UnityEngine;
using System.Collections;
using System.Collections.Generic;



namespace ElementalValuesWeapon
{
    /// <summary>
    /// The Fire elemental should cause an AoE effect on a weapon and increase the effectiveness of that AoE damage when stacked.
    /// </summary>
    public sealed class Fire
    {
        public const bool   isAoe                   = true;     //!< Ensure the bullet gets set to be AoE.

        public const float  damageMulti             = -0.1f,    //!< Decrease the base bullet damage.
                            reloadTimeMulti         = 0.1f,     //!< Increase the reload time.
                            aoeRangeMulti           = 0.1f,     //!< Extend the range of the AoE effect.
                            aoeMaxDamageRangeMulti  = 0.1f,     //!< Extend the range at which enemies will receive maximum damage with no falloff.
                            aoeExplosiveForceMulti  = 0.1f,     //!< Increase the force applied to enemies.
                            aoeMaxFalloffInc        = 0.1f;     //!< Increment the max falloff effect.
    }


    /// <summary>
    /// The Ice elemental should increase damage slightly but also cause a slowing effect on hit which progressively gets longer when stacked.
    /// </summary>
    public sealed class Ice
    {
        public const float  damageMulti             = 0.1f,     //!< Increase the base bullet damage.
                            reloadTimeMulti         = -0.1f,    //!< Decrease the reload time.
                            slowDurationInc         = 0.5f;     //!< Increment the slow effect of the bullet.
    }


    /// <summary>
    /// The Earth elemental should cause the bullet to act more like a cannon, high burst damage, long recovery and moderate speed.
    /// </summary>
    public sealed class Earth
    {
        public const float  damageMulti             = 0.2f,     //!< Increase the base damage significantly.
                            reloadTimeMulti         = 0.2f,     //!< Increase the reload time (negative)
                            reachMulti              = 0.1f,     //!< Increase the max distance of the bullet.
                            lifetimeMulti           = -0.1f;    //!< Decrease the lifetime (increasing the speed).
                            
    }


    /// <summary>
    /// The lightning elemental should cause the bullet to jump between targets on hit, sharing damage with a nearby foe.
    /// </summary>
    public sealed class Lightning
    {
        public const float  damageMulti             = -0.1f,     //!< Decrease base damage slightly.
                            chanceToJumpInc         = 0.1f;      //!< Add to the percentage chance.
    }


    /// <summary>
    /// Light should enable beam functionality but also give a buff to damage but increase the reload time.
    /// </summary>
    public sealed class Light
    {
        public const bool   isBeam                  = true;     //!< Enable beam functionality

        public const float  damageMulti             = 0.1f,     //!< Increase base bullet damage slightly.
                            reloadTimeMulti         = 0.1f;     //!< Increase the reload time (negative).

    }


    /// <summary>
    /// Dark gives a chance on hit to disable targets.
    /// </summary>
    public sealed class Dark
    {
        public const float  damageMulti             = 0.1f,     //!< Increase the base damage of the bullet.
                            chanceToDisableInc      = 0.1f,     //!< Increment the chance to disable targets.
                            disableDurationInc      = 0.1f;     //!< Incrememnt the duration of the disable effect.
    }


    /// <summary>
    /// The Spirit element is all about piercing through targets in an etherial manner.
    /// </summary>
    public sealed class Spirit
    {
        public const bool   isPiercing              = true;     //!< Enable piercing functionality on bullets.

        public const float  damageMulti             = 0.1f,     //!< Increase zee base damage captain!
                            maxPiercingsMulti       = 0.5f,     //!< Increase the number of piercings possible.
                            piercingModifierInc     = 0.1f;     //!< Reduce the amount piercing reducues damage and speed.                                                        
    }


    /// <summary>
    /// Gravity enables and increases homing functionality of a bullet.
    /// </summary>
    public sealed class Gravity
    {
        public const bool   isHoming                = true;     //!< Enable homing functionality on the bullet.

        public const float  damageMulti             = 0.1f,     //!< Increase damage lulz.
                            homingRangeMulti        = 0.1f,     //!< Increase the homing range.
                            homingTurnRateMulti     = 0.1f;     //!< Increase the speed at which the bullet can turn.
    }


    /// <summary>
    /// Air is more of a helper element, it reduces cooldown and increases the reach of the bullet to make up for other elements.
    /// </summary>
    public sealed class Air
    {
        public const float  reloadTimeMulti         = -0.1f,    //!< Reduce the reload time of the weapon.
                            reachMulti              = 0.1f;     //!< Increase the range of the bullet.
    }


    /// <summary>
    /// Organic enables and enhances damage-over-time functionality based on the bullets base damage.
    /// </summary>
    public sealed class Organic
    {
        public const float  dotDurationInc          = 0.1f,     //!< Increment the duration DoT is applied.
                            dotEffectInc            = 0.1f;     //!< Increment the percentage of the base damage to be applied over time.
    }
}



public sealed class EquipmentTypeWeapon : BaseEquipment 
{
    #region Serializable Properties

    // Base stats to reset to and start from
    [SerializeField]                            BulletProperties    m_baseBulletStats = null;
    [SerializeField, Range (0.001f, 10.0f)]     float               m_baseWeaponReloadTime = 0.7f;
    
    // Current stats (base + augment effects)
                                                BulletProperties    m_currentBulletStats = null;
                                                float               m_currentWeaponReloadTime = 0.0f;

    #endregion


    // Internal usage members
    float m_currentReloadCounter = 0.0f;

    #region Overrides
    
    protected override void ResetToBaseStats()
    {
        m_currentBulletStats.CloneProperties(m_baseBulletStats);
        m_currentWeaponReloadTime = m_baseWeaponReloadTime;
    }
    
	protected override void CalculateCurrentStats()
    {
        for(int i = 0; i < m_augmentSlots.Length; i++)
        {
            switch(m_augmentSlots[i].GetElement())
            {
                case Element.Fire:
                {
                    ElementResponseFire(m_augmentSlots[i].GetTier());
                    break;
                }
                case Element.Ice:
                {
                    ElementResponseIce(m_augmentSlots[i].GetTier());
                    break;
                }
                case Element.Earth:
                {
                    ElementResponseEarth(m_augmentSlots[i].GetTier());
                    break;
                }
                case Element.Lightning:
                {
                    ElementResponseLightning(m_augmentSlots[i].GetTier());
                    break;
                }
                case Element.Light:
                {
                    ElementResponseLight(m_augmentSlots[i].GetTier());
                    break;
                }
                case Element.Dark:
                {
                    ElementResponseDark(m_augmentSlots[i].GetTier());
                    break;
                }
                case Element.Spirit:
                {
                    ElementResponseSpirit(m_augmentSlots[i].GetTier());
                    break;
                }
                case Element.Gravity:
                {
                    ElementResponseGravity(m_augmentSlots[i].GetTier());
                    break;
                }
                case Element.Air:
                {
                    ElementResponseAir(m_augmentSlots[i].GetTier());
                    break;
                }
                case Element.Organic:
                {
                    ElementResponseOrganic(m_augmentSlots[i].GetTier());
                    break;
                }
            }
        }
    }
    
    void IncreaseBulletDamage(int increment)
    {
        if(m_currentBulletStats.special != null && m_currentBulletStats.special.dotEffect != 0f)
        {
            //Increase dot instead
            m_currentBulletStats.special.dotEffect += increment;
        }
        else
        {
            //Increase damage as normal
            m_currentBulletStats.damage += increment;
        }
    }
    
    //Element Responses
    //TODO: Add in tier effects
    protected override void ElementResponseFire (int tier)
    {
        //If the aoe component doesn't exist, make one and initialise to base
        if(m_currentBulletStats.aoe == null)
        {
            AOEAttributes newAoE = new AOEAttributes();
            m_currentBulletStats.aoe = newAoE;
            
            //Give it default vales
            newAoE.isAOE = true;
            newAoE.aoeRange = 5.0f;
            newAoE.aoeMaxDamageRange = 1.25f;
            newAoE.aoeExplosiveForce = 10.0f;
            newAoE.aoeMaxFalloff = 0.4f;
        }
        //Otherwise, add effects on to the existing component
        else
        {
            AOEAttributes oldAoE = m_currentBulletStats.aoe;
            
            oldAoE.aoeRange += 4.5f;
            oldAoE.aoeMaxDamageRange += 0.5f;
            oldAoE.aoeExplosiveForce += 5.0f;
            oldAoE.aoeMaxFalloff -= 0.1f;
        }
        
        //Now do non-aoe stuff
        IncreaseBulletDamage(12);
        m_currentWeaponReloadTime += 0.5f;
        
        //Finally, add the element applied to the bullet
        m_currentBulletStats.appliedElements.Add(Element.Fire);
    }
    protected override void ElementResponseIce (int tier)
    {
        if(m_currentBulletStats.special == null)
        {
            SpecialAttributes newSpec = new SpecialAttributes();
            m_currentBulletStats.special = newSpec;
            
            //Initialise
            newSpec.chanceToJump = 0f; 
            newSpec.chanceToDisable = 0f;   
            newSpec.disableDuration = 0f;     
            newSpec.slowDuration = 0.75f;        
            newSpec.dotDuration = 0f;      
            newSpec.dotEffect = 0f; 
        }
        else
        {
            SpecialAttributes oldSpec = m_currentBulletStats.special;
            
            oldSpec.slowDuration += 0.6f;
        }
        
        //Do non-special stuff
        IncreaseBulletDamage(4);
        
        //Add the element
        m_currentBulletStats.appliedElements.Add(Element.Ice);
    }
    protected override void ElementResponseEarth (int tier)
    {
        //Nothing special here, just stats
        IncreaseBulletDamage(35);
        m_currentBulletStats.reach += 4.0f;
        m_currentBulletStats.lifetime -= 0.3f;
        
        m_currentWeaponReloadTime += 1.0f;
        
        //Add the element
        m_currentBulletStats.appliedElements.Add(Element.Earth);
    }
    protected override void ElementResponseLightning (int tier)
    {
        if(m_currentBulletStats.special == null)
        {
            SpecialAttributes newSpec = new SpecialAttributes();
            m_currentBulletStats.special = newSpec;
            
            //Initialise
            newSpec.chanceToJump = 0.2f; 
            newSpec.chanceToDisable = 0f;   
            newSpec.disableDuration = 0f;     
            newSpec.slowDuration = 0.0f;        
            newSpec.dotDuration = 0f;      
            newSpec.dotEffect = 0f; 
        }
        else
        {
            SpecialAttributes oldSpec = m_currentBulletStats.special;
            
            oldSpec.chanceToJump += 0.2f;
        }
        
        //Do non-special stuff
        IncreaseBulletDamage(4);
        
        //Add to element list
        m_currentBulletStats.appliedElements.Add(Element.Lightning);
    }
    protected override void ElementResponseLight (int tier)
    {
        //TODO: rethink reload vs beams, light stacking etc.
        if(!m_currentBulletStats.isBeam)
        {
            m_currentBulletStats.isBeam = true;
        }
        else
        {
            m_currentBulletStats.damage += 4;
            m_currentWeaponReloadTime += 1.5f;
        }
        
        IncreaseBulletDamage(4);
        
        //Add to element list
        m_currentBulletStats.appliedElements.Add(Element.Light);
    }
    protected override void ElementResponseDark (int tier)
    {
        if(m_currentBulletStats.special == null)
        {
            SpecialAttributes newSpec = new SpecialAttributes();
            m_currentBulletStats.special = newSpec;
            
            //Initialise
            newSpec.chanceToJump = 0.0f; 
            newSpec.chanceToDisable = 0.15f;   
            newSpec.disableDuration = 0f;     
            newSpec.slowDuration = 0.0f;        
            newSpec.dotDuration = 0f;      
            newSpec.dotEffect = 0f; 
        }
        else
        {
            SpecialAttributes oldSpec = m_currentBulletStats.special;
            
            oldSpec.disableDuration += 0.15f;
        }
        
        //Non-special
        IncreaseBulletDamage(4);
        
        //Add to element list
        m_currentBulletStats.appliedElements.Add(Element.Dark);
    }
    protected override void ElementResponseSpirit (int tier)
    {
        if(m_currentBulletStats.piercing == null)
        {
            PiercingAttributes newPier = new PiercingAttributes();
            m_currentBulletStats.piercing = newPier;
            
            //Initialise
            newPier.isPiercing = true;
            newPier.maxPiercings = 2;
            newPier.pierceModifier = 0.7f;
        }
        else
        {
            PiercingAttributes oldPier = m_currentBulletStats.piercing;
            
            oldPier.maxPiercings += 2;
            oldPier.pierceModifier -= 0.15f;
        }
        
        IncreaseBulletDamage(4);
        
        //Add to element list
        m_currentBulletStats.appliedElements.Add(Element.Spirit);
    }
    protected override void ElementResponseGravity (int tier)
    {
        if(m_currentBulletStats.homing == null)
        {
            HomingAttributes newHome = new HomingAttributes();
            m_currentBulletStats.homing = newHome;
            
            newHome.isHoming = true;
            newHome.homingRange = 8.5f;
            newHome.homingTurnRate = 4.5f;
        }
        else
        {
            HomingAttributes oldHome = m_currentBulletStats.homing;
            
            oldHome.homingRange += 4.0f;
            oldHome.homingTurnRate += 1.25f;
        }
        
        
        IncreaseBulletDamage(4);
        
        //Add to element list
        m_currentBulletStats.appliedElements.Add(Element.Gravity);
    }
    protected override void ElementResponseAir (int tier)
    {
        //Nothing special here
        m_currentBulletStats.reach += 4.0f;
        m_baseWeaponReloadTime -= 0.4f;
        
        //Add element to list
        m_currentBulletStats.appliedElements.Add(Element.Air);
    }
    protected override void ElementResponseOrganic (int tier)
    {
        if(m_currentBulletStats.special == null)
        {
            SpecialAttributes newSpec = new SpecialAttributes();
            m_currentBulletStats.special = newSpec;
            
            //Initialise
            newSpec.chanceToJump = 0.0f; 
            newSpec.chanceToDisable = 0.0f;   
            newSpec.disableDuration = 0f;     
            newSpec.slowDuration = 0.0f;        
            newSpec.dotDuration = 2.0f;      
            newSpec.dotEffect = m_currentBulletStats.damage;
        }
        else
        {
            SpecialAttributes oldSpec = m_currentBulletStats.special;
            
            oldSpec.dotDuration -= 0.5f;
            m_currentBulletStats.damage = (int)m_currentBulletStats.special.dotEffect;
            IncreaseBulletDamage(6);
        }
        
        IncreaseBulletDamage(4);
    
        //Add element to list
        m_currentBulletStats.appliedElements.Add(Element.Organic);
    }
    
    #endregion
}

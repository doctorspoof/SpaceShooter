using UnityEngine;
using System.Collections;
using System.Collections.Generic;

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
            newSpec.disableEffect = 0f;     
            newSpec.slowEffect = 0.75f;        
            newSpec.dotDuration = 0f;      
            newSpec.dotEffect = 0f; 
        }
        else
        {
            SpecialAttributes oldSpec = m_currentBulletStats.special;
            
            oldSpec.slowEffect += 0.6f;
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
            newSpec.disableEffect = 0f;     
            newSpec.slowEffect = 0.0f;        
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
            newSpec.disableEffect = 0f;     
            newSpec.slowEffect = 0.0f;        
            newSpec.dotDuration = 0f;      
            newSpec.dotEffect = 0f; 
        }
        else
        {
            SpecialAttributes oldSpec = m_currentBulletStats.special;
            
            oldSpec.disableEffect += 0.15f;
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
            newSpec.disableEffect = 0f;     
            newSpec.slowEffect = 0.0f;        
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

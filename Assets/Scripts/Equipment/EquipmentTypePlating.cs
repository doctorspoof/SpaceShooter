using UnityEngine;
using System.Collections.Generic;


/// <summary>
/// Contains all of the unique statistics of the plating.
/// </summary>
[System.Serializable] public sealed class PlatingProperties
{
    [Range (0, 10000)]  public int      hp                  = 100;      //!< How much HP the plating should have.
    [Range (0f, 100f)]  public float    mass                = 1f;       //!< How much the plating should weigh.

    [Range (0f, 1f)]    public float    regen               = 0f;       //!< How much HP to regenerate a second (percentage).
    [Range (0f, 1f)]    public float    returnDamage        = 0f;       //!< How much damage should be returned to aggressors (percentage).
    [Range (0f, 10f)]   public float    slowDuration        = 0f;       //!< How long to slow targets which physically touch the plating.
    [Range (0f, 1f)]    public float    lifesteal           = 0f;       //!< How much of a lifesteal effect the plating should have (percentage).
    [Range (0f, 1f)]    public float    chanceToJump        = 0f;       //!< The chance of debuffs passing onto nearby enemies (percentage).
    [Range (0f, 1f)]    public float    chanceToCleanse     = 0f;       //!< The chance of cleansing the plating of debuffs (percentage).
    [Range (0f, 1f)]    public float    chanceToEthereal    = 0f;       //!< The chance of turning ethereal on hit (percentage).
    [Range (0f, 10f)]   public float    etherealDuration    = 2f;       //!< How long stay ethereal once triggered.

                        public bool     slowsIncoming       = false;    //!< Whether to slow incoming projectiles or not.
    [Range (0f, 1f)]    public float    speedReduction      = 0f;       //!< How much to reduce the projectiles speed by (percentage).

    public void CloneProperties (PlatingProperties stats)
    {
        if (stats != null)
        {            
            hp = stats.hp;
            mass = stats.mass;
            
            regen = stats.regen;
            returnDamage = stats.returnDamage;
            slowDuration = stats.slowDuration;
            lifesteal = stats.lifesteal;
            chanceToJump = stats.chanceToJump;
            chanceToCleanse = stats.chanceToCleanse;
            chanceToEthereal = stats.chanceToEthereal;
            etherealDuration = stats.etherealDuration;
            
            slowsIncoming = stats.slowsIncoming;
            speedReduction = stats.speedReduction;
        }
    }
}



[RequireComponent (typeof (Rigidbody))]
public sealed class EquipmentTypePlating : BaseEquipment 
{    
    [SerializeField]    PlatingProperties   m_baseStats     = null;                     //!< The base stats of the plating.
    [SerializeField]    PlatingProperties   m_currentStats  = new PlatingProperties();  //!< The current stats of the plating.

    [SerializeField]    int                 m_currentHP     = 0;                        //!< The current amount of HP in the plating.
                        float               m_regenFloatCatch = 0.0f;
                        
    Element m_cachedMajorElement = Element.NULL;
    
    //Internals
    float m_cleanseCounter = 0.0f;
    float m_arcCounter = 0.0f;


    #region BaseEquipment Overrides

    protected override void ResetToBaseStats ()
    {
        m_currentStats.CloneProperties (m_baseStats);
    }


    protected override void CalculateCurrentStats ()
    {
        for (int i = 0; i < m_augmentSlots.Length; i++)
        {
            if (m_augmentSlots[i] != null)
            {
                float scalar = ElementalValuesPlating.TierScalar.GetScalar (m_augmentSlots[i].GetTier());
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
            }
        }
        
        m_currentHP = m_currentStats.hp;
        rigidbody.mass = m_currentStats.mass;
    }
    #endregion

    #region HP Value Interaction
    void Start()
    {
        m_currentHP = m_baseStats.hp;
    }
    
    void Update()
    {
        //Regen
        if(m_currentStats.regen > 0 && m_currentHP < m_currentStats.hp)
        {
            //float amount = m_currentStats.regen * (float)m_currentStats.hp * Time.deltaTime;
            float amount = m_currentStats.regen * Time.deltaTime;
            int hpIncr = (int)amount;
            float flCatch = amount -= hpIncr;
            
            m_regenFloatCatch += flCatch;
            
            if((int)m_regenFloatCatch > 0)
            {
                int incr = (int)m_regenFloatCatch;
                m_regenFloatCatch -= incr;
                hpIncr += incr;
            }
            
            m_currentHP += hpIncr;
            if(m_currentHP > m_currentStats.hp)
                m_currentHP = m_currentStats.hp;
        }
        
        
        if(this.GetComponent<Ship>() != null)
        {
            //Cleanse
            if(m_cleanseCounter >= 1.0f)
            {
                m_cleanseCounter = 0.0f;
                float rand = Random.Range(0.0f, 1.0f);
                if(rand < m_currentStats.chanceToCleanse)
                {
                    GetComponent<Ship>().ResetDebuffs();
                }
            }
            else
            {
                m_cleanseCounter += Time.deltaTime;
            }
            
            //Arc
            if(m_arcCounter >= 1.0f)
            {
                List<Debuff> debuffs = this.GetComponent<Ship>().GetListOfCurrentDebuffs();
                
                if(debuffs != null && debuffs.Count > 0)
                {
                    if(m_currentStats.chanceToJump > 0.0f)
                    {
                        m_arcCounter = 0.0f;
                        float rand = Random.Range(0.0f, 1.0f);
                        if(rand < m_currentStats.chanceToJump)
                        {
                            FireArcBolt(debuffs);
                        }
                    }
                }
            }
            else
            {
                m_arcCounter += Time.deltaTime;
            }
        }
    }
    
    void FireArcBolt(List<Debuff> debuffsToApply)
    {
        int layermask = Layers.GetLayerMask(gameObject.layer, MaskType.Targetting);
        Rigidbody[] targets = Physics.OverlapSphere(transform.position, 7.5f, layermask).GetAttachedRigidbodies();
        
        if(targets.Length > 0)
        {
            int rand = Random.Range(0, targets.Length);
            
            GameObject target = targets[rand].gameObject;
            for(int i = 0; i < debuffsToApply.Count; i++)
            {
                target.GetComponent<Ship>().AddDebuff(debuffsToApply[i]);
            }
            
            //Spawn the effect
            GameObject.FindGameObjectWithTag("EffectManager").GetComponent<EffectsManager>().SpawnLightningEffect(transform.position, target.transform.position, LightningZapType.Small);
        }
    }
    
    /// <summary>
    /// Damages the plating.
    /// </summary>
    /// <returns><c>true</c>, if the ship is still alive, <c>false</c> otherwise.</returns>
    /// <param name="damage">Damage dealt to plating.</param>
    public bool DamagePlating(int damage, GameObject firer, GameObject hitter = null)
    {
        m_currentHP -= damage;
        networkView.RPC ("PropagateHealthValue", RPCMode.Others, m_currentHP);
        
        if(m_currentHP <= 0)
        {
            m_currentHP = 0;
            GetComponent<HealthScript>().OnMobDies(firer, hitter);
            return false;
        }
        else
        {
            float rand = Random.Range(0.0f, 1.0f);
            if(rand < m_currentStats.chanceToEthereal)
            {
                GetComponent<Ship>().AddDebuff(new DebuffEthereal(m_currentStats.etherealDuration, this.gameObject));
            }
        
            return true;
        }
    }
    public void RepairPlating(int repair)
    {
        m_currentHP += repair;
        
        if(m_currentHP > m_currentStats.hp)
            m_currentHP = m_currentStats.hp;
        
        networkView.RPC ("PropagateHealthValue", RPCMode.Others, m_currentHP);
    }
    
    public float GetHealthPercentage()
    {
        return (float)m_currentHP / (float)m_currentStats.hp;
    }
    public int GetHealthCurrent()
    {
        return m_currentHP;
    }
    public int GetHealthMax()
    {
        return m_currentStats.hp;
    }
    public int GetDamageToReturn(int damageDoneToUs)
    {
        return Mathf.RoundToInt(damageDoneToUs * m_currentStats.returnDamage);
    }
    public bool CanGravityWell()
    {
        return m_currentStats.slowsIncoming;
    }
    public float GetGravityWellMagnitude()
    {
        return m_currentStats.speedReduction;
    }
    public float GetVampirePercentage()
    {
        return m_currentStats.lifesteal;
    }
    public float GetSlowOnRam()
    {
        return m_currentStats.slowDuration;
    }
    
    [RPC] void PropagateHealthValue(int value)
    {
        m_currentHP = value;
    }
    
    public Element GetMajorityElement()
    {
        if(m_cachedMajorElement == Element.NULL)
            DetermineMajorityElement();
            
        return m_cachedMajorElement;
    }
    Element DetermineMajorityElement()
    {
        int[] counter = new int[10];
        
        for(int i = 0; i < m_augmentSlots.Length; i++)
        {
            if(m_augmentSlots[i] != null)
            {
                switch(m_augmentSlots[i].GetElement())
                {
                case Element.Fire:
                {
                    counter[0] += m_augmentSlots[i].GetTier();
                    break;
                }
                case Element.Ice:
                {
                    counter[1] += m_augmentSlots[i].GetTier();
                    break;
                }
                case Element.Earth:
                {
                    counter[2] += m_augmentSlots[i].GetTier();
                    break;
                }
                case Element.Lightning:
                {
                    counter[3] += m_augmentSlots[i].GetTier();
                    break;
                }
                case Element.Light:
                {
                    counter[4] += m_augmentSlots[i].GetTier();
                    break;
                }
                case Element.Dark:
                {
                    counter[5] += m_augmentSlots[i].GetTier();
                    break;
                }
                case Element.Spirit:
                {
                    counter[6] += m_augmentSlots[i].GetTier();
                    break;
                }
                case Element.Gravity:
                {
                    counter[7] += m_augmentSlots[i].GetTier();
                    break;
                }
                case Element.Air:
                {
                    counter[8] += m_augmentSlots[i].GetTier();
                    break;
                }
                case Element.Organic:
                {
                    counter[9] += m_augmentSlots[i].GetTier();
                    break;
                }
                }
            }
        }
        
        int highestID = -1;
        int highestVal = 0;
        
        for(int i = 0; i < counter.Length; i++)
        {
            if(counter[i] > highestVal)
            {
                highestVal = counter[i];
                highestID = i;
            }
        }
        
        switch(highestID)
        {
        case 0:
        {
            m_cachedMajorElement = Element.Fire;
            return Element.Fire;
        }
        case 1:
        {
            m_cachedMajorElement = Element.Ice;
            return Element.Ice;
        }
        case 2:
        {
            m_cachedMajorElement = Element.Earth;
            return Element.Earth;
        }
        case 3:
        {
            m_cachedMajorElement = Element.Lightning;
            return Element.Lightning;
        }
        case 4:
        {
            m_cachedMajorElement = Element.Light;
            return Element.Light;
        }
        case 5:
        {
            m_cachedMajorElement = Element.Dark;
            return Element.Dark;
        }
        case 6:
        {
            m_cachedMajorElement = Element.Spirit;
            return Element.Spirit;
        }
        case 7:
        {
            m_cachedMajorElement = Element.Gravity;
            return Element.Gravity;
        }
        case 8:
        {
            m_cachedMajorElement = Element.Air;
            return Element.Air;
        }
        case 9:
        {
            m_cachedMajorElement = Element.Organic;
            return Element.Organic;
        }
        default:
        {
            m_cachedMajorElement = Element.NULL;
            return Element.NULL;
        }
        }
    }
    #endregion

    #region ElementResponses
    protected override void ElementResponseFire (float scalar)
    {
        m_currentStats.returnDamage         += ElementalValuesPlating.Fire.returnDamageInc * scalar;
    }


    protected override void ElementResponseIce (float scalar)
    {
        m_currentStats.slowDuration         += ElementalValuesPlating.Ice.slowDurationInc * scalar;
    }
       

    protected override void ElementResponseEarth (float scalar)
    {
        m_currentStats.hp                   += (int) (m_baseStats.hp * ElementalValuesPlating.Earth.hpMulti * scalar);
        m_currentStats.mass                 += m_baseStats.mass * ElementalValuesPlating.Earth.massMulti * scalar;
    }


    protected override void ElementResponseLightning (float scalar)
    {
        m_currentStats.chanceToJump         += ElementalValuesPlating.Lightning.chanceToJumpInc * scalar;
    }


    protected override void ElementResponseLight (float scalar)
    {
        m_currentStats.chanceToCleanse      += ElementalValuesPlating.Light.chanceToCleanseInc * scalar;
    }


    protected override void ElementResponseDark (float scalar)
    {
        m_currentStats.lifesteal            += ElementalValuesPlating.Dark.lifestealInc * scalar;
    }


    protected override void ElementResponseSpirit (float scalar)
    {
        m_currentStats.chanceToEthereal     += ElementalValuesPlating.Spirit.chanceToEtherealInc * scalar;
        m_currentStats.etherealDuration     += m_baseStats.etherealDuration * ElementalValuesPlating.Spirit.etherealDurationMulti * scalar;
    }


    protected override void ElementResponseGravity (float scalar)
    {
        m_currentStats.slowsIncoming        = ElementalValuesPlating.Gravity.slowsIncoming;

        m_currentStats.speedReduction       += ElementalValuesPlating.Gravity.speedReductionInc * scalar;
    }


    protected override void ElementResponseAir (float scalar)
    {
        m_currentStats.mass                 += m_baseStats.mass * ElementalValuesPlating.Air.massMulti * scalar;
    }


    protected override void ElementResponseOrganic (float scalar)
    {
        m_currentStats.regen                += ElementalValuesPlating.Organic.regenInc * scalar;
    }
    #endregion
}

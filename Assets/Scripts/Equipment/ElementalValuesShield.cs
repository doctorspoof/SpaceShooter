using UnityEngine;



namespace ElementalValuesShield
{
    /// <summary>
    /// Fire should enable a fire burst effect on collapse which will cause AoE damage to nearby enemies.
    /// </summary>
    public static class Fire
    {
        public const bool   shouldFireBurst             = true;     //!< Enable shield collapse damage
        
        public const float  baseRechargeDelayMulti      = 0.1f,     //!< Make the shield delay regeneration further.
        
                            burstDamageMulti            = 0.1f,     //!< Increase the damage of the burst effect.
                            burstRangeMulti             = 0.1f,     //!< Extend the range of the burst effect.
                            burstMaxDamageRangeMulti    = 0.1f,     //!< Extend the range at which enemies will receive maximum damage with no falloff.
                            burstMaxFalloffInc          = 0.1f;     //!< Increment the max falloff for the damage.
    }
    
    
    /// <summary>
    /// Ice should reduce the duration of debuffs whilst providing some more minor buffs.
    /// </summary>
    public static class Ice
    {
        public const float  baseShieldMulti             = 0.1f,     //!< Increase the shield capacity slightly.
                            
                            debuffModifierInc           = 0.1f;     //!< Increase the debuff reduction.
    }
    
    
    /// <summary>
    /// The primary purpose of Earth is to increase the capacity of the shield to make it more tanky.
    /// </summary>
    public static class Earth
    {
        public const float  baseShieldMulti             = 0.2f,     //!< Increase the base shield significantly.
                            baseRechargeDelayMulti      = 0.1f;     //!< Increase the delay before recharging.
    }
    
    
    /// <summary>
    /// Lightning enables a passive zap effect to nearby enemies, causing them to take damage.
    /// </summary>
    public static class Lightning
    {
        public const bool   canStaticShock          = true;     //!< Enable shock functionality.

        public const float  baseShieldMulti         = -0.1f,    //!< Reduce the base shield slightly.
                            staticChanceInc         = 0.1f,     //!< Decrease base damage slightly.
                            staticRangeMutli        = 0.1f,     //!< Increase the range of the shock.
                            staticDamageMulti       = 0.1f,     //!< Increase the damage of the shock.
                            staticCooldownMulti     = -0.1f;    //!< Reduce the cooldown.
    }
    
    
    /// <summary>
    /// Light gives a chance to disable nearby enemies on shield collapse, giving the player time to retreat if necessary.
    /// </summary>
    public static class Light
    {
        public const bool   canFlashbang            = true;     //!< Enable disable functionality.
        
        public const float  baseRechargeDelayMulti  = 0.1f,     //!< Increase the time until the shield begins recharging.

                            flashChanceInc          = 0.1f,     //!< Increase the chance to flash enemies.
                            flashRangeMutli         = 0.1f,     //!< Increase the range of the flash.
                            flashStunDurationMulti  = 0.1f;     //!< Increase how long effected enemies are stunned for.
    }
    
    
    /// <summary>
    /// Dark enables absorbtion functionality of the shield.
    /// </summary>
    public static class Dark
    {
        public const float  baseShieldMulti         = -0.1f,    //!< Reduce the capacity of the shield.

                            absorbChanceInc         = 0.1f;     //!< Increase the chance of absorbing damage.
    }
    
    
    /// <summary>
    /// The Spirit elemental allows the player to turn invisible when their shield collapses, making it harder for them to be tracked.
    /// </summary>
    public static class Spirit
    {
        public const float  baseRechargeDelayMulti  = 0.1f,     //!< Increase the recharge delay.

                            invisChanceInc          = 0.1f,     //!< Increase the chance of the player going invisible.
                            invisDurationMulti      = 0.1f;     //!< Increase the duration of the invisibility.
    }
    
    
    /// <summary>
    /// Gravity gives a chance of incoming projectiles to be deflected when they get close.
    /// </summary>
    public static class Gravity
    {
        public const float  deflectChanceInc        = 0.1f;     //!< Increase the chance of deflection.
    }
    
    
    /// <summary>
    /// Air reduces the recharge delay slightly for a small buff.
    /// </summary>
    public static class Air
    {
        public const float  baseRechargeDelayMulti  = -0.1f;    //!< Reduce the recharge delay.
    }
    
    
    /// <summary>
    /// Organic increase the recharge rate of the shield.
    /// </summary>
    public static class Organic
    {
        public const float  baseRechargeRateMulti   = 0.1f;     //!< Increase how fast the shield can recharge.
    }
    
    
    public static class TierScalar
    {
        public const float  tierOne                 = 1f,       //!< The scalar for tier one effects.
                            tierTwo                 = 2f,       //!< The scalar for tier two effects.
                            tierThree               = 3f,       //!< The scalar for tier three effects.
                            tierFour                = 4f,       //!< The scalar for tier four effects.
                            tierFive                = 5f;       //!< The scalar for tier five effects.
        
        
        public static float GetScalar (int tier)
        {
            switch (tier)
            {
                case 1: return tierOne;
                case 2: return tierTwo;                
                case 3: return tierThree;                
                case 4: return tierFour;
                case 5: return tierFive;
                    
                default: 
                    Debug.LogError ("Couldn't find the corresponding scalar value.");
                    return 0f;
            }
        }
    }
}
using UnityEngine;



namespace ElementalValuesWeapon
{
	/// <summary>
	/// The Fire elemental should cause an AoE effect on a weapon and increase the effectiveness of that AoE damage when stacked.
	/// </summary>
	public static class Fire
	{
		public const bool   isAOE                   = true;     //!< Ensure the bullet gets set to be AoE.
		
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
	public static class Ice
	{
		public const float  damageMulti             = 0.1f,     //!< Increase the base bullet damage.
							reloadTimeMulti         = -0.1f,    //!< Decrease the reload time.
							slowDurationInc         = 0.5f;     //!< Increment the slow effect of the bullet.
	}
	
	
	/// <summary>
	/// The Earth elemental should cause the bullet to act more like a cannon, high burst damage, long recovery and moderate speed.
	/// </summary>
	public static class Earth
	{
		public const float  damageMulti             = 0.2f,     //!< Increase the base damage significantly.
							reloadTimeMulti         = 0.2f,     //!< Increase the reload time (negative)
							reachMulti              = 0.1f,     //!< Increase the max distance of the bullet.
							lifetimeMulti           = -0.1f;    //!< Decrease the lifetime (increasing the speed).
	}
	
	
	/// <summary>
	/// The lightning elemental should cause the bullet to jump between targets on hit, sharing damage with a nearby foe.
	/// </summary>
	public static class Lightning
	{
		public const float  damageMulti             = -0.1f,     //!< Decrease base damage slightly.
							chanceToJumpInc         = 0.1f;      //!< Add to the percentage chance.
	}
	
	
	/// <summary>
	/// Light should enable beam functionality but also give a buff to damage but increase the reload time.
	/// </summary>
	public static class Light
	{
		public const bool   isBeam                  = true;     //!< Enable beam functionality
		
		public const float  damageMulti             = 0.1f,     //!< Increase base bullet damage slightly.
							reloadTimeMulti         = 0.1f;     //!< Increase the reload time (negative).
	}
	
	
	/// <summary>
	/// Dark gives a chance on hit to disable targets.
	/// </summary>
	public static class Dark
	{
		public const float  damageMulti             = 0.1f,     //!< Increase the base damage of the bullet.
							chanceToDisableInc      = 0.1f,     //!< Increment the chance to disable targets.
							disableDurationInc      = 0.1f;     //!< Incrememnt the duration of the disable effect.
	}
	
	
	/// <summary>
	/// The Spirit element is all about piercing through targets in an etherial manner.
	/// </summary>
	public static class Spirit
	{
		public const bool   isPiercing              = true;     //!< Enable piercing functionality on bullets.
		
		public const float  damageMulti             = 0.1f,     //!< Increase zee base damage captain!
							maxPiercingsMulti       = 0.5f,     //!< Increase the number of piercings possible.
							piercingModifierInc     = 0.1f;     //!< Reduce the amount piercing reducues damage and speed.                                                        
	}
	
	
	/// <summary>
	/// Gravity enables and increases homing functionality of a bullet.
	/// </summary>
	public static class Gravity
	{
		public const bool   isHoming                = true;     //!< Enable homing functionality on the bullet.
		
		public const float  damageMulti             = 0.1f,     //!< Increase damage lulz.
							homingRangeMulti        = 0.1f,     //!< Increase the homing range.
							homingTurnRateMulti     = 0.1f;     //!< Increase the speed at which the bullet can turn.
	}
	
	
	/// <summary>
	/// Air is more of a helper element, it reduces cooldown and increases the reach of the bullet to make up for other elements.
	/// </summary>
	public static class Air
	{
		public const float  reloadTimeMulti         = -0.1f,    //!< Reduce the reload time of the weapon.
							reachMulti              = 0.1f;     //!< Increase the range of the bullet.
	}
	
	
	/// <summary>
	/// Organic enables and enhances damage-over-time functionality based on the bullets base damage.
	/// </summary>
	public static class Organic
	{
		public const float  dotDurationInc          = 0.1f,     //!< Increment the duration DoT is applied.
							dotEffectInc            = 0.1f;     //!< Increment the percentage of the base damage to be applied over time.
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
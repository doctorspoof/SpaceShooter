using UnityEngine;



namespace ElementalValuesPlating
{
	/// <summary>
	/// The Fire elemental adds a "Thorns" effect to the plating which will cause damage to splash back onto anything that rams it.
	/// </summary>
	public static class Fire
	{
		public const float  returnDamageInc         = 0.1f;   	//!< Increase how much damage gets returned to the ramming enemy.
	}
	
	
	/// <summary>
	/// Ice causes the ramming enemy to be slowed upon contact. Additional Ice increases the duration.
	/// </summary>
	public static class Ice
	{
		public const float  slowDurationInc 		= 0.1f;     //!< Increase how long the enemy is slowed for.
	}
	
	
	/// <summary>
	/// The Earth elemental increases the thickness of the plating, increasing HP but reduce speed by increasing mass.
	/// </summary>
	public static class Earth
	{
		public const float  hpMulti             	= 0.2f,     //!< Increase the base HP of the plating.
							massMulti         		= 0.1f;     //!< Increase the mass so speed is reduced.
	}
	
	
	/// <summary>
	/// Lightning causes debuffs to transfer to nearby enemies. The effectiveness is governed by how much Lightning is applied to the plating.
	/// </summary>
	public static class Lightning
	{
		public const float  chanceToJumpInc         = 0.1f;     //!< Increase the chance that debuffs will jump to an enemy nearby.
	}
	
	
	/// <summary>
	/// Light adds a passive effect which removes all debuffs itself.
	/// </summary>
	public static class Light
	{
		public const float  chanceToCleanseInc		= 0.1f;     //!< Increase the chance to passively cleanse self.
	}
	
	
	/// <summary>
	/// Dark provides a lifesteal effect on the ramming of enemies.
	/// </summary>
	public static class Dark
	{
		public const float  lifestealInc           	= 0.1f;     //!< Increase the amount of damage stolen.
	}
	
	
	/// <summary>
	/// The Spirit element is all about piercing through targets in an etherial manner.
	/// </summary>
	public static class Spirit
	{
		public const float  chanceToEtherealInc		= 0.1f,     //!< Increase the chance to turn ethereal.
							etherealDurationMulti   = 0.1f;		//!< Increase the duration of the ethereal effect.                                            
	}
	
	
	/// <summary>
	/// Gravity slows the approach of nearby objects which can make it easier to escape them.
	/// </summary>
	public static class Gravity
	{
		public const bool	slowsIncoming			= true;		//!< Enable gravitational effect on incoming projectiles.

		public const float  speedReductionInc       = 0.1f;     //!< How much to reduce incoming projectiles speed by.
	}
	
	
	/// <summary>
	/// Air makes the plating lighter, reducing it's defensive capacity but allowing for more speed.
	/// </summary>
	public static class Air
	{
		public const float  massMulti         		= -0.1f;    //!< Reduce the mass of the ship.
	}
	
	
	/// <summary>
	/// A slight regeneration effect is provided by the Organic elemental.
	/// </summary>
	public static class Organic
	{
		public const float  regenMulti          	= 0.05f;    //!< How much HP to regenerate a second.
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
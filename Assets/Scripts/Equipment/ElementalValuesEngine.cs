using UnityEngine;



namespace ElementalValuesEngine
{
	/// <summary>
	/// The Fire elemental should increase baes speed at the cost of turning and strafing ability, this is act as a drag-like enhancement.
	/// </summary>
    public static class Fire
    {
        public const float  speedMulti             	= 0.2f,    	//!< Increase the engines base speed.
                            turnMulti               = -0.1f,    //!< Decrease ability to turn.
                            strafeMulti           	= -0.1f;    //!< Decrease ability to strafe.
    }
	
	
	/// <summary>
	/// Ice element increases the afterburner capacity by cooling the engines.
	/// </summary>
    public static class Ice
    {
		public const float  burnerLengthMulti       = 0.1f;		//!< Increase the available capacity.
    }
	
	
	/// <summary>
	/// Earth acts as a huge capacity upgrade at the cost of extra weight, aka lower speed.
	/// </summary>
    public static class Earth
    {
        public const float  burnerSpeedMulti        = -0.1f;    //!< Decrease the afterburners max speed.
                            burnerLengthMulti       = 0.2f,     //!< Significantly increase afterburner capacity.
    }
	
	
	/// <summary>
	/// Lightning should concentrate on increasing the afterburner speed at the cost of capacity.
	/// </summary>
    public static class Lightning
    {
        public const float  burnerSpeedMulti       	= 0.2f,    	//!< Make the engine go zoom zoom.
                            burnerLengthMulti       = -0.1f;    //!< Decrease the capacity to balance the effect.
    }
	
	
	/// <summary>
	/// Light should increase the core speed of the engine in every aspect but ultimately reduce max afterburner speed.
	/// </summary>
    public static class Light
    {
        public const float  speedMulti           	= 0.1f,     //!< Increase the engines core speed.
                            turnMulti 	        	= 0.1f,     //!< Make the engine allow for faster turning.
                            strafeMulti				= 0.1f,		//!< Increase the agility of the engine.
                            burnerSpeedMulti		= -0.1f;	//!< Decrease the speed of the afterburner.
    }
	
	
	/// <summary>
	/// Dark allows for the usage of a random long-range teleport. Increased Dark element will increase the range and reduce the cooldown.
	/// </summary>
    public static class Dark
    {
        public const bool   longTeleport			= true;		//!< Allow for the usage of the long-range teleport.

        public const float  longRangeMulti          = 0.1f,     //!< Increase the distance of the long-range teleport.
                            longCooldownMulti 	    = -0.1f;   	//!< Decrease the cooldown of the long-range teleport.
    }
	
	
	/// <summary>
	/// The Spirit element allows for a controlled short-range teleport. Increased Spirit element effects the range and cooldown.
	/// </summary>
    public static class Spirit
    {
        public const bool   shortTeleport        	= true;     //!< Enable the short-range teleport ability.

        public const float  shortRangeMulti         = 0.1f,     //!< Increase the range of the teleport.
                            shortCooldownMulti      = -0.1f;    //!< Reduce the cooldown of the teleport.                                                     
    }
	
	
	/// <summary>
	/// With the Gravity element the engines can control how much planetary gravity effects them.
	/// </summary>
    public static class Gravity
    {
        public const bool   gravityControl         	= true;     //!< Enable gravity control.

        public const float  maxGravityChangeInc   	= 0.1f ;    //!< Increase the extra gravity that can be applied to the engines.
    }
	
	
	/// <summary>
	/// Air increases the agility of the ship by a small amount with no negatives.
	/// </summary>
    public static class Air
    {
        public const float  turnMulti         		= 0.1f,    	//!< Increase the turn rate of the engines.
                            strafeMulti             = 0.1f;     //!< Increase the agility of the engines.
    }
	
	
	/// <summary>
	/// Organic is all about increasing the recharge rate of the afterburners.
	/// </summary>
    public static class Organic
    {
        public const float  burnerRechargeMulti   	= 0.1f;   	//!< Increase the recharge rate of the afterburners.
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
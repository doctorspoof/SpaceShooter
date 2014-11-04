using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public static class ItemDescriptionHolder 
{
	static Dictionary<int, string> m_descriptionID = new Dictionary<int, string>()
	{
        {0, "Type: Fire\r\nTier: 1\r\nWeapon: Bullets explode on contact\r\nShield: Explosive pulse when shield collapses\r\nPlating: Burn enemies in physical contact\r\nEngine: Increased straight-line speed"},
        {1, "Type: Ice\r\nTier: 1\r\nWeapon: Slowing effect on targets\r\nShield: Reduced duration of debuffs\r\nPlating: Slowing effect on enemies in physical contact\r\nEngine: Increased afterburner capacity"},
        {2, "Type: Earth\r\nTier: 1\r\nWeapon: Heavier, more damaging shots\r\nShield: Increased capacity, reduced recharge\r\nPlating: Additional armour, heavier weight\r\nEngine: Greatly increased afterburner capacity, slower afterburner speed"},
        {3, "Type: Lightning\r\nTier: 1\r\nWeapon: Shots have a chance to jump to other nearby targets\r\nShield: Chance to zap nearby enemies while shield is up\r\nPlating: Chance to apply your debuffs to nearby enemies\r\nEngine: Greatly increased afterburner speed, shorter afterburner capacity"},
        {4, "Type: Light\r\nTier: 1\r\nWeapon: Convert shots into a laser-esque beam\r\nShield: Chance to release a disabling flash on shield collapse\r\nPlating: Chance to heal self of debuffs\r\nEngine: Reduced afterburner effectiveness for increased engine stats"},
        {5, "Type: Dark\r\nTier: 1\r\nWeapon: Shots have a chance to disable targets\r\nShield: Chance to absorb hits into shield charge\r\nPlating: Chance to drain health on collision\r\nEngine: Grants 'Emergency Teleport' ability"},
        {6, "Type: Spirit\r\nTier: 1\r\nWeapon: Shots pierce targets\r\nShield: Chance to go invisible on shield collapse\r\nPlating: Chance to 'fade out' on hit\r\nEngine: Grants 'Blink' ability"},
        {7, "Type: Gravity\r\nTier: 1\r\nWeapon: Bullets will home in on targets\r\nShield: Chance to deflect incoming fire\r\nPlating: Adds a repelling effect, slowing down incoming fire\r\nEngine: Grants ability to control how much gravity affects you"},
        {8, "Type: Air\r\nTier: 1\r\nWeapon: Faster firing, but weaker shots\r\nShield: Reduced shield recharge delay\r\nPlating: Lighter weight armour\r\nEngine: Increased agility on engines"},
        {9, "Type: Organic\r\nTier: 1\r\nWeapon: Bullets apply a corrosive effect\r\nShield: Faster recharge rate\r\nPlating: Slow health regeneration\r\nEngine: Faster afterburner recharge"},
        {10, "Type: Fire\r\nTier: 2\r\nWeapon: Bullets explode on contact\r\nShield: Explosive pulse when shield collapses\r\nPlating: Burn enemies in physical contact\r\nEngine: Increased straight-line speed"},
        {11, "Type: Ice\r\nTier: 2\r\nWeapon: Slowing effect on targets\r\nShield: Reduced duration of debuffs\r\nPlating: Slowing effect on enemies in physical contact\r\nEngine: Increased afterburner capacity"},
        {12, "Type: Earth\r\nTier: 2\r\nWeapon: Heavier, more damaging shots\r\nShield: Increased capacity, reduced recharge\r\nPlating: Additional armour, heavier weight\r\nEngine: Greatly increased afterburner capacity, slower afterburner speed"},
        {13, "Type: Lightning\r\nTier: 2\r\nWeapon: Shots have a chance to jump to other nearby targets\r\nShield: Chance to zap nearby enemies while shield is up\r\nPlating: Chance to apply your debuffs to nearby enemies\r\nEngine: Greatly increased afterburner speed, shorter afterburner capacity"},
        {14, "Type: Light\r\nTier: 2\r\nWeapon: Convert shots into a laser-esque beam\r\nShield: Chance to release a disabling flash on shield collapse\r\nPlating: Chance to heal self of debuffs\r\nEngine: Reduced afterburner effectiveness for increased engine stats"},
        {15, "Type: Dark\r\nTier: 2\r\nWeapon: Shots have a chance to disable targets\r\nShield: Chance to absorb hits into shield charge\r\nPlating: Chance to drain health on collision\r\nEngine: Grants 'Emergency Teleport' ability"},
        {16, "Type: Spirit\r\nTier: 2\r\nWeapon: Shots pierce targets\r\nShield: Chance to go invisible on shield collapse\r\nPlating: Chance to 'fade out' on hit\r\nEngine: Grants 'Blink' ability"},
        {17, "Type: Gravity\r\nTier: 2\r\nWeapon: Bullets will home in on targets\r\nShield: Chance to deflect incoming fire\r\nPlating: Adds a repelling effect, slowing down incoming fire\r\nEngine: Grants ability to control how much gravity affects you"},
        {18, "Type: Air\r\nTier: 2\r\nWeapon: Faster firing, but weaker shots\r\nShield: Reduced shield recharge delay\r\nPlating: Lighter weight armour\r\nEngine: Increased agility on engines"},
        {19, "Type: Organic\r\nTier: 2\r\nWeapon: Bullets apply a corrosive effect\r\nShield: Faster recharge rate\r\nPlating: Slow health regeneration\r\nEngine: Faster afterburner recharge"},
        {20, "Type: Fire\r\nTier: 3\r\nWeapon: Bullets explode on contact\r\nShield: Explosive pulse when shield collapses\r\nPlating: Burn enemies in physical contact\r\nEngine: Increased straight-line speed"},
        {21, "Type: Ice\r\nTier: 3\r\nWeapon: Slowing effect on targets\r\nShield: Reduced duration of debuffs\r\nPlating: Slowing effect on enemies in physical contact\r\nEngine: Increased afterburner capacity"},
        {22, "Type: Earth\r\nTier: 3\r\nWeapon: Heavier, more damaging shots\r\nShield: Increased capacity, reduced recharge\r\nPlating: Additional armour, heavier weight\r\nEngine: Greatly increased afterburner capacity, slower afterburner speed"},
        {23, "Type: Lightning\r\nTier: 3\r\nWeapon: Shots have a chance to jump to other nearby targets\r\nShield: Chance to zap nearby enemies while shield is up\r\nPlating: Chance to apply your debuffs to nearby enemies\r\nEngine: Greatly increased afterburner speed, shorter afterburner capacity"},
        {24, "Type: Light\r\nTier: 3\r\nWeapon: Convert shots into a laser-esque beam\r\nShield: Chance to release a disabling flash on shield collapse\r\nPlating: Chance to heal self of debuffs\r\nEngine: Reduced afterburner effectiveness for increased engine stats"},
        {25, "Type: Dark\r\nTier: 3\r\nWeapon: Shots have a chance to disable targets\r\nShield: Chance to absorb hits into shield charge\r\nPlating: Chance to drain health on collision\r\nEngine: Grants 'Emergency Teleport' ability"},
        {26, "Type: Spirit\r\nTier: 3\r\nWeapon: Shots pierce targets\r\nShield: Chance to go invisible on shield collapse\r\nPlating: Chance to 'fade out' on hit\r\nEngine: Grants 'Blink' ability"},
        {27, "Type: Gravity\r\nTier: 3\r\nWeapon: Bullets will home in on targets\r\nShield: Chance to deflect incoming fire\r\nPlating: Adds a repelling effect, slowing down incoming fire\r\nEngine: Grants ability to control how much gravity affects you"},
        {28, "Type: Air\r\nTier: 3\r\nWeapon: Faster firing, but weaker shots\r\nShield: Reduced shield recharge delay\r\nPlating: Lighter weight armour\r\nEngine: Increased agility on engines"},
        {29, "Type: Organic\r\nTier: 3\r\nWeapon: Bullets apply a corrosive effect\r\nShield: Faster recharge rate\r\nPlating: Slow health regeneration\r\nEngine: Faster afterburner recharge"},
        {30, "Type: Fire\r\nTier: 4\r\nWeapon: Bullets explode on contact\r\nShield: Explosive pulse when shield collapses\r\nPlating: Burn enemies in physical contact\r\nEngine: Increased straight-line speed"},
        {31, "Type: Ice\r\nTier: 4\r\nWeapon: Slowing effect on targets\r\nShield: Reduced duration of debuffs\r\nPlating: Slowing effect on enemies in physical contact\r\nEngine: Increased afterburner capacity"},
        {32, "Type: Earth\r\nTier: 4\r\nWeapon: Heavier, more damaging shots\r\nShield: Increased capacity, reduced recharge\r\nPlating: Additional armour, heavier weight\r\nEngine: Greatly increased afterburner capacity, slower afterburner speed"},
        {33, "Type: Lightning\r\nTier: 4\r\nWeapon: Shots have a chance to jump to other nearby targets\r\nShield: Chance to zap nearby enemies while shield is up\r\nPlating: Chance to apply your debuffs to nearby enemies\r\nEngine: Greatly increased afterburner speed, shorter afterburner capacity"},
        {34, "Type: Light\r\nTier: 4\r\nWeapon: Convert shots into a laser-esque beam\r\nShield: Chance to release a disabling flash on shield collapse\r\nPlating: Chance to heal self of debuffs\r\nEngine: Reduced afterburner effectiveness for increased engine stats"},
        {35, "Type: Dark\r\nTier: 4\r\nWeapon: Shots have a chance to disable targets\r\nShield: Chance to absorb hits into shield charge\r\nPlating: Chance to drain health on collision\r\nEngine: Grants 'Emergency Teleport' ability"},
        {36, "Type: Spirit\r\nTier: 4\r\nWeapon: Shots pierce targets\r\nShield: Chance to go invisible on shield collapse\r\nPlating: Chance to 'fade out' on hit\r\nEngine: Grants 'Blink' ability"},
        {37, "Type: Gravity\r\nTier: 4\r\nWeapon: Bullets will home in on targets\r\nShield: Chance to deflect incoming fire\r\nPlating: Adds a repelling effect, slowing down incoming fire\r\nEngine: Grants ability to control how much gravity affects you"},
        {38, "Type: Air\r\nTier: 4\r\nWeapon: Faster firing, but weaker shots\r\nShield: Reduced shield recharge delay\r\nPlating: Lighter weight armour\r\nEngine: Increased agility on engines"},
        {39, "Type: Organic\r\nTier: 4\r\nWeapon: Bullets apply a corrosive effect\r\nShield: Faster recharge rate\r\nPlating: Slow health regeneration\r\nEngine: Faster afterburner recharge"},
        {40, "Type: Fire\r\nTier: 5\r\nWeapon: Bullets explode on contact\r\nShield: Explosive pulse when shield collapses\r\nPlating: Burn enemies in physical contact\r\nEngine: Increased straight-line speed"},
        {41, "Type: Ice\r\nTier: 5\r\nWeapon: Slowing effect on targets\r\nShield: Reduced duration of debuffs\r\nPlating: Slowing effect on enemies in physical contact\r\nEngine: Increased afterburner capacity"},
        {42, "Type: Earth\r\nTier: 5\r\nWeapon: Heavier, more damaging shots\r\nShield: Increased capacity, reduced recharge\r\nPlating: Additional armour, heavier weight\r\nEngine: Greatly increased afterburner capacity, slower afterburner speed"},
        {43, "Type: Lightning\r\nTier: 5\r\nWeapon: Shots have a chance to jump to other nearby targets\r\nShield: Chance to zap nearby enemies while shield is up\r\nPlating: Chance to apply your debuffs to nearby enemies\r\nEngine: Greatly increased afterburner speed, shorter afterburner capacity"},
        {44, "Type: Light\r\nTier: 5\r\nWeapon: Convert shots into a laser-esque beam\r\nShield: Chance to release a disabling flash on shield collapse\r\nPlating: Chance to heal self of debuffs\r\nEngine: Reduced afterburner effectiveness for increased engine stats"},
        {45, "Type: Dark\r\nTier: 5\r\nWeapon: Shots have a chance to disable targets\r\nShield: Chance to absorb hits into shield charge\r\nPlating: Chance to drain health on collision\r\nEngine: Grants 'Emergency Teleport' ability"},
        {46, "Type: Spirit\r\nTier: 5\r\nWeapon: Shots pierce targets\r\nShield: Chance to go invisible on shield collapse\r\nPlating: Chance to 'fade out' on hit\r\nEngine: Grants 'Blink' ability"},
        {47, "Type: Gravity\r\nTier: 5\r\nWeapon: Bullets will home in on targets\r\nShield: Chance to deflect incoming fire\r\nPlating: Adds a repelling effect, slowing down incoming fire\r\nEngine: Grants ability to control how much gravity affects you"},
        {48, "Type: Air\r\nTier: 5\r\nWeapon: Faster firing, but weaker shots\r\nShield: Reduced shield recharge delay\r\nPlating: Lighter weight armour\r\nEngine: Increased agility on engines"},
        {49, "Type: Organic\r\nTier: 5\r\nWeapon: Bullets apply a corrosive effect\r\nShield: Faster recharge rate\r\nPlating: Slow health regeneration\r\nEngine: Faster afterburner recharge"}
	};

	public static string GetDescriptionFromID(int id)
	{
		string output = "";
		output = m_descriptionID[id];

		if(output == "MISSINGITEM")
		{
			return null;
		}
		else
		{
			return output;
		}
	}
}

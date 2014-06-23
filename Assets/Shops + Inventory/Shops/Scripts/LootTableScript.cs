using UnityEngine;
using System.Collections;

public class LootTableScript : MonoBehaviour 
{
	[SerializeField]
	GameObject m_ItemManager;

	[SerializeField]
	Item[] m_itemList;

	// Use this for initialization
	void Start () 
	{
		m_ItemManager = GameObject.FindGameObjectWithTag("ItemManager");

		if(m_ItemManager != null)
		{
			m_itemList = m_ItemManager.GetComponent<ItemIDHolder>().ItemList;
		}
	}
	
	// Update is called once per frame
	void Update () 
	{
	
	}

	public int[] RequestItemByApproximateValue(float value, bool[] stockFlags, float[] rarityMods, int numItemsReq)
	{
		//Bool array, order is:
		//Weapons - Shields - Engines - Plating - CWeapons

		//Initialise output array
		int[] output = new int[numItemsReq];

		for(int i = 0; i < numItemsReq; i++)
		{
			//First, decide which of the types of equipment we should get
			int type = -1;
			bool canContinue = false;
			while(!canContinue)
			{
				//TODO: Change this back to 0->5 to reallow CWeapons
				type = Random.Range(0, 4);

				if(stockFlags[type])
				{
					//If the selected type is one we can chose from, then continue!
					canContinue = true;
				}
			}

			//Now that we have an appropiate item type, lets cast it into an enum and work on it
			ItemType workingType = (ItemType)type;
			/*switch(workingType)
			{
				case ItemType.Weapon:
				{
					//Weapons run from 0->29
					float workingValue = value * rarityMods[type];
					Item temp = GetWeaponByValue(workingValue);
					output[i] = temp.itemID;
					break;
				}
				case ItemType.Shield:
				{
					//Shields run from 30->59
					float workingValue = value * rarityMods[type];
					Item temp = GetShieldByValue(workingValue);
					output[i] = temp.itemID;
					break;
				}
				case ItemType.Engine:
				{
					//Engines run from 60->89
					float workingValue = value * rarityMods[type];
					Item temp = GetEngineByValue(workingValue);
					output[i] = temp.itemID;
					break;
				}
				case ItemType.Plating:
				{
					//Platings run from 90->119
					float workingValue = value * rarityMods[type];
					Item temp = GetEngineByValue(workingValue);
					output[i] = temp.itemID;
					break;
				}
				case ItemType.CapitalWeapon:
				{
					//CWeapons run from 120->149
					//Nope
					break;
				}
			}*/

			int[] range = DetermineItemRange(workingType);
			float workingValue = value * rarityMods[type];
			Item temp = GetItemByValue(workingValue, range[0], range[1]);
			output[i] = temp.itemID;
		}

		return output;
	}

	int[] DetermineItemRange (ItemType type)
	{
		int[] range = new int[2] {-1, -1};
		switch (type)
		{
			// Weapons run from 0->29
		case ItemType.Weapon:
			range[0] = 0;
			range[1] = 29;
			break;
			
			// Shields run from 30->59
		case ItemType.Shield:
			range[0] = 30;
			range[1] = 59;
			break;
			
			// Engines run from 60->89
		case ItemType.Engine:
			range[0] = 60;
			range[1] = 89;
			break;
			
			// Plating runs from 90->119
		case ItemType.Plating:
			range[0] = 90;
			range[1] = 119;
			break;
			
		case ItemType.CapitalWeapon:
			range[0] = 120;
			range[1] = 130;
			break;
		}
		return range;
	}
	Item GetItemByValue (float value, int rangeMin, int rangeMax)
	{
		// Pre-condition: maxRange is infact the upper value
		if (rangeMax < rangeMin)
		{
			int temp = rangeMin;
			rangeMin = rangeMax;
			rangeMax = temp;
		}
		
		
		bool totalDone = false;
		int totalFailCounter = 0;
		while (!totalDone)
		{
			// Modify the value by random in here, so it's different per item
			float randDiff = Random.Range (0, value * 0.25f);

			bool hasFailed = false;
			int failCounter = 0;
			int id = -1;
			while (!hasFailed)
			{
				id = Random.Range (rangeMin, rangeMax);
				Item temp = m_itemList[id];
				
				if (temp != null && temp.itemObject != null)
				{
					int cost = temp.itemObject.GetComponent<ItemScript>().m_cost;
					if ((value - randDiff) <= cost && cost <= (value + randDiff))
					{
						// If the cost is appropriate, yippee!
						return temp;
					}
				}
				
				// Otherwise, repeat the random indexing
				++failCounter;
				if (failCounter > 10)
				{
					// Only allow 5 tries before restarting
					hasFailed = true;
				}
			}
			
			++totalFailCounter;
			if (totalFailCounter > 5)
			{
				//Only allow 5 tries before cancelling
				//If we still can't find an item, pick one randomly.
				
				//TODO: Change this!
				int randID = Random.Range (rangeMin, rangeMax);
				return m_itemList[randID];
			}
		}
		
		//Should never get here
		Debug.LogError ("Loot Table encountered catastrophic failure!");
		return null;
	}

	Item GetWeaponByValue(float value)
	{
		bool totalDone = false;
		int totalFailCounter = 0;
		while(!totalDone)
		{
			//Modify the value by random in here, so it's different per item
			float randDiff = Random.Range(0, value*0.25f);

			//Weapons run from 0->29
			bool hasFailed = false;
			int failCounter = 0;
			int id = -1;
			while(!hasFailed)
			{
				id = Random.Range(0, 30);
				Item temp = m_itemList[id];
				//Debug.Log ("Selected item with id# " + id);
				if(temp != null && temp.itemObject != null)
				{
					int cost = temp.itemObject.GetComponent<ItemScript>().m_cost;
					if((value - randDiff) <= cost && cost <= (value + randDiff))
					{
						//If the cost is appropriate, yippee!
						return temp;
					}
				}

				//Otherwise, repeat the random indexing
				++failCounter;
				if(failCounter > 10)
				{
					//Only allow 5 tries before restarting
					hasFailed = true;
				}
			}

			++totalFailCounter;
			if(totalFailCounter > 5)
			{
				//Only allow 5 tries before cancelling
				//If we still can't find an item, pick one randomly.

				//TODO: Change this!
				int randID = Random.Range(0, 30);
				return m_itemList[randID];
			}
		}

		//Should never get here
		Debug.LogError ("Loot Table encountered catastrophic failure!");
		return null;
	}

	Item GetShieldByValue(float value)
	{
		bool totalDone = false;
		int totalFailCounter = 0;
		while(!totalDone)
		{
			//Modify the value by random in here, so it's different per item
			float randDiff = Random.Range(0, value*0.25f);
			
			//Shields run from 30->59
			bool hasFailed = false;
			int failCounter = 0;
			int id = -1;
			while(!hasFailed)
			{
				id = Random.Range(30, 59);
				Item temp = m_itemList[id];
				//Debug.Log ("Selected item with id# " + id);
				if(temp != null && temp.itemObject != null)
				{
					int cost = temp.itemObject.GetComponent<ItemScript>().m_cost;
					if((value - randDiff) <= cost && cost <= (value + randDiff))
					{
						//If the cost is appropriate, yippee!
						return temp;
					}
				}
				
				//Otherwise, repeat the random indexing
				++failCounter;
				if(failCounter > 10)
				{
					//Only allow 5 tries before restarting
					hasFailed = true;
				}
			}
			
			++totalFailCounter;
			if(totalFailCounter > 5)
			{
				//Only allow 5 tries before cancelling
				//If we still can't find an item, pick one randomly.
				
				//TODO: Change this!
				int randID = Random.Range(30, 59);
				return m_itemList[randID];
			}
		}
		
		//Should never get here
		Debug.LogError ("Loot Table encountered catastrophic failure!");
		return null;
	}

	Item GetEngineByValue(float value)
	{
		bool totalDone = false;
		int totalFailCounter = 0;
		while(!totalDone)
		{
			//Modify the value by random in here, so it's different per item
			float randDiff = Random.Range(0, value*0.25f);
			
			//Engines run from 60->89
			bool hasFailed = false;
			int failCounter = 0;
			int id = -1;
			while(!hasFailed)
			{
				id = Random.Range(60, 89);
				Item temp = m_itemList[id];
				//Debug.Log ("Selected item with id# " + id);
				if(temp != null && temp.itemObject != null)
				{
					int cost = temp.itemObject.GetComponent<ItemScript>().m_cost;
					if((value - randDiff) <= cost && cost <= (value + randDiff))
					{
						//If the cost is appropriate, yippee!
						return temp;
					}
				}
				
				//Otherwise, repeat the random indexing
				++failCounter;
				if(failCounter > 10)
				{
					//Only allow 5 tries before restarting
					hasFailed = true;
				}
			}
			
			++totalFailCounter;
			if(totalFailCounter > 5)
			{
				//Only allow 5 tries before cancelling
				//If we still can't find an item, pick one randomly.
				
				//TODO: Change this!
				int randID = Random.Range(60, 89);
				return m_itemList[randID];
			}
		}
		
		//Should never get here
		Debug.LogError ("Loot Table encountered catastrophic failure!");
		return null;
	}

	Item GetPlatingByValue(float value)
	{
		bool totalDone = false;
		int totalFailCounter = 0;
		while(!totalDone)
		{
			//Modify the value by random in here, so it's different per item
			float randDiff = Random.Range(0, value*0.25f);
			
			//Engines run from 90->119
			bool hasFailed = false;
			int failCounter = 0;
			int id = -1;
			while(!hasFailed)
			{
				id = Random.Range(90, 119);
				Item temp = m_itemList[id];
				//Debug.Log ("Selected item with id# " + id);
				if(temp != null && temp.itemObject != null)
				{
					int cost = temp.itemObject.GetComponent<ItemScript>().m_cost;
					if((value - randDiff) <= cost && cost <= (value + randDiff))
					{
						//If the cost is appropriate, yippee!
						return temp;
					}
				}
				
				//Otherwise, repeat the random indexing
				++failCounter;
				if(failCounter > 10)
				{
					//Only allow 5 tries before restarting
					hasFailed = true;
				}
			}
			
			++totalFailCounter;
			if(totalFailCounter > 5)
			{
				//Only allow 5 tries before cancelling
				//If we still can't find an item, pick one randomly.
				
				//TODO: Change this!
				int randID = Random.Range(90, 119);
				return m_itemList[randID];
			}
		}
		
		//Should never get here
		Debug.LogError ("Loot Table encountered catastrophic failure!");
		return null;
	}
}
using UnityEngine;
using System.Collections;

public class ShopScript : MonoBehaviour 
{
	// Use ShopType to indicate whether the shop is a Shipyard or a standard shop
	public enum ShopType
	{
		Basic = 0,
		Shipyard = 1
	}
	

	[SerializeField] ShopType m_shopType = ShopType.Basic;			// The ShopType determines the functionality available

	public ShopType GetShopType()
	{
		return m_shopType;
	}

	[SerializeField]
	GameObject m_dockPoint;
	public Vector3 GetDockPoint()
	{
		if(m_dockPoint != null)
		{
			return m_dockPoint.transform.position;
		}
		else
			return this.transform.position;
	}

	[SerializeField]
	int m_shopSize;

	[SerializeField]
	GameObject[] m_shopInventory;
	public GameObject[] GetShopInventory()
	{
		return m_shopInventory;
	}
	public void RemoveItemFromShopInventory(int id)
	{
		m_shopInventory[id] = null;
		networkView.RPC ("PropagateInventoryRemoval", RPCMode.Others, id);
	}

	[RPC]
	void PropagateInventoryRemoval(int id)
	{
		m_shopInventory[id] = null;
	}

	[SerializeField]
	bool m_canStockWeapons;
	[SerializeField]
	float m_weaponRarityMod = 1.0f;
	[SerializeField]
	bool m_canStockShields;
	[SerializeField]
	float m_shieldRarityMod = 1.0f;
	[SerializeField]
	bool m_canStockEngines;
	[SerializeField]
	float m_engineRarityMod = 1.0f;
	[SerializeField]
	bool m_canStockPlating;
	[SerializeField]
	float m_platingRarityMod = 1.0f;
	[SerializeField]
	bool m_canStockCWeapons;
	[SerializeField]
	float m_cWeaponRarityMod = 1.0f;

	/// <summary>
	/// The percentage of base cost charged by this shop.
	/// Scale: 0.5f -> 1.5f
	/// </summary>
	
	public float m_pricePercent;

	// Use this for initialization
	void Start () 
	{
	
	}
	
	// Update is called once per frame
	void Update () 
	{
		this.transform.rotation = Quaternion.Euler(new Vector3(0, 0, this.transform.rotation.eulerAngles.z + (5.0f * Time.deltaTime)));
	}

	void OnSerializeNetworkView(BitStream stream, NetworkMessageInfo info)
	{
		float rotZ = this.transform.rotation.eulerAngles.z;

		if(stream.isWriting)
		{
			stream.Serialize(ref rotZ);
		}
		else
		{
			stream.Serialize(ref rotZ);

			this.transform.rotation = Quaternion.Euler(0, 0, rotZ);
		}
	}

	public void RequestNewInventory(float elapsedTime)
	{
		Debug.Log ("Recieved request to reset inventory!");
		m_shopInventory = new GameObject[m_shopSize];

		ItemIDHolder itemMan = GameObject.FindGameObjectWithTag("ItemManager").GetComponent<ItemIDHolder>();
		int[] returnedIDs = new int[m_shopSize];

		//Compile stockFlags
		bool[] stockFlags = new bool[5];
		stockFlags[0] = m_canStockWeapons;
		stockFlags[1] = m_canStockShields;
		stockFlags[2] = m_canStockEngines;
		stockFlags[3] = m_canStockPlating;
		stockFlags[4] = m_canStockCWeapons;

		//Compile rarityMods
		float[] rarityMods = new float[5];
		rarityMods[0] = m_weaponRarityMod;
		rarityMods[1] = m_shieldRarityMod;
		rarityMods[2] = m_engineRarityMod;
		rarityMods[3] = m_platingRarityMod;
		rarityMods[4] = m_cWeaponRarityMod;

		returnedIDs = GameObject.FindGameObjectWithTag("LootTable").GetComponent<LootTableScript>().RequestItemByApproximateValue((elapsedTime + 150), stockFlags, rarityMods, m_shopSize);

		//Now turn the IDs back into objects
		for(int i = 0; i < returnedIDs.Length; i++)
		{
			GameObject temp = itemMan.GetItemWithID(returnedIDs[i]);
			m_shopInventory[i] = temp;
		}

		//Now we're done!
		//Propagate our results to the clients
		for(int i = 0;i < m_shopInventory.Length; i++)
		{
			networkView.RPC ("PropagateItemInSlot", RPCMode.Others, returnedIDs[i], i);
		}
	}

	[RPC]
	void PropagateItemInSlot(int objectID, int slot)
	{
		//Recieve each object as an id

		//Turn it into an object and let it sit in the shop
		m_shopInventory[slot] = GameObject.FindGameObjectWithTag("ItemManager").GetComponent<ItemIDHolder>().GetItemWithID(objectID);
	}
}

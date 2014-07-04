using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;



/// <summary>
/// A NetworkInventory is a special type of inventory where the host manages everything to do with it. Any item requests
/// must go through the host server and items can only be taken if the client has a valid ItemTicket. This maintains
/// a synchronised and unexploitable inventory system.
/// </summary>
[System.Serializable]
public class NetworkInventory : MonoBehaviour 
{
	private enum RequestCheck
	{
		None = 1,
		Requested = 2,
		Unrequested = 3
	}


	/// Unity modifiable variables
	[SerializeField] List<ItemScript> m_inventory = new List<ItemScript>(0);	// Only objects with ItemScript components are valid
	[SerializeField, Range (0, 100)] int m_capacity = 20;						// The maximum number of items the inventory can hold
	[SerializeField, Range (0.1f, 120f)] float m_requestTimeOutSeconds = 5f;	// How long before a request ticket will be deleted due to it timing out
	[SerializeField] bool m_nullRemovedItems = false;							// Whether removals should just null the reference or remove it from the list entirely


	/// Internal data
	List<bool> m_isItemRequested = new List<bool>(0);				// Keeps a reference of whether each item has been requested
	List<ItemTicket> m_requestTickets = new List<ItemTicket>(0);	// Each ticket which has previously been handed out

	bool m_hadServerResponse = false;								// Indicates whether a response has been received or not
	bool m_itemAddResponse = false;									// Indicates whether the server will allow the item addition or not
	ItemTicket m_itemRequestResponse = new ItemTicket();			// The ticket to return once a response has been given


	int m_ticketNumber = Random.Range (0, int.MaxValue);			// A valid ticket number which will be incremented each time a ticket is created

	NetworkMessageInfo m_blankMessage = new NetworkMessageInfo();	// Used when the server sends a message to itself
	

	/// External references
	ItemIDHolder m_itemIDs = null;	// Useful for turning item ID numbers into actual GameObjects



	/// Getters, setters and properties
	public int capacity
	{
		get { return m_capacity; }
	}


	public int count
	{
		get { return m_inventory.Count; }
	}


	public bool itemAddResponse
	{
		get { return m_itemAddResponse; }
	}
	
	
	public ItemTicket itemRequestResponse
	{
		get { return m_itemRequestResponse; }
	}

	
	public ItemScript GetItemScript (int index)
	{
		if (IsValidIndex (index))
		{
			if (m_inventory[index])
			{
				return m_inventory[index];
			}
		}

		return null;
	}


	// Simply returns whether the server has responded to a request and what the response was
	public bool HasServerResponded()
	{
		return m_hadServerResponse;
	}



	/// Behavior functions
	// Initialise lists during load
	void Awake()
	{
		m_isItemRequested = Enumerable.Repeat (false, m_inventory.Count).ToList();
		m_requestTickets = Enumerable.Repeat (new ItemTicket(), m_inventory.Count).ToList();
	}


	//Initialise external references
	void Start() 
	{
		InitialiseItemIDs();
	}



	/// Private functions
	// Obtain a reference to the ItemIDHolder script
	void InitialiseItemIDs()
	{
		// Find Item Manager
		GameObject itemManager = GameObject.FindGameObjectWithTag ("ItemManager");
		
		if (itemManager)
		{
			m_itemIDs = itemManager.GetComponent<ItemIDHolder>();
			
			if (!m_itemIDs)
			{
				Debug.LogError ("ItemManager object does not contain an ItemIDHolder component.");
			}
		}
		
		else
		{
			Debug.LogError ("Unable to find object with tag: ItemManager");
		}
	}


	// Simply tests if the index is valid for the inventory List (I'm getting sick of typing it)
	bool IsValidIndex (int index)
	{
		return index >= 0 && index < m_inventory.Count;
	}
	
	
	// Resets the response booleans to default values so another request can be made
	void ResetResponse (bool fakeDecline = false)
	{
		m_hadServerResponse = fakeDecline;
		m_itemAddResponse = false;
		m_itemRequestResponse.Reset();
	}


	// Checks whether the item at the passed index is the desired item
	bool IsDesired (int index, int itemID, RequestCheck check = RequestCheck.None)
	{
		if (IsValidIndex (index))
		{
			// Check the itemID is correct then check whether it matters if it has been requested or not
			if (m_inventory[index] && m_inventory[index].m_equipmentID == itemID)
			{
				switch (check)
				{
					case RequestCheck.None:
						return true;

					case RequestCheck.Requested:
						return m_isItemRequested[index];

					case RequestCheck.Unrequested:
						return !m_isItemRequested[index];
				}
			}
		}

		return false;
	}
	
	
	// Determines the correct index for an item in the inventory, checking the preferred slot first
	int DetermineDesiredIndex (int itemID, int preferredIndex = -1, RequestCheck check = RequestCheck.None)
	{
		// Prioritise the preferred index over the itemID
		if (IsDesired (preferredIndex, itemID, check))
		{
			return preferredIndex;
		}

		// Brute force search
		else
		{
			for (int i = 0; i < m_inventory.Count; ++i)
			{
				if (IsDesired (i, itemID, check))
				{
					return i;
				}
			}
		}

		// -1 represents failure
		return -1;
	}


	// Searches for the corresponding index of the m_requestedTickets list for the given ticket
	int DetermineTicketIndex (ItemTicket ticket)
	{
		// The index of ticket
		int index = ticket.itemIndex;

		// Ideally the itemIndex will contain the perfect value
		if (IsDesired (ticket.itemID, ticket.itemIndex, RequestCheck.Requested) && m_requestTickets[index].Equals(ticket))
		{
			index = ticket.itemIndex;
		}
		
		else
		{
			// Use the functionality of List to find the ticket
			index = m_requestTickets.IndexOf (ticket);
		}

		return index;
	}






	/// Network functions
	// Used by clients to request an item from the server
	[RPC] void RequestItem (int itemID, int preferredIndex, NetworkMessageInfo message)
	{
		if (Network.isServer)
		{
			ItemTicket ticket;

			int index = DetermineDesiredIndex (itemID, preferredIndex, RequestCheck.Requested);
			
			if (IsValidIndex (index))
			{
				// Create the ticket
				ticket = new ItemTicket (m_ticketNumber++, itemID, index);
				
				// Keep a copy on the server and flag it as requested
				m_requestTickets[index] = ticket;
				m_isItemRequested[index] = true;

				// Clamp the ticket number between 0 and int.MaxValue, negative values are invalid
				m_ticketNumber = Mathf.Max (0, m_ticketNumber);
			}
			
			else
			{
				// Send back an invalid ticket
				ticket = new ItemTicket();
			}
			
			// This is the only way I've found to check if the message is blank, an alternative method would be preferable
			if (message.Equals (m_blankMessage))
			{
				RespondToItemRequest (ticket.uniqueID, ticket.itemID, ticket.itemIndex);
			}
			
			else
			{
				networkView.RPC ("RespondToItemRequest", message.sender, ticket.uniqueID, ticket.itemID, ticket.itemIndex);
			}
		}
		
		else
		{
			Debug.LogError ("A client attempted to call RequestItem in NetworkInventory.");
		}
	}


	// When a client requests an item be added to the inventory it must be authorised first
	[RPC] void RequestAdd (NetworkMessageInfo message)
	{
		// This is a much more simple function which will just check to see if the inventory is full or not

	}


	// Used to cancel an item request, provided you have the related ticket
	[RPC] void RequestCancel (int ticketID, int itemID, int itemIndex)
	{
		if (Network.isServer)
		{
			// Recompose the ticket for comparison purposes
			ItemTicket ticket = new ItemTicket (ticketID, itemID, itemIndex);

			if (ticket.isValid())
			{
				// The index of the cancellation
				int index = DetermineTicketIndex (ticket);

				// Attempt to cancel the ticket
				if (IsValidIndex (index))
				{
					// Reset the ticket
					m_requestTickets[ticket.itemIndex].Reset();
					m_isItemRequested[ticket.itemIndex] = false;
				}

				else
				{
					Debug.LogError ("An attempt was made to cancel a request which doesn't exist in NetworkInventory.");
				}
			}
		}

		else
		{
			Debug.LogError ("A client attempted to call RequestCancel in NetworkInventory.");
		}
	}
	
		
	// Used to tell the client if they are allowed to take the item they requested or not
	[RPC] void RespondToAddRequest (bool response)
	{
		m_hadServerResponse = true;
		m_itemAddResponse = response;
	}

	// Used to tell the client if they are allowed to take the item they requested or not
	[RPC] void RespondToItemRequest (int ticketID, int itemID, int itemIndex)
	{
		m_hadServerResponse = true;
		m_itemRequestResponse = new ItemTicket (ticketID, itemID, itemIndex);
	}

	
	// Used to specify the item at a particular slot which is then synchronised
	[RPC] void PropagateAddAtIndex (int index, int itemID)
	{

		// Allow null values if m_nullRemovedItems
		GameObject itemObject = m_itemIDs.GetItemWithID (itemID);
		ItemScript item = itemObject ? itemObject.GetComponent<ItemScript>() : null;

		// Only allow nulls if that has been specified as an attribute of the particular
		if (m_nullRemovedItems || !m_nullRemovedItems && item)
		{
			// The index should always be valid but just in case.
			if (IsValidIndex (index))
			{
				m_inventory[index] = item;
				m_isItemRequested[index] = false;
				m_requestTickets[index].Reset();
			}
			
			// The server may decide simply adding would be more suitable
			else
			{
				m_inventory.Add (item);
				m_isItemRequested.Add (false);
				m_requestTickets.Add (new ItemTicket());
			}
			
			
			// Propagate it to the clients to keep the clients inventories in sync
			if (Network.isServer)
			{
				networkView.RPC ("PropagateItemAtIndex", RPCMode.Others, index, itemID);
			}
		}
	}


	// Used to perform a synchronised removal
	[RPC] void PropagateRemovalAtIndex (int uniqueID, int itemID, int itemIndex, bool nullInsteadOfRemove)
	{
		ItemTicket ticket = new ItemTicket (uniqueID, itemID, itemIndex);

		if (ticket.isValid())
		{
			// The correct index is guaranteed for the clients so only determine it for the server
			int index = Network.isServer ? DetermineTicketIndex (ticket) : itemIndex;

			// Check if it is valid
			if (IsValidIndex (index))
			{
				// Remove or null the item based on the passed parameter
				if (nullInsteadOfRemove)
				{
					m_inventory[index] = null;
					m_isItemRequested[index] = false;
					m_requestTickets[index].Reset();
				}

				else
				{
					m_inventory.RemoveAt (index);
					m_isItemRequested.RemoveAt (index);
					m_requestTickets.RemoveAt (index);
				}


				// Propagate the change to the clients
				if (Network.isServer)
				{
					// Give clients the correct index
					itemIndex = index;

					// Propagate the removal
					networkView.RPC ("PropagateRemovalAtIndex", RPCMode.Others, uniqueID, itemID, itemIndex, nullInsteadOfRemove);
				}
			}

			else
			{
				Debug.LogError ("Attempt to remove an item from " + name + " with an expired ticket.");
			}
		}
	}

	

	/// Public functions
	// Used to request an item from the server, this should be the main entry point in inventory transactions
	public void RequestServerItem (int itemID, int preferredIndex = -1)
	{
		// Have client do the legwork to reduce strain on the host
		int index = DetermineDesiredIndex (itemID, preferredIndex);
		
		// -1 is returned on failure
		if (IsValidIndex (index))
		{
			// Silly Unity requires a workaround for the server
			if (Network.isServer)
			{
				RequestItem (itemID, index, m_blankMessage);
			}
			
			else
			{
				networkView.RPC ("RequestItem", RPCMode.Server, itemID, index);
			}
		}
		
		// Pretend the server declined the request
		else
		{
			ResetResponse (true);
		}
	}


	// Causes the server to cancel a request so that others can request the item
	public void RequestServerCancel (ItemTicket ticket)
	{
		// Ensure we are not wasting time by checking if the ticket is valid
		if (ticket.isValid())
		{
			if (Network.isServer)
			{
				RequestCancel (ticket.uniqueID, ticket.itemID, ticket.itemIndex);
			}

			else
			{
				networkView.RPC ("RequestCancel", RPCMode.Server, ticket.uniqueID, ticket.itemID, ticket.itemIndex);
			}
		}

		// Reset the response since we know they've received it
		ResetResponse (false);
	}


	// Requests that the server add an item to its inventory
	public void RequestServerAddItem (ItemScript item)
	{
		;
	}
}
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public enum RequestType
{
    ItemTake = 0,
    ItemAdd = 1,
    TicketValidity = 2
}

/// <summary>
/// A NetworkInventory is a special type of inventory where the host manages everything to do with it. Any item requests
/// must go through the host server and items can only be taken if the client has a valid ItemTicket. This maintains
/// a synchronised and unexploitable inventory system. Supports the use of composition and being a native component.
/// </summary>
[System.Serializable]
public sealed class NetworkInventory : MonoBehaviour
{
    // Used in the IsDesired functions
    private enum RequestCheck
    {
        None = 1,
        Requested = 2,
        Unrequested = 3
    }



    /// Unity modifiable variables
    [SerializeField]
    List<ItemScript> m_inventory = new List<ItemScript>(0);	// Only objects with ItemScript components are valid
    [SerializeField, Range(0, 100)]
    int m_capacity = 20;						// The maximum number of items the inventory can hold
    [SerializeField, Range(0.1f, 600f)]
    float m_requestTimeOutSeconds = 120f;	// How long before a request ticket will be deleted due to it timing out
    [SerializeField]
    bool m_nullRemovedItems = false;							// Whether removals should just null the reference or remove it from the list entirely



    /// Internal data
    bool m_hasAwoken = false;										// Stops Awake() being called more than once
    bool m_hasStarted = false;										// Stops Start() being called more than once
    bool m_hasServerResponded = false;								// Indicates whether a response has been received or not
    bool m_hasAdminKeyBeenRetrieved = false;						// The master key can only be retrieved once, otherwise an invalid value will be returned
    bool m_ticketValidityResponse = false;							// Used to tell the client if their ticket is valid or not

    int m_adminKey = 0;												// A unique key which allows for elevated privelages such as replacing requested tickets
    int m_ticketNumber = 0;											// A valid ticket number which will be incremented each time a ticket is created
    // NOTE: NEVER SET THIS VALUE DIRECTLY! Use the ticketNumber property instead

    int m_addRequests = 0;											// A counter for how many add requests have been made
    // NOTE: NEVER SET THIS VALUE DIRECTLY! Use the addRequests property instead

    List<bool> m_isItemRequested = new List<bool>(0);				// Keeps a reference of whether each item has been requested
    List<ItemTicket> m_requestTickets = new List<ItemTicket>(0);	// Each ticket which has previously been handed out

    ItemTicket m_itemAddResponse = new ItemTicket();				// Indicates whether the server will allow the item addition or not
    ItemTicket m_itemRequestResponse = new ItemTicket();			// The ticket to return once a response has been given
    NetworkMessageInfo m_blankMessage = new NetworkMessageInfo();	// Used when the server sends a message to itself



    /// External references
    ItemIDHolder m_itemIDs = null;	// Useful for turning item ID numbers into actual GameObjects



    /// Getters, setters and properties
    public bool HasServerResponded()
    {
        return m_hasServerResponded;
    }


    public bool GetTicketValidityResponse()
    {
        return m_ticketValidityResponse;
    }


    /// <summary>
    /// The first time this is called the real admin key will be returned, otherwise -1 will be returned. The admin key allws for the requesting
    /// of requested items and potentially other administrative functionality.
    /// </summary>
    /// <returns>The capacity.</returns>
    public int GetAdminKey()
    {
        if (!m_hasAdminKeyBeenRetrieved)
        {
            m_hasAdminKeyBeenRetrieved = true;
            return m_adminKey;
        }

        else
        {
            Debug.Log("An attempt was made to retrieve " + name + ".NetworkInventory().m_adminKey when it has already been requested.");
            return -1;
        }
    }


    public int GetCapacity()
    {
        return m_capacity;
    }


    public int GetCount()
    {
        return m_inventory.Count;
    }


    public ItemTicket GetItemAddResponse()
    {
        return m_itemAddResponse;
    }


    public ItemTicket GetItemRequestResponse()
    {
        return m_itemRequestResponse;
    }


    public ItemScript GetItemScript(int index)
    {
        // Since this will be the main entry point of the GUI and will be called multiple times per frame
        // a try-catch block is used instead of checking the the index each time.
        try
        {
            return m_inventory[index];
        }

        catch (System.Exception error)
        {
            Debug.LogError("Exception occurred in " + name + ".NetworkInventory: " + error.Message);
            return null;
        }
    }


    /// Shorthand for GetItemScript(), allows the usage of the [] operator
    public ItemScript this[int index]
    {
        get { return GetItemScript(index); }
    }


    // Means that we don't have to max sure m_ticketNumber is clamped every time it's incremented
    private int ticketNumber
    {
        get { return m_ticketNumber; }
        set
        {
            // Keep it clamped to positive integers
            m_ticketNumber = Mathf.Max(0, value);
        }
    }


    // Means that we don't have to max sure m_addNumber is clamped every time it's incremented
    private int addRequests
    {
        get { return m_addRequests; }
        set
        {
            // Keep it clamped to positive integers
            m_addRequests = Mathf.Max(0, value);
        }
    }



    /// Behavior functions
    // Initialise lists during load, allow the use of composition by making Awake() public
    public void Awake()
    {
        if (!m_hasAwoken)
        {
            // Clean the inventory
            if (!m_nullRemovedItems)
            {
                PropagateRemoveNulls(true);
            }

            // Ensure the correct capacity has been set
            if (m_inventory.Count > m_capacity)
            {
                m_inventory.RemoveRange(m_capacity, m_inventory.Count - m_capacity);
            }

            m_inventory.Capacity = m_capacity;

            // Fill the other lists
            m_isItemRequested = Enumerable.Repeat(false, m_inventory.Count).ToList();
            m_requestTickets = Enumerable.Repeat(new ItemTicket(), m_inventory.Count).ToList();

            // Resize their capacities to increase performance at the sake of RAM
            m_isItemRequested.Capacity = m_capacity;
            m_requestTickets.Capacity = m_capacity;

            // Assign the admin key, it doesn't need to be synchronised because only clients will check it
            m_adminKey = Random.Range(0, int.MaxValue);

            // Give ticketNumber a random starting value
            ticketNumber = Random.Range(0, int.MaxValue);

            // Prevent future calls
            m_hasAwoken = true;
        }
    }


    // Initialise external references before the first frame, allow the use of composition by making Start() public
    public void Start()
    {
        if (!m_hasStarted)
        {
            // Ensure NetworkInventory has awoken at least once
            Awake();

            // m_itemIDs needs to be working otherwise there's gonna be big problems
            InitialiseItemIDs();

            // Prevent future calls
            m_hasStarted = true;
        }
    }



    /// Private functions
    // Obtain a reference to the ItemIDHolder script
    void InitialiseItemIDs()
    {
        // Find Item Manager
        GameObject itemManager = GameObject.FindGameObjectWithTag("ItemManager");

        if (itemManager)
        {
            m_itemIDs = itemManager.GetComponent<ItemIDHolder>();

            if (!m_itemIDs)
            {
                Debug.LogError("ItemManager object does not contain an ItemIDHolder component.");
            }
        }

        else
        {
            Debug.LogError("Unable to find object with tag: ItemManager");
        }
    }


    // Resets the response booleans to default values so another request can be made
    void ResetResponse(bool fakeDecline = false)
    {
        m_hasServerResponded = fakeDecline;
        m_ticketValidityResponse = false;
        m_itemAddResponse.Reset();
        m_itemRequestResponse.Reset();
    }


    // Simply tests if the index is valid for the inventory List (I'm getting sick of typing it)
    bool IsValidIndex(int index)
    {
        return index >= 0 && index < m_inventory.Count;
    }


    // Determines if the inventory is full
    bool IsInventoryFull()
    {
        // If items are being nulled then we need to count the nulls for accuracy
        int count = m_nullRemovedItems ?
                                        m_inventory.Count - CountNulls() + addRequests :
                                        m_inventory.Count + addRequests;

        return count >= m_capacity;
    }


    // Checks whether the item at the passed index is the desired item
    bool IsDesiredIndex(int index, int itemID, RequestCheck check = RequestCheck.None)
    {
        if (IsValidIndex(index))
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


    // Searches for the first null value in m_inventory
    int FindFirstNull()
    {
        for (int i = 0; i < m_inventory.Count; ++i)
        {
            if (!m_inventory[i])
            {
                return i;
            }
        }

        // Default to -1 if none are found
        return -1;
    }


    // Counts how many nulls exist in the inventory list
    int CountNulls()
    {
        int found = 0;

        for (int i = 0; i < m_inventory.Count; ++i)
        {
            if (!m_inventory[i])
            {
                ++found;
            }
        }

        return found;
    }


    // Determines the correct index for an item in the inventory, checking the preferred slot first
    int DetermineDesiredIndex(int itemID, int preferredIndex = -1, RequestCheck check = RequestCheck.None)
    {
        // Prioritise the preferred index over the itemID
        if (IsDesiredIndex(preferredIndex, itemID, check))
        {
            return preferredIndex;
        }

        // Brute force search
        else
        {
            for (int i = 0; i < m_inventory.Count; ++i)
            {
                if (IsDesiredIndex(i, itemID, check))
                {
                    return i;
                }
            }
        }

        // -1 represents failure
        return -1;
    }


    // Searches for the corresponding index of the m_requestedTickets list for the given ticket
    int DetermineTicketIndex(ItemTicket ticket)
    {

        // Ideally the itemIndex will contain the perfect value
        if (IsDesiredIndex(ticket.itemIndex, ticket.itemID, RequestCheck.Requested) && m_requestTickets[ticket.itemIndex].Equals(ticket))
        {
            return ticket.itemIndex;
        }

        else
        {

            // Manually search for the ticket
            for (int i = 0; i < m_requestTickets.Count; ++i)
            {
                if (m_requestTickets[i].Equals(ticket))
                {
                    return i;
                }
            }
        }

        Debug.LogError("Couldn't determine index of " + ticket + " in " + name + ".NetworkInventory");
        return -1;
    }


    // Reserves an item using the passed ticket
    void ReserveItem(int index, ItemTicket ticket)
    {
        if (IsValidIndex(index))
        {
            // Ensure the previous ticket gets reset so it's invalid
            m_requestTickets[index].Reset();

            // Keep a copy on the server and flag it as requested
            m_requestTickets[index] = ticket;
            m_isItemRequested[index] = true;
        }

        // Start the expiration countdown
        StartCoroutine(ExpireItemTicket(ticket, m_requestTimeOutSeconds));
    }


    // This function should be called using Invoke() after the desired period of time
    IEnumerator ExpireItemTicket(ItemTicket toExpire, float timeToWait)
    {
        //Debug.Log("Waiting to expire ticket: " + toExpire + " in " + timeToWait);

        // Wait for the desired amount of time
        yield return new WaitForSeconds(timeToWait);

        // Tickets which have been redeemed will be reset to the standard ticket
        if (toExpire.IsValid())
        {
            // Only the server needs to manage whether the item is flagged as requested or not
            if (Network.isServer)
            {
                // Obtain the index
                int index = DetermineTicketIndex(toExpire);

                // Flag the item as unrequested
                if (IsValidIndex(index))
                {
                    // Reset the request
                    m_isItemRequested[index] = false;
                }

                // Must be an add request
                else
                {
                    --addRequests;
                }
            }

            // Reset the ticket to ensure it's invalid
            toExpire.Reset();

            Debug.Log("Expiring ItemTicket: " + toExpire.uniqueID + " / " + toExpire.itemID + " / " + toExpire.itemIndex);
        }
    }



    /// Admin functions
    // Completely wipes the inventory and all tickets
    [RPC]
    void PropagateResetInventory()
    {
        if (m_nullRemovedItems)
        {
            for (int i = 0; i < m_inventory.Count; ++i)
            {
                m_inventory[i] = null;
                m_isItemRequested[i] = false;
                m_requestTickets[i].Reset();
            }
        }
        
        else
        {
            // Clear the lists
            m_inventory.Clear();
            m_isItemRequested.Clear();
            m_requestTickets.Clear();
        }

        // Reset the add request counter and all responses
        ResetResponse(false);
        addRequests = 0;

        // Propagate it to the others
        if (Network.isServer)
        {
            networkView.RPC("PropagateResetInventory", RPCMode.Others);
        }
    }



    /// Network functions
    // Used by clients to request an item from the server
    [RPC]
    void RequestItem(int itemID, int preferredIndex, NetworkMessageInfo message)
    {
        if (Network.isServer)
        {
            ItemTicket ticket = new ItemTicket();

            int index = DetermineDesiredIndex(itemID, preferredIndex, RequestCheck.Unrequested);

            if (IsValidIndex(index))
            {
                // Create the ticket
                ticket.uniqueID = ticketNumber++;
                ticket.itemID = itemID;
                ticket.itemIndex = index;

                // Ensure the item is successfully reserved
                ReserveItem(index, ticket);
            }

            // Else send back an invalid ticket
            //Debug.Log("Index: " + index + ", itemID: " + itemID + ", preferredIndex: " + preferredIndex + ".");

            // This is the only way I've found to check if the message is blank, an alternative method would be preferable
            if (message.Equals(m_blankMessage))
            {
                RespondToItemRequest(ticket);
            }

            else
            {
                networkView.RPC("RespondToItemRequest", message.sender, ticket.uniqueID, ticket.itemID, ticket.itemIndex);
            }
        }

        else
        {
            ResetResponse(true);
            Debug.LogError("A client attempted to call RequestItem in NetworkInventory.");
        }
    }


    // When a client requests an item be added to the inventory it must be authorised first
    [RPC]
    void RequestAdd(int itemID, int index, bool adminMode, NetworkMessageInfo info)
    {
        if (Network.isServer)
        {
            // Create the default response
            ItemTicket ticket = new ItemTicket(-1, itemID, index);

            // We now need to check if the itemID is valid
            if (itemID >= 0)
            {
                bool isValidIndex = IsValidIndex(index);

                // An item can only be requested to be replaced if the item hasn't been requested or we're running in admin mode
                if (!isValidIndex || !m_isItemRequested[index] || adminMode)
                {
                    if (isValidIndex)
                    {
                        // Check to see if the item at the index is null, if so we know it is an add request.
                        if (!m_inventory[index])
                        {
                            if (!IsInventoryFull())
                            {
                                ticket.uniqueID = ticketNumber++;
                                ++addRequests;

                                ReserveItem(index, ticket);
                            }
                        }

                        // We know it is a replace request so it doesn't matter if the inventory is full, we also know that the item
                        // is either unrequested or we have access to overwrite it because of the intial conditions.
                        else
                        {
                            ticket.uniqueID = ticketNumber++;

                            ReserveItem(index, ticket);
                        }
                    }

                    // If the index is invalid it should be added on to the end which is an add request.
                    else if (!IsInventoryFull())
                    {
                        ticket.uniqueID = ticketNumber++;
                        ++addRequests;

                        ReserveItem(index, ticket);
                    }
                }
            }


            // Silly workaround for RPC sending limitation
            if (info.Equals(m_blankMessage))
            {
                RespondToAddRequest(ticket);
            }

            else
            {
                networkView.RPC("RespondToAddRequest", info.sender, ticket.uniqueID, ticket.itemID, ticket.itemIndex);
            }
        }

        // A client called the function
        else
        {
            ResetResponse(true);
            Debug.LogError("A client attempted to call RequestAdd in NetworkInventory");
        }
    }


    // Used to cancel an item request, provided you have the related ticket. This function is an entry point for RequestCancelItem (ticket:ItemTicket):void.
    [RPC]
    void RequestCancel(int ticketID, int itemID, int itemIndex)
    {
        if (Network.isServer)
        {
            // Recompose the ticket for comparison purposes
            RequestCancel(new ItemTicket(ticketID, itemID, itemIndex));
        }

        else
        {
            Debug.LogError("A client attempted to call RequestCancel in NetworkInventory");
        }
    }


    // An ItemTicket version is provided to increase performance as items time out.
    void RequestCancel(ItemTicket ticket)
    {
        if (ticket.IsValid())
        {
            // If the index is invalid we know that the ticket is an add request
            if (!IsValidIndex(ticket.itemIndex))
            {
                --addRequests;
            }

            else
            {
                // The index of the cancellation
                int index = DetermineTicketIndex(ticket);

                // Attempt to cancel the ticket
                if (IsValidIndex(index))
                {
                    // Reset the ticket

                    m_requestTickets[index].Reset();
                    m_isItemRequested[index] = false;

                    // Check to see if the desired index was a null item, this means that it was an add requests
                    if (!m_inventory[index])
                    {
                        --addRequests;
                    }
                }

                else
                {
                    Debug.LogError("An attempt was made to cancel a request which doesn't exist in " + name + ".NetworkInventory.");
                }
            }
        }

        else
        {
            Debug.LogError("Attempt to cancel invalid ticket in " + name + ".NetworkInventory");
        }
    }


    // Checks to see ticket still exists on the server
    [RPC]
    void TicketValidityCheck(int ticketID, int itemID, int itemIndex, NetworkMessageInfo message)
    {
        if (Network.isServer)
        {
            TicketValidityCheck(new ItemTicket(ticketID, itemID, itemIndex), message);
        }

        else
        {
            Debug.LogError("A client attempted to call TicketValidtyCheck() in NetworkInventory()");
        }
    }


    // An ItemTicket version to increase efficiency for the host
    void TicketValidityCheck(ItemTicket ticket, NetworkMessageInfo message)
    {
        // We know that DetermineTicketIndex will either return a correct index or an invalid index if it doesn't work
        int index = DetermineTicketIndex(ticket);

        // Using the validity of the index we can tell if the ticket is available and then check if it's still valid
        bool response = IsValidIndex(index) && m_requestTickets[index].IsValid();

        // Silly Unity workaround
        if (message.Equals(m_blankMessage))
        {
            RespondToTicketValidityCheck(response);
        }

        else
        {
            networkView.RPC("RespondToTicketValidityCheck", message.sender, response);
        }
    }


    // Used to tell the client if they are allowed to take the item they requested or not
    [RPC]
    void RespondToItemRequest(int ticketID, int itemID, int itemIndex)
    {
        RespondToItemRequest(new ItemTicket(ticketID, itemID, itemIndex));
    }


    // The server can just pass the ticket reference through to improve performance
    void RespondToItemRequest(ItemTicket ticket)
    {
        // Assign response values
        m_hasServerResponded = true;
        m_itemRequestResponse = ticket;

        // Ensure the ticket expires, the server doesn't need to do it again
        if (Network.isClient)
        {
            StartCoroutine(ExpireItemTicket(ticket, m_requestTimeOutSeconds));
        }
    }


    // Used to tell the client if they are allowed to take the item they requested or not
    [RPC]
    void RespondToAddRequest(int ticketID, int itemID, int itemIndex)
    {
        RespondToAddRequest(new ItemTicket(ticketID, itemID, itemIndex));
    }


    // The server can just pass the ticket reference through to improve performance
    void RespondToAddRequest(ItemTicket ticket)
    {
        m_hasServerResponded = true;
        m_itemAddResponse = ticket;

        // Ensure the ticket expires, the server doesn't need to do it again
        if (Network.isClient)
        {
            StartCoroutine(ExpireItemTicket(ticket, m_requestTimeOutSeconds));
        }
    }


    // Used to tell clients whether their ticket is still valid for use
    [RPC]
    void RespondToTicketValidityCheck(bool isValid)
    {
        m_hasServerResponded = true;
        m_ticketValidityResponse = isValid;
    }


    // The entry point for item addition
    [RPC]
    void ServerAddItem(int uniqueID, int itemID, int itemIndex)
    {
        if (Network.isServer)
        {
            ServerAddItem(new ItemTicket(uniqueID, itemID, itemIndex));
        }

        else
        {
            Debug.LogError("A client attempted to call ServerAddItem in NetworkInventory");
        }
    }


    void ServerAddItem(ItemTicket ticket)
    {
        if (ticket.IsValid())
        {
            // Attempt to find the first null value, if a position hasn't been specified
            if (m_nullRemovedItems && !IsValidIndex(ticket.itemIndex))
            {
                ticket.itemIndex = FindFirstNull();
            }

            // Finally propagate the addition
            PropagateItemAtIndex(ticket.itemIndex, ticket.itemID);
        }

        else
        {
            Debug.LogError("Attempt to add item with invalid or expired ticket in " + name + ".NetworkInventory");
        }
    }


    // Used to specify the item at a particular slot which is then synchronised
    [RPC]
    void PropagateItemAtIndex(int index, int itemID)
    {
        // Allow null values if m_nullRemovedItems
        GameObject itemObject = m_itemIDs.GetItemWithID(itemID);
        ItemScript item = itemObject ? itemObject.GetComponent<ItemScript>() : null;

        // Only allow nulls if that has been specified as an attribute
        if (m_nullRemovedItems || (!m_nullRemovedItems && item))
        {
            // The index should always be valid but just in case.
            if (IsValidIndex(index))
            {
                // Decrement the addRequests counter if being placed in a null
                if (!m_inventory[index])
                {
                    --addRequests;
                }

                m_inventory[index] = item;
                m_isItemRequested[index] = false;
                m_requestTickets[index].Reset();
            }

            // The server may decide simply adding would be more suitable
            else
            {
                m_inventory.Add(item);
                m_isItemRequested.Add(false);
                m_requestTickets.Add(new ItemTicket());
                --addRequests;
            }


            // Propagate it to the clients to keep the clients inventories in sync
            if (Network.isServer)
            {
                --addRequests;


                networkView.RPC("PropagateItemAtIndex", RPCMode.Others, index, itemID);
            }
        }

        else
        {
            Debug.Log("Attempt to add null to " + name + ".NetworkInventory.m_inventory when nulls are not allowed.");
        }
    }


    // Reconstructs the item ticket before passing the ticket into the actual function
    [RPC]
    void PropagateRemovalAtIndex(int uniqueID, int itemID, int itemIndex)
    {
        // Reconstruct the ticket
        ItemTicket ticket = new ItemTicket(uniqueID, itemID, itemIndex);
        
        PropagateRemovalAtIndex(ticket);
    }


    // Used to perform a synchronised removal
    void PropagateRemovalAtIndex(ItemTicket ticket)
    {
        //Debug.Log(ticket.ToString());
        if (ticket.IsValid())
        {
            // The correct index is guaranteed for the clients so only determine it for the server
            int index = Network.isServer ? DetermineTicketIndex(ticket) : ticket.itemIndex;
            //Debug.Log("Index: " + index);
            // Check if it is valid
            if (IsValidIndex(index))
            {
                // Remove or null the item based on the passed parameter
                if (m_nullRemovedItems)
                {
                    m_inventory[index] = null;
                    m_isItemRequested[index] = false;
                }

                else
                {
                    //Debug.Log("Removing at: " + index);
                    m_inventory.RemoveAt(index);
                    m_isItemRequested.RemoveAt(index);

                    // Reset the ticket so the expiration coroutine knows it has been removed
                    m_requestTickets.RemoveAt(index);
                }

                // Propagate the change to the clients
                if (Network.isServer)
                {
                    // Give clients the correct index
                    ticket.itemIndex = index;

                    // Propagate the removal
                    Debug.Log("Sending removal notification to others.");
                    networkView.RPC("PropagateRemovalAtIndex", RPCMode.Others, ticket.uniqueID, ticket.itemID, ticket.itemIndex);
                }
            }

            else
            {
                Debug.LogError("Attempt to remove an item from " + name + " with an invalid or expired ticket.");
            }
        }
    }


    // Deletes any null values
    [RPC]
    void PropagateRemoveNulls(bool localOnly)
    {
        bool propagateRemoval = false;
        for (int i = 0; i < m_inventory.Count; ++i)
        {
            if (!m_inventory[i])
            {
                m_inventory.RemoveAt(i);

                // Counts are synchronised, this just ensures that if ran at the start, the function won't crash.
                if (m_isItemRequested.Count > i)
                {
                    m_isItemRequested.RemoveAt(i);
                    m_requestTickets.RemoveAt(i);
                }

                propagateRemoval = true;
                --i;
            }
        }

        // Don't waste bandwidth if no removal was performed
        if (Network.isServer && !localOnly && propagateRemoval)
        {
            networkView.RPC("PropagateRemoveNulls", RPCMode.Others);
        }
    }



    /// Public functions
    ///
    /// <summary>
    /// This is an administrator function which completely resets the inventory. Only valid if the adminKey is correct.
    /// </summary>
    /// <returns><c>true</c>, if the admin key is valid, <c>false</c> otherwise.</returns>
    /// <param name="adminKey">Admin key.</param>
    public bool AdminResetInventory(int adminKey)
    {
        if (adminKey == m_adminKey)
        {
            if (Network.isServer)
            {
                PropagateResetInventory();
            }

            else
            {
                networkView.RPC("PropagateResetInventory", RPCMode.Server);
            }

            return true;
        }

        return false;
    }


    // Used to request an item from the server, this should be the main entry point in inventory transactions
    public void RequestServerItem(int itemID, int preferredIndex = -1)
    {
        // Have client do the legwork to reduce strain on the host
        int index = Network.isClient ? DetermineDesiredIndex(itemID, preferredIndex) : preferredIndex;

        // Reset the response variables now
        ResetResponse(false);

        // Silly Unity requires a workaround for the server
        if (Network.isServer)
        {
            RequestItem(itemID, index, m_blankMessage);
        }

        // Clients need to check if the index generated is valid
        else if (IsValidIndex(index))
        {
            networkView.RPC("RequestItem", RPCMode.Server, itemID, index);
        }

        // Pretend the server responded with a failure
        else
        {
            ResetResponse(true);
        }
    }


    /// <summary>
    /// Makes a request to add an item at any point or replace an existing one. Using the admin key you can replace requested items, however, without
    /// the key you're limited to only replacing unrequested items.
    /// </summary>
    /// <param name="item">Item to be added.</param>
    /// <param name="preferredIndex">The index of the item to replace (-1 will just add it anywhere).</param>
    /// <param name="adminKey">Unlocks admin mode if you have the right key.</param>
    public void RequestServerAdd(ItemScript item, int preferredIndex = -1, int adminKey = -1)
    {
        if (item && item.m_equipmentID >= 0)
        {
            // Determine whether admin mode is accessible
            bool adminMode = adminKey == m_adminKey;

            // Reset the response whilst they away one
            ResetResponse(false);

            // Silly Unity requires a workaround for the server
            if (Network.isServer)
            {
                RequestAdd(item.m_equipmentID, preferredIndex, adminMode, m_blankMessage);
            }

            else
            {
                networkView.RPC("RequestAdd", RPCMode.Server, item.m_equipmentID, preferredIndex, adminMode);
            }
        }

        // Pretend the server declined the request
        else
        {
            ResetResponse(true);
            Debug.LogError(name + ".NetworkInventory: Refused to request invalid item.");
        }
    }


    // Causes the server to cancel a request so that others can request the item
    public void RequestServerCancel(ItemTicket ticket)
    {

        Debug.Log("Cancelling ticket: " + ticket);
        // Ensure we are not wasting time by checking if the ticket is valid
        if (ticket && ticket.IsValid())
        {
            if (Network.isServer)
            {
                RequestCancel(ticket);
            }

            else
            {
                networkView.RPC("RequestCancel", RPCMode.Server, ticket.uniqueID, ticket.itemID, ticket.itemIndex);
            }
        }

        // Reset the response since we know they've received it
        ResetResponse(false);
    }


    // Used to ensure the validity of a ticket just before handing it in
    public void RequestTicketValidityCheck(ItemTicket ticket)
    {
        if (ticket && ticket.IsValid())
        {
            // Reset the response, don't call the function otherwise it will break AddItemToServer() and RemoveItemFromServer()
            m_hasServerResponded = false;
            m_ticketValidityResponse = false;

            // All that needs to be done is just contact the server to find out if the ticket still exists in the list
            if (Network.isServer)
            {
                TicketValidityCheck(ticket, m_blankMessage);
            }

            else
            {
                networkView.RPC("TicketValidityCheck", RPCMode.Server, ticket.uniqueID, ticket.itemID, ticket.itemIndex);
            }
        }

        else
        {
            ResetResponse(true);
        }
    }


    /// <summary>
    /// Attempts to add an item to the inventory, note if you haven't been given express permission from your latest request this will fail.
    /// Also it's worth noting that the return value doesn't mean the server will definitely add the item, if any error occurs it will not exist.
    /// </summary>
    /// <returns><c>true</c>, if the transaction goes through, <c>false</c> otherwise.</returns>
    /// <param name="ticket">The ticket given by the server which authorises your transaction.</param>
    public bool AddItemToServer(ItemTicket ticket)
    {
        // Check the item exists and whether the transaction has been authorised
        if (ticket && ticket.IsValid() && ticket == m_itemAddResponse)
        {
            // Unity silliness again
            if (Network.isServer)
            {
                ServerAddItem(ticket);
            }

            else
            {
                networkView.RPC("ServerAddItem", RPCMode.Server, ticket.uniqueID, ticket.itemID, ticket.itemIndex);
            }

            // Reset the response to ensure the security of future transactions
            ResetResponse(false);

            // Transaction processed successfully
            return true;
        }


        // If this point has been reached then a problem has occurred
        Debug.LogError(name + ".NetworkInventory.AddItemToServer() transaction failed.");
        ResetResponse(false);
        return false;
    }


    /// <summary>
    /// Attempts to remove the item from the server, if the ticket doesn't exist on the servers side due to it timing out and such then
    /// it will not be removed.
    /// </summary>
    /// <returns><c>true</c>, if item from server was removed, <c>false</c> otherwise.</returns>
    /// <param name="ticket">Ticket.</param>
    public bool RemoveItemFromServer(ItemTicket ticket)
    {
        // Ensure the ticket is both valid to prevent wasting the servers time
        if (ticket && ticket.IsValid() && ticket == m_itemRequestResponse)
        {
            if (Network.isServer)
            {
                PropagateRemovalAtIndex(ticket);
            }

            else
            {
                networkView.RPC("PropagateRemovalAtIndex", RPCMode.Server, ticket.uniqueID, ticket.itemID, ticket.itemIndex);
            }

            // Reset the response to maintain security of the inventory
            ResetResponse(false);

            // Transaction completed successfully
            return true;
        }


        // If this point has been reached then a problem has occurred
        Debug.LogError(name + ": NetworkInventory.RemoveItemFromServer() transaction failed.");
        ResetResponse(false);
        return false;
    }
}
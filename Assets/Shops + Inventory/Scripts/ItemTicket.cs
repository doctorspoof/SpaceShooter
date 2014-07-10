/// <summary>
/// An ItemTicket is used in transactions with a NetworkInventory system. It contains a unique ID, the index of the
/// requested item and the item ID if the index is invalid. The contents of the ticket are read-only so a ticket
/// can only be duplicated by knowing the exact values.
/// </summary>
public sealed class ItemTicket
{
	/// Variables

	/// <summary>
	/// Each ticket should contain a unique ID which is used to identify whether the ticket is valid or not.
	/// </summary>
	public int uniqueID = -1;
	
	
	/// <summary>
	/// Each item contains a unique ID which itemID represents, this should be used as a fallback if the index is
	/// invalid.
	/// </summary>
	public int itemID = -1;


	/// <summary>
	/// The itemIndex identifies where in the array or list of the NetworkInventory the corresponding item should be.
	/// </summary>
	public int itemIndex = -1;
	
	
	/// <summary>
	/// A static reference to the default values an ItemTicket holds when it is reset.
	/// </summary>
	public static readonly ItemTicket reset = new ItemTicket().Reset();



	/// Functions
	/// <summary>
	/// The default constructor which will initiate the ItemTicket to default invalid values.
	/// </summary>
	public ItemTicket (int _uniqueID = -1, int _itemID = -1, int _itemIndex = -1)
	{
		uniqueID = _uniqueID;
		itemID = _itemID;
		itemIndex = _itemIndex;
	}


	// Allows for statements such as if (ticket) instead of if (ticket != null).
	public static implicit operator bool (ItemTicket ticket)
	{
		return ticket != null;
	}


	/// <summary>
	/// A simple function which will check to see if any value is equal to -1, making it invalid.
	/// </summary>
	public bool IsValid()
	{
		return !(uniqueID == -1 || itemID == -1);
	}


	/// <summary>
	/// Will reset the value of each variable to -1.
	/// </summary>
	public ItemTicket Reset()
	{
		uniqueID = -1;
		itemID = -1;
		itemIndex = -1;

		return this;
	}
}

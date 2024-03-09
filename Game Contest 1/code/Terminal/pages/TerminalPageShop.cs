
public class TerminalPageShop : ITerminalPage
{

	public string[] Lines { get; set; } = new string[]
	{
		"Welcome to the store.",
		"Type BUY followed by any item.",
		"Order items in bulk by typing a number.",
		"----------------------------------------------",
	};

	public string[] GetLines()
	{
		// boiler-plate
		List<string> l = Lines.ToList();

		// List all moons.
		foreach (ShopManager.ShopItem item in ShopManager.Items )
		{
			l.Add( " " );
			l.Add( $"> {item.Name}    //    Price: ${item.Cost}" );
		}

		return l.ToArray();

	}




}

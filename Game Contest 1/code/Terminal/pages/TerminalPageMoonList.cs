
public class TerminalPageMoonList : ITerminalPage
{

	public string[] Lines { get; set; } = new string[]
	{
		"Available destinations are listed down below.",
		"Type 'ROUTE' followed by desired destination",
		"To route the Auto-Pilot.",
		"----------------------------------------------"
	};

	public string[] GetLines()
	{
		// boiler-plate
		List<string> l = Lines.ToList();

		// List all moons.
		foreach (MoonDefinition moon in LethalGameManager.MoonDefinitions )
		{
			l.Add( " " );
			l.Add( $"> {moon.ResourceName.ToUpper()} ( '{moon.TravelCost} Credits )" );
		}

		return l.ToArray();

	}




}

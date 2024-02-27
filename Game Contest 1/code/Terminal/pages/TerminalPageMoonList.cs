
public class TerminalPageMoonList : ITerminalPage
{

	public string[] Lines { get; set; } = new string[]
	{
		"Available destinations are listed down below.",
		"Type 'MOON' followed by desired destination to travel.",
		"----------------------------------------------"
	};

	public string[] GetLines()
	{
		// boiler-plate
		List<string> l = Lines.ToList();

		// List all moons.
		foreach(MoonDefinition moon in LethalGameManager.Instance.MoonDefinitions )
		{
			Log.Info( moon.ResourceName );

			l.Add( " " );
			l.Add( $"{moon.ResourceName} ('{moon.TravelCost} Credit)" );
		}

		return l.ToArray();

	}




}

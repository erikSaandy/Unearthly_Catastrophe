
public class TerminalPageMain : ITerminalPage
{

	public string[] Lines { get; set; } = new string[]
	{
		"U.C.T TERMINAL v1.2",
		"----------------------------------------------",
		"  Type 'NEXT' at any time to move through pages.",
		" ",
		"> Moons",
		"  A list of all moons available for the auto-pilot.",
		" ",
		//"> Shop",
		//"  Browse the shop for items to aid you.",
		//" ",
		"> Home",
		"  Return to this page.",
		" ",
		"> Exit",
		"  Exit out of the terminal interface."

	};

	public string[] GetLines()
	{
		return Lines;
	}
}

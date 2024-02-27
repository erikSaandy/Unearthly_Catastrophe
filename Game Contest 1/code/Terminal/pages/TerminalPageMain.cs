
public class TerminalPageMain : ITerminalPage
{

	public string[] Lines { get; set; } = new string[]
	{
		"U.C.T TERMINAL v1.2",
		"----------------------------------------------",
		" ",
		"> Moons",
		" A list of all moons available for the auto-pilot.",
		" ",
		"> Home",
		" Return to this page.",
		" ",
		"> Exit",
		"Exit out of the terminal interface"

	};

	public string[] GetLines()
	{
		return Lines;
	}
}

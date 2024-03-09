
public class TerminalPageConfirm : ITerminalPage
{

	public Action OnConfirm { get; private set; }

	public TerminalPageConfirm( string message, Action OnConfirm )
	{
		Lines[0] = message;
		this.OnConfirm = OnConfirm;
	}


	public string[] Lines { get; set; } = new string[]
	{
		"",
		"",
		"Type CONFIRM or DENY to continue."
	};

	public string[] GetLines() { return Lines; }




}

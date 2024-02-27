using Saandy;
using Sandbox;

public sealed class TerminalComponent : Component
{
	readonly TerminalCommand[] Commands = new TerminalCommand[]
	{
		new TerminalCommand("moons"),
		new TerminalCommand("store"),
		new TerminalCommand("quit")
	};

	[Sync] public bool IsBeingUsed { get; private set; }

	/// <summary>
	/// MAX PAGE LENGTH
	/// </summary>
	public const int PageLineCount = 15;

	[Property] public WorldPanel Panel { get; set; }
	public TerminalHud Hud { get; private set; }

	[Property] public GameObject Keyboard { get; set; }

	public string PageInfo => "[PAGE " + (PageNumber + 1) + "/" + PageCount + "]";

	[Property] public List<string> TextLines { get; set; } = new();
	[Property] public string TextInput { get; set; } = "";

	public int PageNumber { get; private set; } = 0;

	public int PageStartLine => PageLineCount * PageNumber;

	public int PageCount => (TextLines.Count / PageLineCount) + 1;


	public bool ShowCursor => ((int)(Time.Now * 5) % 2) == 0;

	protected override void OnAwake()
	{
		base.OnAwake();

		Hud = Panel.Components.Get<TerminalHud>();
		Hud.Owner = this;

		Keyboard.Components.Get<InteractionProxy>().OnInteracted += OnInteract;

	}

	void OnInteract(Player player)
	{
		if(IsBeingUsed) { return; }

		IsBeingUsed = true;
		Log.Info( "hey" );
		AddLine( "ass" );
	}

	public void Clear()
	{
		PageNumber = 0;
		TextLines.Clear();
	}

	public void AddLine( string line )
	{
		TextLines.Add( line );
	}

}

public class TerminalCommand
{
	public string KeyWord { get; set; }

	public TerminalCommand(string keyword)
	{
		this.KeyWord = keyword;
	}

}

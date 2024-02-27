using Saandy;
using Sandbox;
using Sandbox.UI;

public sealed class TerminalComponent : Component
{
	readonly TerminalCommand[] Commands = new TerminalCommand[]
	{
		new TerminalCommandExit("exit", "quit", "stop", "leave"),
	};

	public Player Owner { get; private set; } = default;

	/// <summary>
	/// MAX PAGE LENGTH
	/// </summary>
	public const int PageLineCount = 15;

	[Property] public Vector3 CameraPosition { get; private set; }
	[Property] public Angles CameraAngles { get; private set; }

	[Property] public Sandbox.WorldPanel Panel { get; set; }
	public TerminalHud Hud { get; private set; }

	[Property] public GameObject Keyboard { get; set; }

	public string PageInfo => "[PAGE " + (PageNumber + 1) + "/" + PageCount + "]";

	[Property] public List<string> TextLines { get; set; } = new();

	public int PageNumber { get; private set; } = 0;

	public int PageStartLine => PageLineCount * PageNumber;

	public int PageCount => (TextLines.Count / PageLineCount) + 1;


	public bool ShowCursor => ((int)(Time.Now * 5) % 2) == 0;

	protected override void DrawGizmos()
	{
		base.DrawGizmos();

		Gizmo.Draw.Color = Color.White;
		Gizmo.Draw.LineSphere( CameraPosition, 5f );
		Gizmo.Draw.Color = Color.Green;
		Gizmo.Draw.Line( CameraPosition, CameraPosition + CameraAngles.Forward * 8 );

	}

	protected override void OnAwake()
	{
		base.OnAwake();

		Network.SetOwnerTransfer(OwnerTransfer.Takeover);

		Hud = Panel.Components.Get<TerminalHud>();
		Hud.Owner = this;
		Keyboard.Components.Get<InteractionProxy>().OnInteracted += OnInteract;
		Hud.TextEntry.OnKeyPressed += OnKeyPressed;
	}

	void OnInteract(Player player)
	{

		if( player.IsProxy) { return; }

		if (Owner != null) { return; }

		Owner = player;

		Network.TakeOwnership();

		Log.Info( player + " interacted with terminal." );
		player.PlayerInput = new TerminalInput( player, this );
		Hud.Focus( true );

	}

	protected override void OnUpdate()
	{
		if(GameObject.IsProxy ) { return; }


		if ( Input.EscapePressed )
		{
			Exit( );
		}
	}

	public void ClearLines()
	{

		PageNumber = 0;
		TextLines.Clear();
	}

	public void OnKeyPressed(string button)
	{
		if(button == "enter")
		{
			OnSubmit();
		}
	}

	public void OnSubmit()
	{
		string input = Hud.TextEntry.InputText.Trim();

		float bestMatch = 0;
		TerminalCommand bestMatchCommand = null;

		// Get command that best matches input text.
		foreach(TerminalCommand command in Commands)
		{
			float currentMatch = command.GetMatch( input );

			if(currentMatch > bestMatch) { 
				bestMatch = currentMatch;
				bestMatchCommand = command;
			}

			// 100% match.
			if ( currentMatch == 1f ) { break; }

		}

		//Log.Info( bestMatch );

		Hud.TextEntry.InputText = "";

		// Don't accept command if match is very weak.
		if (bestMatch < 0.7f) { Log.Warning( "could not find command matching " + input ); return; }

		string[] parts = input.Contains( ' ' ) ? input.Substring( input.IndexOf( " " ) + 1 ).Split( ' ' ) : null;
		bestMatchCommand?.Run( this, parts );
	}

	public void Exit()
	{
		Owner.PlayerInput = new PlayerInput( Owner );
		Owner = null;
		Hud.Focus( false );
		Network.DropOwnership();
	}

	protected override void OnDisabled()
	{
		base.OnDisabled();

		Hud.Focus( false );
		Network.DropOwnership();
	}

}

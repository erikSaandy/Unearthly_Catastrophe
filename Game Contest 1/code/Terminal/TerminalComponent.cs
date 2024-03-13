using Saandy;
using Sandbox;
using Sandbox.UI;

public sealed class TerminalComponent : Component
{
	public readonly TerminalCommand[] Commands = new TerminalCommand[]
	{
		new TerminalCommandExit("exit", "quit", "stop", "leave"),
		new TerminalCommandHome("home", "back", "main"),
		new TerminalCommandMoonList("moons"),
		new TerminalCommandRoute("route"),
		new TerminalCommandShop("shop"),

		new TerminalCommandConfirm("confirm"),
		new TerminalCommandDeny("deny"),

		new TerminalCommandNextPage("next"),
	};

	public ITerminalPage CurrentPage { get; private set; }

	[Property][Category("Sound")] public SoundEvent TypingSound { get; set; }
	[Property][Category( "Sound" )] public SoundEvent TypingEnterSound { get; set; }

	public Player Owner { get; private set; } = default;

	/// <summary>
	/// MAX PAGE LENGTH
	/// </summary>
	public const int PageLineCount = 16;

	[Property] public Vector3 CameraPosition { get; private set; }
	[Property] public Angles CameraAngles { get; private set; }

	[Property] public Sandbox.WorldPanel Panel { get; set; }
	public TerminalHud Hud { get; private set; }

	[Property] public GameObject KeyboardCollider { get; set; }
	[Property] public GameObject ScreenCollider { get; set; }

	[Sync] public static int SelectedMoon { get; set; }

	public string PageInfo => "[PAGE " + (PageNumber + 1) + "/" + PageCount + "]";

	public List<string> TextLines { get; set; } = new();

	public int PageNumber { get; private set; } = 0;

	public int PageStartLine => PageLineCount * PageNumber;

	public int PageCount => (TextLines.Count / PageLineCount) + 1;

	public bool ShowCursor => ((int)(Time.Now * 5) % 2) == 0;

	[Property] public float ScreenDistance { get; set; } = 32;

	[Broadcast]
	public static void SelectMoon(int i)
	{
		TerminalComponent.SelectedMoon = i;
		LethalGameManager.Instance.Ship.Lever.ToolTipDeactivated = $"Land on {LethalGameManager.MoonDefinitions[SelectedMoon].ResourceName}";
		//LethalGameManager.Instance.Ship.Lever.IsLocked = false;
	}

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

		SelectedMoon = -1;

		Network.SetOwnerTransfer(OwnerTransfer.Takeover);

		Hud = Panel.Components.Get<TerminalHud>();
		Hud.Owner = this;
		KeyboardCollider.Components.Get<InteractionProxy>().OnInteracted += OnInteract;
		ScreenCollider.Components.Get<InteractionProxy>().OnInteracted += OnInteract;

		Hud.TextEntry.OnKeyPressed += OnKeyPressed;

		OpenPage( new TerminalPageMain() );
	}

	void OnInteract(Player player)
	{

		if( player.IsProxy) { return; }

		if (Owner != null) { return; }

		Owner = player;

		Network.TakeOwnership();

		Log.Info( player + " interacted with terminal." );
		player.PlayerInput = new PlayerTerminalInput( player, this );
		Hud.Focus( true );

	}

	protected override void OnUpdate()
	{
		if(GameObject.IsProxy ) { return; }

		// Not using terminal...
		if ( Owner == null ) { return; }

	}


	[Broadcast]
	public void OnKeyPressed(string button)
	{
		if(button == "enter")
		{
			if ( !GameObject.IsProxy ) { OnSubmit(); }

			Sound.Play( TypingEnterSound, KeyboardCollider.Transform.Position );
		}
		else
		{
			Sound.Play( TypingSound, KeyboardCollider.Transform.Position );
		}

	}

	public void OpenPage( ITerminalPage page )
	{
		PageNumber = 0;
		TextLines.Clear();

		CurrentPage = page;
		AddLinesAsync( page.GetLines() );
	}

	public void GoToNextPage()
	{
		PageNumber++;
		PageNumber = PageNumber % PageCount;
	}

	private async void AddLinesAsync( string[] lines )
	{
		for(int i = 0; i < lines.Length; i++ )
		{
			TextLines.Add( lines[i] );
			await Task.Delay( 15 );
		}
	}


	public void OnSubmit()
	{
		string input = Hud.TextEntry.InputText.Trim().ToLower();

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
		if (bestMatch < 0.7f) { Log.Info( $"could not find command matching '{input}'" ); return; }

		string[] parts = input.Contains( ' ' ) ? input.Substring( input.IndexOf( " " ) + 1 ).Split( ' ' ) : null;
		bestMatchCommand?.Run( this, parts );
	}

	public void Exit()
	{
		OpenPage( new TerminalPageMain() );
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

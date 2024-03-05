using Dungeon;
using Sandbox;
using static DungeonDefinition;

public class LethalGameManager : Component
{
	public static LethalGameManager Instance { get; set; } = null;

	public static MoonDefinition[] MoonDefinitions { get; private set; }
	public static Moon CurrentMoon { get; set; } = null;

	/// <summary>
	/// How much money does the team have?
	/// </summary>
	[Sync] public static bool IsLoading { get; set; } = false;

	/// <summary>
	/// How much money does the team have?
	/// </summary>
	[Sync] public static int Balance { get; set; }

	public static Random Random { get; private set; }

	public static Action OnLoadedMoon { get; set; }
	public static Action OnStartLoadMoon { get; set; }

	[Property] public ShipComponent Ship { get; set; }

	protected override void OnAwake()
	{
		IsLoading = false;

		base.OnAwake();

		MoonDefinitions = new[]
		{
			ResourceLibrary.Get<MoonDefinition>( "moons/kronos.moon" )
		};

		Instance = this;

		if ( GameObject.IsProxy ) { return; }

		GameObject.Network.TakeOwnership();

		Random = new Random();
		
	}

	protected override void OnStart()
	{
		if(GameObject.IsProxy) { return; }

		//Log.Info( Instance == null );

		//if(Instance == null) { Instance = this; }
		//else if(Instance != this) { this.Destroy(); }

	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		//if ( Input.Pressed( "chat" ) )
		//{
		//	LoadMoon( MoonDefinitions[0] );
		//}

	}

	[Broadcast( NetPermission.Anyone )]
	private void StartLoadMoon()
	{
		IsLoading = true;
		OnStartLoadMoon?.Invoke();
	}

	[Broadcast( NetPermission.Anyone )]
	private void LoadedMoon()
	{
		IsLoading = false;
		OnLoadedMoon.Invoke();

		Ship.Land( CurrentMoon.LandingPad );
	}

	[Broadcast( NetPermission.Anyone )]
	public void LoadSelectedMoon( )
	{
		Instance.LoadMoon( TerminalComponent.SelectedMoon );
	}

	[Broadcast(NetPermission.Anyone)]
	public void LoadMoon( MoonDefinition moon ) {

		if ( IsProxy ) return;

		Ship.Lever.IsLocked = true;
		Instance.LoadMoonAsync( moon );
	}

	private async void LoadMoonAsync(MoonDefinition moon)
	{
		Log.Info( "Loading moon " + moon.MoonPrefab + "..." );

		await Task.Delay( 250 );

		Balance -= moon.TravelCost;

		StartLoadMoon();

		await Task.Delay( 1000 );

		GameObject moonObject = SceneUtility.GetPrefabScene( ResourceLibrary.Get<PrefabFile>( moon.MoonPrefab ) ).Clone( Vector3.Zero );
		moonObject.BreakFromPrefab();
		moonObject.NetworkSpawn();

		CurrentMoon = moonObject.Components.Get<Moon>();

		DungeonGenerator.GenerateDungeon( ResourceLibrary.Get<DungeonDefinition>( moon.DungeonDefinitions[0] ) );

		LoadedMoon();

	}

	[Broadcast( NetPermission.Anyone )]
	public void StartLeaveCurrentMoon()
	{

	}

	[Broadcast( NetPermission.Anyone )]
	public void LeftCurrentMoon()
	{

	}

	[Broadcast( NetPermission.Anyone )]
	public void LeaveCurrentMoon()
	{
		if ( IsProxy ) return;

		Ship.Lever.IsLocked = true;

		LeaveCurrentMoonAsync();

	}

	private async void LeaveCurrentMoonAsync( )
	{
		Log.Info( "Leaving moon " + CurrentMoon.GameObject.Name + "..." );
		StartLeaveCurrentMoon();

		await Task.Delay( 250 );

		await Ship.TakeOff();

		CurrentMoon.GameObject.Destroy();
		CurrentMoon = null;

		LeftCurrentMoon();

	}

}

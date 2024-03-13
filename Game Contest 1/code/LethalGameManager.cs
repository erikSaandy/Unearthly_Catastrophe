using Dungeon;
using System;

public class LethalGameManager : Component
{
	public static LethalGameManager Instance { get; set; } = null;
	public IEnumerable<Player> ConnectedPlayers => Scene.Components.GetAll<Player>(find: FindMode.EnabledInSelfAndChildren).OrderBy(x => x.GetHashCode());
	public IEnumerable<Player> AlivePlayers => ConnectedPlayers.Where( x => x.LifeState == LifeState.Alive );
	public IEnumerable<Player> DeadPlayers => ConnectedPlayers.Where( x => x.LifeState == LifeState.Dead );

	public static Player GetPlayer( int i ) { return Instance.ConnectedPlayers.ElementAt( i ); }

	public static int GetNextPlayerIdAlive(int startId)
	{
		List<Player> connections = Instance.ConnectedPlayers.ToList();

		for (int i = 0; i < connections.Count; i++ )
		{
			int index = (startId + i) % connections.Count;
			Player player = connections.ElementAt( index );

			// Player alive?
			if(player != null && player.LifeState == LifeState.Alive) 
			{
				return index; 
			}
		}

		return -1;
	}

	public async void RespawnAllDeadPlayersAsync(int secondDelay)
	{
		await Task.DelayRealtime( secondDelay * 1000 );
		RespawnAllDeadPlayers();
	}

	public void RespawnAllDeadPlayers()
	{
		if ( IsProxy ) { return; }

		Log.Info( "respawn all..." );
		List<Player> deadPlayers = Instance.DeadPlayers.ToList();

		foreach ( Player player in deadPlayers )
		{
			Log.Info( player.Network.OwnerConnection.DisplayName + " respawn..." );
			player.Respawn();
		}
	}

	public void KillAllStrandedPlayers()
	{
		List<Player> alivePlayers = Instance.AlivePlayers.ToList();

		foreach ( Player player in alivePlayers )
		{
			// alive player not on ship?
			if( !Instance.Ship.Transporter.Passengers.Contains( player ) )
			{
				player.Kill();
			}
		}
	}

	[Broadcast]
	public void OnPlayerDeath( Guid playerId )
	{
		// only host.
		if(IsProxy) { return; }



		// All players are dead.
		if( Instance.AlivePlayers.Count() <= 0 && Ship.CurrentMovementState != ShipComponent.MovementState.Leaving)
		{
			Log.Info( "all palyers dead, ship now leaving." );

			// Not on a moon.
			if(CurrentMoonGuid == default)
			{
				RespawnAllDeadPlayersAsync(3);
			}

			// On a moon.
			else
			{
				LeaveCurrentMoonAsync(6);
			}
		}

	}
 
	public static MoonDefinition[] MoonDefinitions { get; private set; }

	[Sync] public Guid CurrentMoonGuid { get; set; } = default;
	public Moon CurrentMoon => (CurrentMoonGuid == default) ? null : Instance.Scene.Directory.FindByGuid( CurrentMoonGuid ).Components.Get<Moon>();

	/// <summary>
	/// How much money does the team have?
	/// </summary>
	[Sync] public static bool IsLoading { get; set; } = false;

	/// <summary>
	/// How much money does the team have?
	/// </summary>
	[Sync] public int Balance { get; set; }

	[Broadcast]
	public void AddBalance(int value)
	{
		if(IsProxy) { return; }

		Balance += value;
		Sandbox.Services.Stats.SetValue( "balance", Balance );
		Log.Info( $"added ${value} to ship balance." );
	}

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

		Random = new Random();

		if ( GameObject.IsProxy ) { return; }

		Ship = Scene.GetAllComponents<ShipComponent>().First();

		GameObject.Network.TakeOwnership();

		AddBalance( 100 );
	}

	[Broadcast]
	public static void OnPlayerConnected(Guid playerId)
	{
		if( Instance.GameObject.IsProxy ) { return; }

		// Already on moon? start spectating
		if ( Instance.CurrentMoon != null )
		{
			Player player = Instance.Scene.Directory.FindByGuid( playerId ).Components.Get<Player>();
			player.GameObject.Transform.Position = new Vector3( 0, 0, -8000 );
			player.Kill();
		}

	}

	[Broadcast]
	public static void OnPlayerDisconnected(Guid playerId)
	{
		if ( Instance.GameObject.IsProxy ) { return; }

		Instance.OnPlayerDeath(playerId);
	}

	protected override void OnStart()
	{

		if (Instance.GameObject.IsProxy) { return; }

		TerminalComponent.SelectMoon( 0 );

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

		if ( Instance.GameObject.IsProxy ) { return; }

	}

	[Broadcast( NetPermission.Anyone )]
	public void StartLoadMoon()
	{
		IsLoading = true;
		OnStartLoadMoon?.Invoke();
	}

	[Broadcast( NetPermission.Anyone )]
	public void LoadedMoon()
	{

		IsLoading = false;
		OnLoadedMoon?.Invoke();

		Log.Info( "[ Loaded Moon Successfully ]" );

		if ( Instance.GameObject.IsProxy ) return;

		// Land ship
		Ship.Land( CurrentMoon.LandingPad );

		// Start timer
		MoonTimerComponent.Instance.StartTimer( 400, delegate { LeaveCurrentMoon(); } );
	}


	[Broadcast( NetPermission.Anyone )]
	public void LoadSelectedMoon( )
	{
		if ( Instance.GameObject.IsProxy ) { return; }

		Log.Info( "Selected moon: " + TerminalComponent.SelectedMoon );
		Instance.LoadMoon( TerminalComponent.SelectedMoon );
	}

	[Broadcast(NetPermission.Anyone)]
	public void LoadMoon( int moon )
	{

		//Ship.Lever.IsLocked = true;

		if ( Instance.GameObject.IsProxy ) { return; }

		RespawnAllDeadPlayers();

		Instance.LoadMoonAsync( moon );

	}

	private async void LoadMoonAsync( int moonId )
	{

		if ( Instance.GameObject.IsProxy ) { return; }

		MoonDefinition moon = LethalGameManager.MoonDefinitions[moonId];

		Log.Info( "Loading moon " + moon.MoonPrefab + "..." );

		await Task.Delay( 250 );
		StartLoadMoon();

		Balance -= moon.TravelCost;

		await Task.Delay( 1000 );

		GameObject moonObject = SceneUtility.GetPrefabScene( ResourceLibrary.Get<PrefabFile>( moon.MoonPrefab ) ).Clone( Vector3.Zero );
		moonObject.BreakFromPrefab();
		moonObject.NetworkSpawn();

		CurrentMoonGuid = moonObject.Id;

		await DungeonGenerator.GenerateDungeon( ResourceLibrary.Get<DungeonDefinition>( moon.DungeonDefinitions[0] ) );

		Log.Info( "Generating navmesh.." );
		await Instance.Scene.NavMesh.Generate( Scene.PhysicsWorld );

		LoadedMoon();

	}

	[Broadcast( NetPermission.Anyone )]
	private void StartLeaveCurrentMoon()
	{

	}

	[Broadcast( NetPermission.Anyone )]
	private void LeftCurrentMoon()
	{

	}

	[Broadcast( NetPermission.Anyone )]
	public void LeaveCurrentMoon()
	{
		if ( IsProxy ) return;

		LeaveCurrentMoonAsync( 1 );

	}

	private async void LeaveCurrentMoonAsync( float secondDelay = 0f )
	{
		if ( IsProxy ) return;

		if ( CurrentMoon == null ) { Log.Error( "Can't leave moon as we are not currently on a moon." ); return; }

		MoonTimerComponent.Instance.StopTimer();

		//Ship.Lever.IsLocked = true;
		StartLeaveCurrentMoon();

		await Task.DelayRealtimeSeconds( secondDelay );

		Log.Info( "Leaving moon " + CurrentMoon.GameObject.Name + "..." );

		await Task.Delay( 250 );

		await Ship.FlyIntoSpaceLol();

		Ship.Doors.Lock();

		KillAllStrandedPlayers();

		await Task.DelayRealtimeSeconds( 3 );

		CurrentMoon.GameObject.Destroy();
		CurrentMoonGuid = default;

		MonsterManager.DeleteAllMonsters();

		await Task.DelayRealtimeSeconds( 1 );

		RespawnAllDeadPlayers();

		LeftCurrentMoon();

	}

}

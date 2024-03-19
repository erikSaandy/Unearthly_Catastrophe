using Dungeon;
using Sandbox.UI;
using System;
using System.Threading.Tasks;

public class LethalGameManager : Component, Component.INetworkListener
{
	public static LethalGameManager Instance { get; set; } = null;
	public IEnumerable<Player> ConnectedPlayers => Scene.Components.GetAll<Player>( find: FindMode.EverythingInChildren );
	public IEnumerable<Player> AlivePlayers => ConnectedPlayers?.Where( x => x.LifeState == LifeState.Alive );
	public IEnumerable<Player> DeadPlayers => ConnectedPlayers?.Where( x => x.LifeState == LifeState.Dead );

	public static Player GetPlayer( int i ) { 
		IEnumerable<Player> connections = Instance?.ConnectedPlayers; 
		if(i < 0) { i = connections.Count() - 1; }
		return connections.ElementAt( i % connections.Count() ); 
	}

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

		List<Player> deadPlayers = Instance.DeadPlayers.ToList();

		foreach ( Player player in deadPlayers )
		{

			Log.Info( "Respawning " + player.Network.OwnerConnection.DisplayName + "..." );
			player.Respawn();
		}
	}

	public void KillAllStrandedPlayers()
	{
		List<Player> alivePlayers = Instance.AlivePlayers.ToList();

		foreach ( Player player in alivePlayers )
		{
			if ( player == null ) { continue; }

			// alive player not on ship?
			if ( !Instance.Ship.Transporter.Passengers.Contains( player ) || Vector3.DistanceBetween(player.Transform.Position, Instance.Ship.Transform.Position) > 500 )
			{
				player.Kill();
			}
		}
	}


	private int OnPlayerDeathQueue { get; set; } = 0;
	[Broadcast]
	public void QueueOnPlayerDeath()
	{
		// only host.
		if ( IsProxy ) { return; }
		OnPlayerDeathQueue++;
	}

	[Broadcast]
	private void OnPlayerDeath()
	{
		// only host.
		if(IsProxy) { return; }

		// If leaving moon, players will be respawned soon. Don't do anything.
		if ( Ship.CurrentMovementState != ShipComponent.MovementState.Leaving)
		{

			Log.Info( "checking for alive players..." );

			// Not on a moon.
			if (CurrentMoonGuid == default)
			{
				RespawnAllDeadPlayersAsync(3);
			}

			// On a moon, and all players are dead
			else if( Instance.AlivePlayers.Count() <= 0 )
			{
				OnPlayerDeathQueue = 0;
				Log.Info( "all players dead." );
				LeaveCurrentMoonAsync(3);
				OnLostGame();
			}
		}

	}

	[Broadcast] //[Authority] 
	private void OnLostGame()
	{
		if ( IsProxy ) { return; }

		List<Scrap> shipScrap = Instance.Ship.Components.GetAll<Scrap>( FindMode.InChildren ).ToList();
		for(int i = 0; i < shipScrap.Count; i++ )
		{
			shipScrap[i]?.GameObject?.Destroy();
		}

		Balance = 100;

	}
 
	[Property] public List<MoonDefinition> MoonDefinitions { get; set; }

	[Sync] public int SelectedMoon { get; set; } = -1;
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
	public void AddBalance(int value, string scrapName = "")
	{
		if(IsProxy) { return; }

		Balance += value;
		Sandbox.Services.Stats.SetValue( "balance", Balance );
		Log.Info( $"added ${value} to ship balance." );

		if(scrapName != string.Empty)
		{
			InfoBox.SendInfo( $"Added {scrapName} [ ${value} ]", InfoBox.EntryType.Balance, 2f );
		}
	}

	public static Random Random { get; private set; }

	public static Action OnLoadedMoon { get; set; }
	public static Action OnStartLoadMoon { get; set; }

	[Property] public ShipComponent Ship { get; set; }

	protected override void OnAwake()
	{
		IsLoading = false;

		base.OnAwake();

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

	public void OnDisconnected( Connection channel )
	{
		if ( Instance.IsProxy ) { return; }

		Instance.QueueOnPlayerDeath();

	}

	public void OnBecameHost( Connection previousHost )
	{
		Instance.Network.TakeOwnership();
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

		if ( OnPlayerDeathQueue > 0 )
		{
			OnPlayerDeathQueue--;
			OnPlayerDeath();
		}

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
		InfoBox.SendInfo( $"Landing on {CurrentMoon.Definition.Name}. {CurrentMoon.Definition.Description}", InfoBox.EntryType.Info );

		// Start timer
		MoonTimerComponent.Instance.StartTimer( 
			600,
			onFinish: LeaveCurrentMoon,
			onWarning: delegate { InfoBox.SendInfo( $"Warning! The ship will be leaving in {MoonTimerComponent.WARNING_TIME} seconds. Don't get stranded.", InfoBox.EntryType.Warning ); } 
		);

	}


	[Broadcast( NetPermission.Anyone )]
	public void LoadSelectedMoon( )
	{
		if ( Instance.GameObject.IsProxy ) { return; }

		Log.Info( "Selected moon: " + SelectedMoon );
		Instance.LoadMoon( SelectedMoon );
	}

	[Broadcast(NetPermission.Anyone)]
	public void LoadMoon( int moon )
	{

		//Ship.Lever.IsLocked = true;

		if ( Instance.GameObject.IsProxy ) { return; }

		//RespawnAllDeadPlayers();

		Instance.LoadMoonAsync( moon );

	}

	private async void LoadMoonAsync( int moonId )
	{

		if ( Instance.GameObject.IsProxy ) { return; }

		MoonDefinition moon = LethalGameManager.Instance.MoonDefinitions[moonId];

		Log.Info( "Loading moon " + moon.MoonPrefab + "..." );

		StartLoadMoon();
		await Task.Delay( 250 );

		Balance -= moon.TravelCost;

		GameObject moonObject = SceneUtility.GetPrefabScene( ResourceLibrary.Get<PrefabFile>( moon.MoonPrefab ) ).Clone( Vector3.Zero );
		moonObject.BreakFromPrefab();
		moonObject.NetworkSpawn();

		CurrentMoonGuid = moonObject.Id;

		await Task.Delay( 500 );

		await DungeonGenerator.GenerateDungeon( ResourceLibrary.Get<DungeonDefinition>( moon.DungeonDefinitions[0] ) );

		await Task.Delay( 500 );

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
		InfoBox.SendInfo( $"Ship is now leaving. Have a nice trip!", InfoBox.EntryType.Info );

		//Ship.Lever.IsLocked = true;
		StartLeaveCurrentMoon();

		await Task.DelayRealtimeSeconds( secondDelay );

		Log.Info( "Leaving moon " + CurrentMoon.GameObject.Name + "..." );

		await Task.Delay( 250 );

		await Ship.FlyIntoSpace();

		Ship.Doors.Lock();

		KillAllStrandedPlayers();

		await Task.DelayRealtimeSeconds( 1 );

		RespawnAllDeadPlayers();

		await Task.DelayRealtimeSeconds( 2 );

		CurrentMoon.GameObject.Destroy();
		CurrentMoonGuid = default;

		MonsterManager.DeleteAllMonsters();

		await Task.DelayRealtimeSeconds( 1 );

		// Respawn again for safety. Someone might have been dumb and died since last respawn.
		RespawnAllDeadPlayers();

		LeftCurrentMoon();

	}

}

using Dungeon;
using System;

public class LethalGameManager : Component
{
	public static LethalGameManager Instance { get; set; } = null;
	[Sync] public NetList<Guid> ConnectedPlayers { get; private set; }

	[Sync] public int AlivePlayers { get; set; } = 0;

	public static Player GetPlayer( int i ) 
	{
		return Instance.Scene.Directory.FindByGuid( Instance.ConnectedPlayers[i] ).Components.Get<Player>();

	}

	public static int GetNextPlayerIdAlive(int startId)
	{
		for(int i = 0; i < Instance.ConnectedPlayers.Count; i++ )
		{
			int index = (startId + i) % Instance.ConnectedPlayers.Count;
			Player player = Instance.Scene.Directory.FindByGuid( Instance.ConnectedPlayers[index] )?.Components.Get<Player>();

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
		foreach ( Guid playerId in ConnectedPlayers )
		{
			Player player = Instance.Scene.Directory.FindByGuid( playerId ).Components.Get<Player>();
			if ( player.LifeState == LifeState.Dead )
			{
				AlivePlayers++;
				player.Respawn();
			}
		}
	}

	public void KillAllStrandedPlayers()
	{
		foreach ( Guid playerId in ConnectedPlayers )
		{

			if( Vector3.DistanceBetween( Transform.Position, LethalGameManager.Instance.Ship.Transform.Position ) > 800f )
			{
				Player player = Instance.Scene.Directory.FindByGuid( playerId ).Components.Get<Player>();

				if ( player.LifeState == LifeState.Alive )
				{
					player.Kill();
				}
			}
		}
	}

	[Broadcast]
	public void OnPlayerDeath( Guid playerId )
	{
		// only host.
		if(IsProxy) { return; }

		AlivePlayers--;

		// All players are dead.
		if( AlivePlayers <= 0)
		{

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
	}

	public static Random Random { get; private set; }

	public static Action OnLoadedMoon { get; set; }
	public static Action OnStartLoadMoon { get; set; }

	[Property] public ShipComponent Ship { get; set; }

	protected override void OnAwake()
	{
		IsLoading = false;

		base.OnAwake();

		ConnectedPlayers = new NetList<Guid>();

		MoonDefinitions = new[]
		{
			ResourceLibrary.Get<MoonDefinition>( "moons/kronos.moon" )
		};

		Instance = this;

		if ( GameObject.IsProxy ) { return; }

		GameObject.Network.TakeOwnership();

		Random = new Random();

		AddBalance( 100 );
	}

	[Broadcast]
	public static void OnPlayerConnected(Guid playerId)
	{
		if(Instance.IsProxy) { return; }

		Instance.ConnectedPlayers.Add( playerId );
		Instance.AlivePlayers++;

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
		if ( Instance.IsProxy ) { return; }

		Instance.ConnectedPlayers.Remove( playerId );
		Instance.OnPlayerDeath(playerId);
	}

	protected override void OnStart()
	{
		if(GameObject.IsProxy) { return; }

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
		OnLoadedMoon?.Invoke();

		if ( GameObject.IsProxy ) return;

		Ship.Land( CurrentMoon.LandingPad );
	}


	[Broadcast( NetPermission.Anyone )]
	public void LoadSelectedMoon( )
	{
		Instance.LoadMoon( TerminalComponent.SelectedMoon );
	}

	[Broadcast(NetPermission.Anyone)]
	public void LoadMoon( int moon )
	{
		Ship.Lever.IsLocked = true;

		if ( IsProxy ) { return; }

		RespawnAllDeadPlayers();

		Instance.LoadMoonAsync( moon );

	}

	private async void LoadMoonAsync( int moonId )
	{
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

		LoadedMoon();

	}

	[Broadcast( NetPermission.Anyone )]
	public void StartLeaveCurrentMoon()
	{

	}

	[Broadcast( NetPermission.Anyone )]
	public void LeftCurrentMoon()
	{
		RespawnAllDeadPlayers();
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

		Ship.Lever.IsLocked = true;

		await Task.DelayRealtimeSeconds( secondDelay );

		Log.Info( "Leaving moon " + CurrentMoon.GameObject.Name + "..." );
		StartLeaveCurrentMoon();

		await Task.Delay( 250 );

		await Ship.FlyIntoSpaceLol();

		Ship.Doors.Lock();

		await Task.DelayRealtimeSeconds( 3 );

		CurrentMoon.GameObject.Destroy();
		CurrentMoonGuid = default;

		await Task.DelayRealtimeSeconds( 1 );

		RespawnAllDeadPlayers();

		LeftCurrentMoon();

	}

}

using Saandy;
using System.Diagnostics;

public class PlayerSpectateInput : PlayerInput
{
	public Angles EyeAngles { get; set; } = Angles.Zero;

	public CameraComponent Camera => Owner.Camera;

	private int SpectatedPlayerId { get; set; } = -1;

	private Player SpectatedPlayer { get; set; } = null;

	public PlayerSpectateInput(Player owner ) : base( owner ) {
		this.Owner = owner;

		owner.Voice.Volume = 0;

		CycleNextAlivePlayer();
	}

	public override void UpdateInput( )
	{
		AnalogMove = 0;
		WantsToRun = false;

		// Just died, spectate corpse.
		if(Owner.TimeSinceDeath < 1 )
		{
			SpectatedPlayer = Owner;
			return;
		}
		else if(SpectatedPlayer == Owner)
		{
			CycleNextAlivePlayer();
		}

		// Cycle alive players.
		if ( Input.Pressed( "attack1" ) )
		{
			//Log.Info( LethalGameManager.Instance.ConnectedPlayers.Count );
			CycleNextAlivePlayer();
		}

		// Spectate any alive player
		else if ( SpectatedPlayer != null )
		{
			if(SpectatedPlayer.LifeState == LifeState.Dead)
			{
				Log.Info( "player you spectated died." );
				CycleNextAlivePlayer();
				Log.Info( SpectatedPlayerId );
			}
		}


		// No player to spectate...
		else
		{

		}

	}

	private void CycleNextAlivePlayer()
	{
		SpectatedPlayer = null;
		SpectatedPlayerId++;
		SpectatedPlayerId = LethalGameManager.GetNextPlayerIdAlive( SpectatedPlayerId );

		if(SpectatedPlayerId != -1 )
		{
			Log.Info( "found alive player to spectate." );
			SpectatedPlayer = LethalGameManager.GetPlayer( SpectatedPlayerId );
		}
	}

	public override void OnJump( )
	{
	}

	public override void CameraInput()
	{

		EyeAngles += Sandbox.Input.AnalogLook;
		EyeAngles = EyeAngles.WithPitch( Math.Clamp( EyeAngles.pitch, Owner.CameraController.MinPitch, Owner.CameraController.MaxPitch ) );

		Camera.Transform.Rotation = EyeAngles.ToRotation();

		GameObject follow = null;
		float followDistance = 100;

		if(SpectatedPlayer == null)
		{
			follow = LethalGameManager.Instance.Ship.GameObject;
			followDistance = 750;
		}
		else
		{
			follow = SpectatedPlayer.HeadBone;
		}

		if( follow != null)
		{
			SceneTraceResult trace = Owner.Scene.Trace.Ray( follow.Transform.Position, (follow.Transform.Position - EyeAngles.Forward * followDistance) )
				.IgnoreGameObjectHierarchy( follow )
				.WithoutTags( "owned" )
				.Size( 8f )
				.Run();

			Camera.Transform.Position = trace.EndPosition;
		}
	}

	public override void InventoryInput()
	{
	}

}

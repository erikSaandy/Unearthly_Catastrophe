using Saandy;
using Sandbox;
using System.Numerics;

public class DoorEntranceComponent : Component, IInteractable
{

	[Property] public SoundEvent EnterDoorSound { get; set; }

	public bool IsInteractableBy( Player player ) { return true; }
	public float InteractionTime { get { return IsLocked ? 0 : 1.1f; } }
	public string ToolTip { get; set; } = "";

	public virtual string GetToolTip( Player player )
	{
		if ( IsLocked ) { return "[ Locked ]"; }

		return $"{IInteractable.GetInteractionKey()} - Enter";
	}


	public bool IsOpen { get; private set; } = false;
	[Property] public bool IsLocked { get; set; } = false;

	protected override void DrawGizmos()
	{
		base.DrawGizmos();

	}

	protected override void OnStart()
	{
		base.OnStart();

		GameObject.Tags.Add( "door_entrance" );

		if(GameObject.IsPrefabInstance)
		{
			GameObject.BreakFromPrefab();
		}

		if ( GameObject.IsProxy ) { return; }

	}

	public void OnInteract( Guid playerId )
	{
		GoToConnectedDoor( playerId );
	}

	private void GoToConnectedDoor( Guid playerId )
	{
		if ( IsLocked ) { return; }

		//Log.Info( Scene.Directory.FindByGuid( playerId ).Network.OwnerConnection.DisplayName + " going into entrance door." );
		List<DoorEntranceComponent> entranceDoors = Scene.GetAllComponents<DoorEntranceComponent>().ToList<DoorEntranceComponent>();

		for ( int i = 0; i < entranceDoors.Count; i++ )
		{
			//Log.Info( entranceDoors[i].Root );
			if ( entranceDoors[i].GameObject != GameObject )
			{
				GoTo( entranceDoors[i].GameObject.Id, playerId );
				return;
			}
		}
	}


	[Broadcast]
	public void GoTo( Guid doorId, Guid playerId)
	{
		GameObject to = GameObject.Scene.Directory.FindByGuid( doorId );
		Sound.Play( EnterDoorSound, to.Transform.Position );
		Sound.Play( EnterDoorSound, Transform.Position );

		GameObject playerObj = GameObject.Scene.Directory.FindByGuid( playerId );

		if(playerObj.IsProxy) { return; }

		Player player = playerObj?.Components.Get<Player>();
		
		Angles angleDelta = to.Transform.Rotation.Angles() - Transform.Rotation.Angles();
		float yaw = angleDelta.yaw;

		Angles rotation = new( 0, player.EyeAngles.yaw + angleDelta.yaw + 180, 0 );
		player.TeleportTo( to.Transform.Position + ( Vector3.Up * 8 ) + ( to.Transform.Rotation.Left * 32 ) , rotation );

	}

}

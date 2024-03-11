using Saandy;
using Sandbox;
using System.Numerics;

public class DoorEntranceComponent : Component, IInteractable
{
	private static List<DoorEntranceComponent> EntranceDoors { get; set; } = new();

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

	protected override void OnAwake()
	{
		base.OnAwake();

		if(EntranceDoors.Count >= 2) { EntranceDoors.RemoveAt( 0 ); }
		EntranceDoors.Add(this);

	}

	public void OnInteract( Guid playerId )
	{

		if (IsLocked) { return; }

		for(int i = 0; i < EntranceDoors.Count; i++ )
		{
			if ( EntranceDoors[i] != this )
			{
				GoTo( i, playerId );
				return;
			}
		}
	}

	[Broadcast]
	public void GoTo(int doorId, Guid playerId)
	{
		DoorEntranceComponent to = EntranceDoors[doorId];
		Sound.Play( EnterDoorSound, to.Transform.Position );
		Sound.Play( EnterDoorSound, Transform.Position );

		if (IsProxy) { return; }

		Player player = GameObject.Scene.Directory.FindByGuid( playerId )?.Components.Get<Player>();
		player.Transform.Position = to.Transform.Position + to.Transform.LocalRotation.Left * 32;
		
		Angles angleDelta = to.Transform.Rotation.Angles() - Transform.Rotation.Angles();
		float yaw = angleDelta.yaw;
		Log.Info( angleDelta );

		player.EyeAngles = new Angles(0, player.EyeAngles.yaw + angleDelta.yaw + 180, 0);// player.EyeAngles. .RotateAroundAxis(Vector3.Up, angleDelta.yaw + 180 );
	}

}

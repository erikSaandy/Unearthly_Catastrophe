using Saandy;
using Sandbox;
using System.Numerics;

public class DoorEntranceComponent : Component, IInteractable
{
	private static List<DoorEntranceComponent> EntranceDoors { get; set; } = new();


	public bool IsInteractableBy( Player player ) { return !IsLocked; }
	public float InteractionTime { get; set; } = 1.1f;
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
		Player player = GameObject.Scene.Directory.FindByGuid( playerId )?.Components.Get<Player>();

		if (IsLocked) { return; }

		for(int i = 0; i < EntranceDoors.Count; i++ )
		{
			if ( EntranceDoors[i] != this )
			{
				GoTo( i, player );
				return;
			}
		}
	}

	public void GoTo(int doorId, Player player)
	{
		DoorEntranceComponent to = EntranceDoors[doorId];
		player.Transform.Position = to.Transform.Position + to.Transform.LocalRotation.Left * 32;
	}

}

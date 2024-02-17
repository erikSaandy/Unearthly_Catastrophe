
using Sandbox;
using System.Diagnostics;

public sealed class Inventory : Component
{
	[Property] public Player Owner { get; private set; }

	protected override void OnAwake()
	{
		base.OnAwake();
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		if ( !Input.Pressed( "use" ) ) { return; }

		var from = Owner.Camera.Transform.Position;
		var to = from + Owner.Camera.Transform.Rotation.Forward * 80;
		SceneTraceResult trace = Scene.Trace.Ray( from, to ).IgnoreGameObjectHierarchy(GameObject).UseHitboxes().Size(5f).Run();

		if(trace.Hit)
		{
			trace.GameObject.Components.TryGet<Pickup>(out Pickup pickup);
			if(pickup != null) {
				pickup.OnPickup( Owner );
				Pickup( pickup );
			}
		}

	}

	public void Pickup(Pickup pickup)
	{



	}

}



using Sandbox;
using Sandbox.UI;
using System.Diagnostics;

public partial class Inventory : Panel
{
	[Property] public Player Owner { get; private set; }

	public int ActiveSlot { get; set; } = 0;
	public Slot[] Slots { get; private set; }

	public Inventory() { }

	public Inventory( Player owner, int size = 4 )
	{
		this.Owner = owner;
		Slots = new Slot[size];
	}

	public void OnUpdate()
	{

		if ( !Input.Pressed( "use" ) ) { return; }

		var from = Owner.Camera.Transform.Position;
		var to = from + Owner.Camera.Transform.Rotation.Forward * 80;
		SceneTraceResult trace = Scene.Trace.Ray( from, to ).IgnoreGameObjectHierarchy(Owner.GameObject).UseHitboxes().Size(5f).Run();

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

	public struct Slot
	{
		public ICarriable Item { get; set; }
	}

}


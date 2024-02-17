
public abstract class Pickup : Component
{
	[Property] public Collider Collider { get; set; }
	[Property] public GameObject Handle { get; set; }

	public abstract void OnPickup( Player player );
}


public abstract class Pickup : Component, ICarriable
{
	[Property] public Collider Collider { get; set; }
	[Property] public GameObject Handle { get; set; }

	[Property] public Texture Icon { get; set; }

	public abstract void OnPickup( Player player );

}

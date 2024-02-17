public sealed class Scrap : Pickup
{
	public override void OnPickup( Player player )
	{
		Log.Warning( "PICKUP" );
	}

}

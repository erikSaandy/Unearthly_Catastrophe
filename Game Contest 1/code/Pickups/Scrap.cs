public sealed class Scrap : Pickup
{

	public int Value { get; private set; }
	[Property] private int MinValue { get; set; } = 20;
	[Property] private int MaxValue { get; set; } = 30;

	protected override void OnStart()
	{
		Value = Game.Random.Next( MinValue, MaxValue );
	}

	public override void OnPickup( Player player )
	{
		Log.Warning( "PICKUP" );
	}

}

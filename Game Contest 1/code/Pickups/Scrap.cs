public sealed class Scrap : Carriable
{

	public int Value { get; private set; }
	[Property] private int MinValue { get; set; } = 20;
	[Property] private int MaxValue { get; set; } = 30;

	protected override void OnStart()
	{
		Value = Game.Random.Next( MinValue, MaxValue );
	}

	public override void OnDrop()
	{
		base.OnDrop();



	}

	public override void OnUsePrimary()
	{
		base.OnDrop();
	
		

	}

	public override void OnUseSecondary()
	{
		base.OnDrop();



	}
}

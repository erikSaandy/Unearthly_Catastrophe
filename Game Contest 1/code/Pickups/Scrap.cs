public sealed class Scrap : Carriable, ISellable
{
	public int MinValue { get; set; }
	public int MaxValue { get; set; }
	public int Value { get; set; }

	public override string ToolTip { get; set; }

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

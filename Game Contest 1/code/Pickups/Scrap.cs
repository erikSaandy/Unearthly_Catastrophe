public sealed class Scrap : Carriable, ISellable
{
	[Property][Range(0, 300)] public int MinValue { get; set; }
	[Property][Range( 0, 300 )] public int MaxValue { get; set; }
	[Sync] public int Value { get; set; }

	[Property] public string Name { get; set; } = "";
	public override string ToolTip { get; set; }

	public override string GetToolTip( Player player ) { return $"{IInteractable.GetInteractionKey()} - Pickup " + Name; }

	protected override void OnStart()
	{
		base.OnStart();

		if(IsProxy) { return; }

		Value = Game.Random.Next( MinValue, MaxValue );
	}

	public override void OnDrop()
	{
		base.OnDrop();

	}

	public override void OnUsePrimary()
	{

	}

	public override void OnUseSecondary()
	{

	}
}

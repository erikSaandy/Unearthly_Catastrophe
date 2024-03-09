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
		if ( !IsProxy ) {
			Value = Game.Random.Next( MinValue, MaxValue );
		}

		base.OnStart();
	}

	public override void OnDrop()
	{
		base.OnDrop();

	}

	protected override void OnDropOnGround( SceneTraceResult result )
	{

		if(IsProxy) { return; }

		if(Value == 0) { return; }

		// Added scrap to ship
		if(result.GameObject == LethalGameManager.Instance.Ship.GameObject)
		{
			LethalGameManager.Instance.AddBalance( Value );
			Value = 0;
		}

	}


	public override void OnUsePrimary()
	{

	}

	public override void OnUseSecondary()
	{

	}
}

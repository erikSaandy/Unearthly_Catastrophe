public sealed class Scrap : Carriable, ISellable, IHasMapIcon
{
	[Property][Range(0, 300)] public int MinValue { get; set; }
	[Property][Range( 0, 300 )] public int MaxValue { get; set; }
	[Sync] public int Value { get; set; }
	public int StartValue { get; set; }

	[Property] public string Name { get; set; } = "";
	public override string ToolTip { get; set; }

	[Property] public Action OnUsePrimary;
	[Property] public Action OnUseSecondary;

	public Color IconColor => Color.Blue;
	public float IconRotation => Transform.Rotation.Angles().yaw;

	public void RenderMapIcon() { MiniMapComponent.AddIcon( this ); }

	public override string GetToolTip( Player player ) { return $"{IInteractable.GetInteractionKey()} - Pickup {Name} [${StartValue}]"; }

	protected override void OnStart()
	{
		if ( !IsProxy ) {
			StartValue = Game.Random.Next( MinValue, MaxValue );
			Value = StartValue;
		}

		base.OnStart();
	}

	protected override void OnFixedUpdate()
	{
		base.OnUpdate();

		RenderMapIcon();
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
		if(result.GameObject.Root == LethalGameManager.Instance.Ship.GameObject)
		{
			LethalGameManager.Instance.AddBalance( Value, Name );
			Value = 0;
		}

	}

	[Broadcast]
	public override void UsePrimary()
	{
		OnUsePrimary?.Invoke();
	}

	[Broadcast]
	public override void UseSecondary()
	{
		OnUseSecondary?.Invoke();
	}
}

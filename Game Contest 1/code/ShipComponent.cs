using Sandbox;

public sealed class ShipComponent : Component
{
	protected override void OnAwake()
	{
		base.OnAwake();

		GameObject.BreakFromPrefab();

	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

	}



}

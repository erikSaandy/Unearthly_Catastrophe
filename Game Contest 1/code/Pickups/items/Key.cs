using Sandbox;
using Sandbox.Citizen;
using System.Numerics;

public sealed class Key : Carriable
{
	public override string ToolTip { get; set; } = "Pickup Key";

	public int ShopPrice { get; set; } = 10;

	public override void UsePrimary()	{ }
	public override void UseSecondary() { }

	[Broadcast]
	public override void WasUsedOn( Guid interactableObject )
	{
		if(IsProxy) { return; }

		DoorComponent door = null;
		Scene.Directory.FindByGuid( interactableObject ).Components.TryGet<DoorComponent>( out door );

		if( door != null )
		{
			if ( door.IsLocked )
			{
				door.Unlock();
				Owner?.Inventory.Drop( this );
				OnUnlock();
			}
		}
	}

	[Broadcast]
	private void OnUnlock()
	{
		GameObject.Destroy();

	}

}

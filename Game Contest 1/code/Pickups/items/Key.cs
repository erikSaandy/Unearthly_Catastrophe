using Sandbox;
using Sandbox.Citizen;
using System.Numerics;

public sealed class Key : Carriable
{
	public override string ToolTip { get; set; } = "Pickup Key";

	public int ShopPrice { get; set; } = 10;

	public override void OnUsePrimary()	{ }
	public override void OnUseSecondary() { }

	[Property] public SoundEvent UnlockSound { get; private set; }

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
				OnUnlock();
				door.IsLocked = false;
				Owner?.Inventory.Drop( this );
				GameObject.Destroy();
			}
		}
	}

	[Broadcast]
	private void OnUnlock()
	{
		Sound.Play( UnlockSound, Transform.Position );
	}

}

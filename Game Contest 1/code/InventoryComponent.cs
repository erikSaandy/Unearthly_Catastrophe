public sealed class InventoryComponent : Component
{
	[Property] public Player Owner { get; private set; }
	[Property] public int Size { get; private set; } = 4;

	[Sync] public int ActiveSlot { get; set; } = 0;
	public Carriable ActiveItem => Items[ActiveSlot];
	public Carriable[] Items { get; private set; }

	public int Weight { get; private set; }

	protected override void OnAwake()	
	{
		if ( GameObject.IsProxy ) { return; }

		Items = new Carriable[Size];
		for(int i = 0; i < Items.Length; i++ ) { Items[i] = null; }
	}


	protected override void OnUpdate()
	{

		if ( GameObject.IsProxy ) { return; }

		ActiveItem?.UpdateHeldPosition();

		Owner.PlayerInput?.InventoryInput();

	}

	public bool TryPickup(Carriable carriable, out int slotId)
	{
		slotId = -1;

		for(int i = 0; i < Items.Length; i++ )
		{
			if ( Items[i] == null )
			{
				slotId = i;
				return TryPickup( carriable, i );
			}
		}

		return false;
	}

	private bool TryPickup(Carriable carriable, int slotId)
	{
		if ( carriable == null ) { return false; }
		if ( Items[slotId] != null ) { Log.Info( $"can't add item to slot {slotId}, as slot is already used." ); return false; }

		Weight += carriable.Weight;
		Items[slotId] = carriable;

		return true;

	}

	public void DropActive() => Drop( ActiveSlot );

	public void Drop( int slotId )
	{
		if ( Items[slotId] == null ) { Log.Info( $"can't drop null item from slot {slotId}." ); return; }

		Weight -= Items[slotId].Weight;
		Items[slotId].OnDrop();
		Items[slotId] = null;

		Owner.Animator.HoldType = Sandbox.Citizen.CitizenAnimationHelper.HoldTypes.None;
	}



}


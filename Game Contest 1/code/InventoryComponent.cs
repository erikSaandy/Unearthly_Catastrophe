
using Saandy;
using Sandbox;
using Sandbox.UI;
using System.Diagnostics;
using System.Drawing;
using System.Net.Mail;
using System.Numerics;

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

		UpdateHeldPosition();

		if ( Input.Pressed( "use" ) )
		{

			var from = Owner.CameraController.Camera.Transform.Position;
			var to = from + Owner.Camera.Transform.Rotation.Forward * 70;
			IEnumerable<SceneTraceResult> trace = Scene.Trace.Ray( from, to ).IgnoreGameObjectHierarchy( Owner.GameObject ).WithoutTags("owned").UseRenderMeshes().Size( 12f ).RunAll();

			Carriable carriable = null;
			foreach ( SceneTraceResult item in trace )
			{
				item.GameObject.Components.TryGet( out carriable );
				if ( carriable != null )
				{
					Pickup( carriable );
					break;
				}
			}

		}

		// Inventory scroll
		if ( Input.MouseWheel.y != 0 )
		{
			int slot = ActiveSlot;
			slot = (ActiveSlot - Math.Sign( Input.MouseWheel.y )) % Items.Length;
			if ( slot < 0 ) { slot += Items.Length; }
			ActiveItem?.Undeploy();
			ActiveSlot = slot;
			ActiveItem?.Deploy();
		}

		if (Input.Pressed("attack1"))
		{
			ActiveItem?.OnUsePrimary();
		}

		if ( Input.Pressed( "attack2" ) )
		{
			ActiveItem?.OnUseSecondary();
		}

		if(Input.Pressed("drop"))
		{
			DropActive();
		}

	}

	public void UpdateHeldPosition()
	{
		if ( ActiveItem == null ) {
			return; 
		}

		//Transform hands = Owner.Animator.Target.GetAttachment( "middle_of_both_hands" ) ?? default;
		//ActiveItem.Transform.Position = hands.Position;
		//ActiveItem.Transform.Rotation = hands.Rotation * ActiveItem.HeldAngleOffset;

		Vector3 hands = (Owner.HandRBone.Transform.Position + Owner.HandLBone.Transform.Position) * 0.5f;
		ActiveItem.Transform.Position = hands;
		ActiveItem.Transform.Rotation = Owner.HandRBone.Transform.Rotation * ActiveItem.HeldAngleOffset;
	}

	public void Pickup(Carriable carriable)
	{
		for(int i = 0; i < Items.Length; i++ )
		{
			if ( Items[i] == null )
			{
				Pickup( carriable, i );
				return;
			}
		}
	}

	public void Pickup(Carriable carriable, int slotId)
	{

		if ( Items[slotId] != null ) { Log.Info( $"can't add item to slot {slotId}, as slot is already used." ); return; }
		if( carriable == null ) { return; }
		if ( carriable.Owner != null ) { Log.Info( $"Can't pick up owned item!" ); return; }

		carriable.OnPickup( Owner );
		Weight += carriable.Weight;
		Items[slotId] = carriable;

		if ( slotId == ActiveSlot )
		{
			Items[slotId].Deploy();
		}


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


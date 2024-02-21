using Sandbox.Citizen;
using Sandbox.UI;
using System;
using System.Collections.Generic;
using System.Numerics;


public abstract class Carriable : Component, ICarriable
{
	public Player Owner { get; set; }

	[Property] public Texture Icon { get; set; }

	[Property][Range(1, 200)] public int Weight { get; set; }

	[Property] public CitizenAnimationHelper.HoldTypes HoldType { get; set; }

	public ModelRenderer Renderer { get; set; }
	public ModelCollider Collider { get; set; }
	public Rigidbody Rigidbody { get; set; }

	[Property] public Angles HeldAngleOffset { get; set; }
	[Property] public Vector3 HeldPositionOffset { get; set; }


	protected override void OnAwake()
	{
		GameObject.Network.SetOwnerTransfer( OwnerTransfer.Takeover );

		Renderer = Components.Get<ModelRenderer>();
		Collider = Components.Get<ModelCollider>();
		Rigidbody = Components.Get<Rigidbody>();
	}


	/// <summary>
	/// This carriable was added to player inventory.
	/// </summary>
	/// <param name="player">Player who picked up this carriable.</param>
	public virtual void OnPickup( Player player )
	{
		if ( GameObject.IsProxy ) { return; }

		GameObject.Network.TakeOwnership();

		Tags.Add( "owned" );
		SetOwner( player ); //TOOD...

		Collider.Enabled = false;
		Rigidbody.Enabled = false;
		GameObject.Enabled = false;
		GameObject.SetParent( Owner.HandRBone );
		Transform.LocalPosition = Vector3.Zero;

	}

	/// <summary>
	/// This carriable was removed from player inventory.
	/// </summary>
	public virtual void OnDrop()
	{
		if ( GameObject.IsProxy ) { return; }

		Collider.Enabled = true;
		GameObject.Enabled = true;
		Rigidbody.Enabled = true;

		GameObject.SetParent( null );

		// apply force
		Rigidbody.Velocity = Owner.EyeAngles.Forward * 250 + Owner.Controller.Velocity;
		Owner.CurrentHoldType = CitizenAnimationHelper.HoldTypes.None;

		Tags.Remove( "owned" );
		SetOwner( null );

		GameObject.Network.DropOwnership();

	}

	/// <summary>
	/// This carriable is no longer in the active slot of owner.
	/// </summary>
	[Broadcast]
	public virtual void Undeploy()
	{
		Collider.Static = true;
		GameObject.Enabled = false;

		if(GameObject.IsProxy) { return; }

		Transform.LocalPosition = Vector3.Zero;

		Owner.CurrentHoldType = CitizenAnimationHelper.HoldTypes.None;

	}

	/// <summary>
	/// This carriable is now in the active slot of owner.
	/// </summary>
	[Broadcast]
	public virtual void Deploy()
	{

		Collider.Static = true;
		GameObject.Enabled = true;

		if ( GameObject.IsProxy ) { return; }

		Owner.CurrentHoldType = HoldType;

		Owner.Animator.TriggerDeploy();
	}

	[Broadcast]
	private void SetOwner(Player owner)
	{
		this.Owner = owner;
	}

	[Broadcast] public abstract void OnUsePrimary();
	[Broadcast] public abstract void OnUseSecondary();

}

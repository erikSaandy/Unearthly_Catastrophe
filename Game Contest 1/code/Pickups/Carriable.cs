﻿using Sandbox.Citizen;
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
		if ( player.IsProxy ) { return; }
		
		GameObject.Network.TakeOwnership();

		Tags.Add( "owned" );
		Owner = player;

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

		Owner.CurrentHoldType = CitizenAnimationHelper.HoldTypes.None;

		Collider.Enabled = true;
		GameObject.Enabled = true;
		Rigidbody.Enabled = true;

		GameObject.SetParent( null );

		// apply force
		Rigidbody.Velocity = Owner.EyeAngles.Forward * 250 + Owner.Controller.Velocity;

		Tags.Remove( "owned" );
		Owner = null;

		GameObject.Network.DropOwnership();

	}

	public virtual void UpdateHeldPosition()
	{
		Vector3 hands = (Owner.HandRBone.Transform.Position + Owner.HandLBone.Transform.Position) * 0.5f;
		Transform.Position = hands;
		Transform.Rotation = Owner.HandRBone.Transform.Rotation * HeldAngleOffset;
	}

	/// <summary>
	/// This carriable is no longer in the active slot of owner.
	/// </summary>
	[Broadcast]
	public virtual void Undeploy()
	{
		GameObject.Enabled = false;
		Transform.LocalPosition = Vector3.Zero;
		GameObject.SetParent( Owner.GameObject );

		if (GameObject.IsProxy) { return; }

		Owner.CurrentHoldType = CitizenAnimationHelper.HoldTypes.None;

	}

	/// <summary>
	/// This carriable is now in the active slot of owner.
	/// </summary>
	[Broadcast]
	public virtual void Deploy()
	{
		GameObject.Enabled = true;

		if ( GameObject.IsProxy ) { return; }

		GameObject.SetParent( Owner.HandRBone );

		Owner.CurrentHoldType = HoldType;
		Owner.Animator.TriggerDeploy();
	}

	[Broadcast] public abstract void OnUsePrimary();
	[Broadcast] public abstract void OnUseSecondary();

}

using Sandbox.Citizen;
using Sandbox.UI;
using System;
using System.Collections.Generic;
using System.Numerics;
using static Sandbox.Clothing;


public abstract class Carriable : Component, ICarriable
{
	public bool IsInteractableBy( Player player ) { return true; }

	public float InteractionTime { get; set; } = 0f;
	public abstract string ToolTip { get; set; }
	public virtual string GetToolTip( Player player ) { return $"{IInteractable.GetInteractionKey()} - " + ToolTip; }

	public Player Owner { get; set; }

	[Property] public Texture Icon { get; set; }

	[Property][Range(1, 200)] public int Weight { get; set; }

	[Property] public CitizenAnimationHelper.HoldTypes HoldType { get; set; }

	public ModelRenderer Renderer { get; set; }
	public ModelCollider Collider { get; set; }
	//public Rigidbody Rigidbody { get; set; }

	[Property] public Angles HeldAngleOffset { get; set; }

	protected override void OnAwake()
	{
		GameObject.Network.SetOwnerTransfer( OwnerTransfer.Takeover );

		Renderer = Components.Get<ModelRenderer>();
		Collider = Components.Get<ModelCollider>();
		//Rigidbody = Components.Get<Rigidbody>();

		GameObject.BreakFromPrefab();
	
	}

	protected override void OnStart()
	{
		base.OnStart();

		if ( IsProxy ) { return; }

		DropToGround();

	}

	public virtual void OnInteract( Player player )
	{
		if ( player.IsProxy ) { return; }

		if ( Tags.Has("owned")) { Log.Info( $"Can't pick up owned item!" ); return; }
		if ( Owner != null ) { Log.Info( $"Can't pick up owned item!" ); return; }

		if (!player.Inventory.TryPickup(this, out int slotId)) { return; }

		// transfer ownership
		GameObject.Network.TakeOwnership();		
		Owner = player;
		Tags.Add( "owned" );


		if ( slotId == player.Inventory.ActiveSlot )
		{
			Deploy();
		}
		else
		{
			Undeploy();
		}

	}


	/// <summary>
	/// This carriable was removed from player inventory.
	/// </summary>
	public virtual void OnDrop()
	{

		if ( GameObject.IsProxy ) { return; }

		Owner.CurrentHoldType = CitizenAnimationHelper.HoldTypes.None;

		Collider.Enabled = true;
		Renderer.Enabled = true;
		GameObject.SetParent( null );

		SceneTraceResult trace = Scene.Trace.Ray( Owner.Camera.Transform.Position, Owner.Camera.Transform.Position + Owner.Camera.Transform.Rotation.Forward * 64 )
			.IgnoreGameObjectHierarchy( Owner.GameObject )
			.IgnoreGameObjectHierarchy( GameObject )
			.UseHitboxes()
			.Radius(8)
			.Run();

		GameObject.Transform.Position = trace.EndPosition;
		Renderer.Enabled = true;

		DropToGround();

		// apply force
		//Rigidbody.Velocity = Owner.EyeAngles.Forward * 250 + Owner.Controller.Velocity;

		Tags.Remove( "owned" );
		Owner = null;

		GameObject.Network.DropOwnership();

	}

	public SceneTraceResult DropToGround()
	{

		SceneTraceResult trace = Scene.Trace.Sphere( (Renderer.Bounds.Size.z * 0.4f), Transform.Position, Transform.Position + Vector3.Down * 512 )
		.UseHitboxes()
		.WithoutTags("item", "player")
		.UsePhysicsWorld()
		.Run();

		if ( Owner != null ) { GameObject.Transform.Rotation = Owner.Transform.Rotation.Angles().WithPitch( 0 ).WithRoll( 0 ); }

		GameObject.Transform.Position = trace.EndPosition;

		if ( trace.GameObject != null )
		{
			GameObject.SetParent( trace.GameObject.Root );

		}

		return trace;

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
		Renderer.Enabled = false;

		if ( IsProxy ) { return; }

		GameObject.SetParent( Owner.GameObject );
		Transform.LocalPosition = Vector3.Zero;

		if(Owner.Inventory.ActiveItem == this)
		{
			Owner.CurrentHoldType = CitizenAnimationHelper.HoldTypes.None;
		}


	}

	/// <summary>
	/// This carriable is now in the active slot of owner.
	/// </summary>
	[Broadcast]
	public virtual void Deploy()
	{
		Renderer.Enabled = true;

		if ( IsProxy ) { return; }

		GameObject.SetParent( Owner.HandRBone );

		Owner.CurrentHoldType = HoldType;
		Owner.Animator.TriggerDeploy();
	}

	[Broadcast] public abstract void OnUsePrimary();
	[Broadcast] public abstract void OnUseSecondary();

}

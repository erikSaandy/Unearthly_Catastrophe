using Sandbox.Citizen;
using Sandbox.UI;
using System;
using System.Collections.Generic;
using System.Numerics;


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

		DropToGround();

	}

	public virtual void OnInteract( Player player )
	{
		if ( player.IsProxy ) { return; }

		if ( Tags.Has("owned")) { Log.Info( $"Can't pick up owned item!" ); return; }
		if ( Owner != null ) { Log.Info( $"Can't pick up owned item!" ); return; }
		Owner = player;

		if (!player.Inventory.TryPickup(this)) { return; }

		GameObject.Network.TakeOwnership();

		Tags.Add( "owned" );

		Collider.Enabled = false;
		//Rigidbody.Enabled = false;
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
		//Rigidbody.Enabled = true;

		GameObject.SetParent( null );

		SceneTraceResult trace = Scene.Trace.Ray( Owner.Camera.Transform.Position, Owner.Camera.Transform.Position + Owner.Transform.Rotation.Forward * 64 )
			.IgnoreGameObjectHierarchy( Owner.GameObject )
			.IgnoreGameObjectHierarchy( GameObject )
			.UseHitboxes()
			.Size( 12f )
			.Run();

		GameObject.Transform.Position = trace.EndPosition;

		DropToGround();

		// apply force
		//Rigidbody.Velocity = Owner.EyeAngles.Forward * 250 + Owner.Controller.Velocity;

		Tags.Remove( "owned" );
		Owner = null;

		GameObject.Network.DropOwnership();

	}

	private void DropToGround()
	{
		SceneTraceResult trace = Scene.Trace.Ray( Transform.Position, Transform.Position + Vector3.Down * 512 )
		.UseRenderMeshes()
		.UseHitboxes()
		.UsePhysicsWorld()
		.Size(8)
		.Run();

		if ( Owner != null ) { GameObject.Transform.Rotation = Owner.Transform.Rotation.Angles().WithPitch( 0 ).WithRoll( 0 ); }

		GameObject.Transform.Position = trace.EndPosition + Vector3.Up * 4;

		if ( trace.GameObject != null && trace.GameObject.Tags.Has("spaceship"))
		{
			GameObject.SetParent( trace.GameObject );
		}
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

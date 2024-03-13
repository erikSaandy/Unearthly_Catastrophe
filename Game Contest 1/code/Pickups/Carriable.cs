﻿using Sandbox.Citizen;

public abstract class Carriable : Item, IInteractable
{
	public bool IsInteractableBy( Player player ) { return true; }

	public float InteractionTime { get; set; } = 0f;
	public abstract string ToolTip { get; set; }
	public virtual string GetToolTip( Player player ) { return $"{IInteractable.GetInteractionKey()} - {ToolTip}"; }

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

	[Broadcast]
	public virtual void OnInteract( Guid playerId )
	{

		GameObject _obj = GameObject.Scene.Directory.FindByGuid( playerId );

		if ( _obj.IsProxy ) {
			Tags.Add( "owned" );
			return;
		}

		if ( Tags.Has("owned")) { return; }
		if ( Owner != null ) { Log.Info( $"Can't pick up owned item!" ); return; }

		Player player = _obj.Components.Get<Player>();

		if (!player.Inventory.TryPickup(this, out int slotId)) { return; }

		// transfer ownership
		GameObject.Network.TakeOwnership();		
		Owner = player;
		Tags.Add( "owned" );

		SceneTraceResult trace = Scene.Trace.Ray( Transform.Position, Transform.Position + Vector3.Down * 32 ).UseHitboxes().WithoutTags( "item", "player" ).Run();

		if ( trace.Surface != null )
		{
			var sound = trace.Surface.Sounds.ImpactHard;
			if ( sound is not null )
			{
				var handle = Sound.Play( sound, Transform.Position );
			}
		}

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
	[Broadcast]	
	public virtual void OnDrop()
	{

		Tags.Remove( "owned" );

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

		Owner = null;

		GameObject.Network.DropOwnership();

	}

	public override SceneTraceResult DropToGround()
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
			OnDropOnGround( trace );

			if ( trace.Surface != null )
			{

				var sound = trace.Surface.Sounds.ImpactHard;
				if ( sound is not null )
				{
					var handle = Sound.Play( sound, trace.HitPosition + trace.Normal * 5f );
				}
			}

		}

		return trace;

	}

	protected virtual void OnDropOnGround( SceneTraceResult result )
	{
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

	public virtual void WasUsedOn( Guid interactable )
	{

	}

}

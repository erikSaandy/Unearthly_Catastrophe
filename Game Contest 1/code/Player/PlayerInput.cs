﻿using Sandbox;
using Sandbox.UI;
using Sandbox.VR;
using System;

public class PlayerInput
{

	public Player Owner { get; protected set; }

	public Vector3 AnalogMove { get; protected set; } = Vector3.Zero;
	public bool WantsToRun { get; protected set; } = false;

	public bool HasInput => AnalogMove.Length > 0f;
	public bool IsMoving => Owner.Controller.Velocity.WithY( 0 ).Length > 5f;
	public bool IsRunning => WantsToRun && HasInput && IsMoving;

	public PlayerInput(Player owner)
	{
		this.Owner = owner;
		Owner.Camera.GameObject.SetParent( owner.GameObject );
	}

	public virtual void UpdateInput( )
	{
		AnalogMove = Sandbox.Input.AnalogMove.Normal;
		WantsToRun = Sandbox.Input.Down( "Run" );

		if ( Sandbox.Input.Pressed( "use" ) )
		{
			var from = Owner.CameraController.Camera.Transform.Position;
			var to = from + Owner.Camera.Transform.Rotation.Forward * 70;
			IEnumerable<SceneTraceResult> trace = Owner.Scene.Trace.Ray( from, to ).IgnoreGameObjectHierarchy( Owner.GameObject ).WithoutTags( "owned" ).UseRenderMeshes().Size( 12f ).RunAll();
			IInteractable interactable = null;
			foreach ( SceneTraceResult item in trace )
			{
				item.GameObject.Components.TryGet( out interactable );
				if ( interactable != null )
				{
					interactable.OnInteract( Owner );
					break;
				}
			}

		}

		if ( Sandbox.Input.Pressed( "attack1" ) )
		{
			Owner.Inventory?.ActiveItem?.OnUsePrimary();
		}

		if ( Sandbox.Input.Pressed( "attack2" ) )
		{
			Owner.Inventory?.ActiveItem?.OnUseSecondary();
		}

		if ( Sandbox.Input.Pressed( "drop" ) )
		{
			Owner.Inventory?.DropActive();
		}

	}

	public virtual void OnJump( )
	{
		Owner.Controller.Punch( Vector3.Up * Owner.JumpStrength );
		Owner.Animator.TriggerJump();
		Owner.OnJumped?.Invoke();
	}

	public virtual void CameraInput()
	{
		Owner.EyeAngles += Sandbox.Input.AnalogLook;
		Owner.EyeAngles = Owner.EyeAngles.WithPitch( Math.Clamp( Owner.EyeAngles.pitch, Owner.CameraController.MinPitch, Owner.CameraController.MaxPitch ) );

		//Transform.LocalPosition = EyeOffset;

		Transform eyeTx = Owner.Animator.Target.GetAttachment( "eyes" ) ?? default;
		Owner.Camera.Transform.Position = eyeTx.Position;
		Owner.Camera.Transform.Rotation = Owner.EyeAngles.ToRotation();
	}

	public virtual void InventoryInput()
	{
		// Inventory scroll
		if ( Sandbox.Input.MouseWheel.y != 0 )
		{
			int slot = Owner.Inventory.ActiveSlot;
			slot = (Owner.Inventory.ActiveSlot - Math.Sign( Sandbox.Input.MouseWheel.y )) % Owner.Inventory.Items.Length;
			if ( slot < 0 ) { slot += Owner.Inventory.Items.Length; }
			Owner.Inventory.ActiveItem?.Undeploy();
			Owner.Inventory.ActiveSlot = slot;
			Owner.Inventory.ActiveItem?.Deploy();
		}
	}

}

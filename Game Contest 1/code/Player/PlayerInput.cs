public class PlayerInput
{

	public Player Owner { get; protected set; }

	public Vector3 AnalogMove { get; protected set; } = Vector3.Zero;
	public bool WantsToRun { get; protected set; } = false;

	public bool HasInput => AnalogMove.Length > 0f;
	public bool IsMoving => Owner.Controller.Velocity.WithY( 0 ).Length > 0.01f;
	public bool IsRunning => WantsToRun && HasInput && IsMoving && !Owner.EnergyBar.IsExhausted;


	private IInteractable InteractedWith = null;
	public IInteractable LookingAt { get; private set; } = null;
	public float InteractionTimer { get; private set; } = 0;

	public PlayerInput(Player owner)
	{
		this.Owner = owner;

		if (owner.IsProxy) { return; }

	}

	public virtual void UpdateInput( )
	{
		if( Owner.IsProxy ) { return; }

		BuildWishVelocity();

		AnalogMove = Sandbox.Input.AnalogMove.Normal;
		WantsToRun = Sandbox.Input.Down( "Run" );

		DoInteractionTrace();

		if ( Sandbox.Input.Pressed( "attack1" ) )
		{
			Owner.Inventory?.ActiveItem?.UsePrimary();
		}

		if ( Sandbox.Input.Pressed( "attack2" ) )
		{
			Owner.Inventory?.ActiveItem?.UseSecondary();
		}

		if ( Sandbox.Input.Pressed( "drop" ) )
		{
			Owner.Inventory?.DropActive();
		}

		if ( Sandbox.Input.Pressed( "mute" ) )
		{	
			//Toggle microphone
			ToggleMicrophone();
		}

	}

	protected virtual void BuildWishVelocity()
	{
		var rotation = Owner.EyeAngles.ToRotation();

		Owner.WishVelocity = rotation * Input.AnalogMove;
		Owner.WishVelocity = Owner.WishVelocity.WithZ( 0f );

		if ( !Owner.WishVelocity.IsNearZeroLength )
			Owner.WishVelocity = Owner.WishVelocity.Normal;

		if ( IsRunning )
			Owner.WishVelocity *= Owner.RunSpeed;
		else
			Owner.WishVelocity *= Owner.WalkSpeed;
	}
	private void DoInteractionTrace()
	{
		if ( Owner.IsProxy ) { return; }

		var from = Owner.CameraController.Camera.Transform.Position;
		var to = from + Owner.Camera.Transform.Rotation.Forward * 70;
		IEnumerable<SceneTraceResult> trace = Owner.Scene.Trace.Ray( from, to ).IgnoreGameObjectHierarchy( Owner.GameObject ).WithoutTags( "owned" ).Size( 12f ).RunAll();

		IInteractable hit = null;
		LookingAt = null;

		foreach ( SceneTraceResult item in trace )
		{
			item.GameObject?.Components.TryGet( out hit );
			if ( hit != null )
			{
				if( !hit.IsInteractableBy(Owner) ) { return; }

				LookingAt = hit;

				if ( InteractedWith == null && Input.Pressed( "use" ) && hit.IsInteractableBy( Owner ) )
				{
					InteractedWith = hit;
				}

				if(InteractedWith == null) { return; }

				if (Input.Down("use"))
				{
					InteractionTimer += Time.Delta;

					if(InteractionTimer >= InteractedWith.InteractionTime)
					{
						LookingAt.OnInteract( Owner.GameObject.Id );
						Owner.Inventory.ActiveItem?.WasUsedOn( item.GameObject.Id );
						InteractedWith = null;
						InteractionTimer = 0;
					}
				}

				break;
			}
		}

		if(Input.Released("use") || LookingAt == null)
		{
			InteractedWith = null;
			InteractionTimer = 0;
		}

	}

	public virtual void OnJump( )
	{
		if ( Owner.IsProxy ) { return; }

		Owner.Controller.Punch( Vector3.Up * Owner.JumpStrength );
		Owner.Animator.TriggerJump();
		Owner.OnJumped?.Invoke();
	}

	public virtual void CameraInput()
	{
		if(Owner.IsProxy) { return; }
	}

	public virtual void OnPreRender()
	{
		var angles = Owner.EyeAngles.Normal;
		angles += Sandbox.Input.AnalogLook * 0.5f;
		angles.pitch = angles.pitch.Clamp( Owner.CameraController.MinPitch, Owner.CameraController.MaxPitch );
		Owner.EyeAngles = angles.WithRoll( 0 );

		Transform eyeTx = Owner.Animator.Target.GetAttachment( "forward_reference" ) ?? default;
		Owner.Camera.Transform.Position = eyeTx.Position + eyeTx.Rotation.Up * 6;
		Owner.Camera.Transform.Rotation = Owner.EyeAngles.ToRotation();
	}

	public virtual void InventoryInput()
	{
		if ( Owner.IsProxy ) { return; }

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

	protected void ToggleMicrophone()
	{
		if ( Owner.IsProxy ) { return; }

		Owner.Voice.Mode = Owner.Voice.Mode == Voice.ActivateMode.PushToTalk ? Voice.ActivateMode.AlwaysOn : Voice.ActivateMode.PushToTalk;
	}

}

using Sandbox;

[Title( "View Model" )]
public sealed class ViewModel : Component
{
	[Property] public SkinnedModelRenderer ModelRenderer { get; set; }
	[Property] public bool UseSprintAnimation { get; set; }

	/// <summary>
	/// Looks up the tree to find the player controller.
	/// </summary>
	[Property] public Player Owner {  get; set; }
	private CameraComponent Camera => Owner.Camera;
	private Carriable Carriable { get; set; }

	public void SetCarriable( Carriable carriable )
	{
		Carriable = carriable;
	}

	protected override void OnStart()
	{
		if( Owner.IsProxy )
		{
			ModelRenderer.RenderType = Sandbox.ModelRenderer.ShadowRenderType.Off;
			return;
		}

		ModelRenderer.Set( "b_deploy", true );

		if ( Owner.IsValid() )
		{
			Owner.OnJumped += OnPlayerJumped;
		}
	}

	protected override void OnDestroy()
	{
		if ( Owner.IsProxy ) { return; }

		if ( Owner.IsValid() )
		{
			Owner.OnJumped -= OnPlayerJumped;
		}

		base.OnDestroy();
	}

	protected override void OnUpdate()
	{
		if ( GameObject.IsProxy ) { return; }

		LocalRotation = Rotation.Identity;
		LocalPosition = Vector3.Zero;

		ApplyVelocity();
		ApplyStates();
		ApplyAnimationParameters();

		LerpedLocalRotation = Rotation.Lerp( LerpedLocalRotation, LocalRotation, Time.Delta * 10f );
		LerpedLocalPosition = LerpedLocalPosition.LerpTo( LocalPosition, Time.Delta * 10f );

		Transform.LocalRotation = LerpedLocalRotation;
		Transform.LocalPosition = LerpedLocalPosition;
	}

	private void OnPlayerJumped()
	{
		ModelRenderer.Set( "b_jump", true );
	}

	private Vector3 LerpedWishLook { get; set; }
	private Vector3 LocalPosition { get; set; }
	private Rotation LocalRotation { get; set; }
	private Vector3 LerpedLocalPosition { get; set; }
	private Rotation LerpedLocalRotation { get; set; }

	private void ApplyVelocity()
	{
		var moveVel = Owner.Controller.Velocity;
		var moveLen = moveVel.Length;
		if ( Owner.Tags.Has( "slide" ) ) moveLen = 0;

		var wishLook = Owner.InputData.AnalogMove;

		LerpedWishLook = LerpedWishLook.LerpTo( wishLook, Time.Delta * 5.0f );

		LocalRotation *= Rotation.From( 0, -LerpedWishLook.y * 3f, 0 );
		LocalPosition += -LerpedWishLook;

		ModelRenderer.Set( "move_groundspeed", moveLen );
	}

	private void ApplyStates()
	{
		if ( !Owner.Tags.Has( "slide" ) )
		{
			return;
		}

		LocalPosition += Vector3.Backward * 2f;
		LocalRotation *= Rotation.From( 10f, 25f, -5f );
	}

	private void ApplyAnimationParameters()
	{
		ModelRenderer.Set( "b_sprint", UseSprintAnimation && Owner.InputData.IsRunning );
		ModelRenderer.Set( "b_grounded", Owner.Controller.IsOnGround );
	}
}

public sealed class EnergyBarComponent : Component
{
	[Property] public Player Owner { get; private set; }

	public const float MaxEnergy = 100;

	/// <summary>
	/// Minimum energy decay while running (per second)
	/// </summary>
	[Property][Range(0, MaxEnergy)] public float Decay { get; set; } = 10;
	[Property][Range( 0, MaxEnergy * 2 )] public float ExhaustionPenalty { get; set; } = 50;

	//[Property] public Curve DecayCurve;

	public float CurrentEnergy { get; set; }

	public bool IsExhausted { get; private set; } = false;



	public EnergyBarComponent() { 
		CurrentEnergy = MaxEnergy;
	}

	protected override void OnStart()
	{
		base.OnStart();

		if ( GameObject.IsProxy ) { return; }

		Owner.OnJumped += OnJumped;
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();


		if ( GameObject.IsProxy ) { return; }

		if (IsExhausted)
		{
			CurrentEnergy += Time.Delta * Decay * 0.5f;
			if(CurrentEnergy >= MaxEnergy)
			{
				CurrentEnergy = MaxEnergy;
				IsExhausted = false;
			}
		}
		else if( !Owner.Controller.IsOnGround)
		{
			
		}
		// Running and moving
		else if ( Owner.PlayerInput != null && Owner.PlayerInput.WantsToRun && Owner.PlayerInput.HasInput && Owner.PlayerInput.IsMoving )
		{
			CurrentEnergy -= Time.Delta * Decay;

			if(CurrentEnergy < -15) { 
				CurrentEnergy = -ExhaustionPenalty; 
				IsExhausted = true;
			}

		}
		else if(CurrentEnergy < MaxEnergy)
		{
			CurrentEnergy += Time.Delta * Decay * 0.4f;
			if(CurrentEnergy > MaxEnergy) { CurrentEnergy = MaxEnergy; }
		}
	}

	void OnJumped()
	{
		if(!IsExhausted)
		{
			CurrentEnergy -= 20;
		}
	}

}

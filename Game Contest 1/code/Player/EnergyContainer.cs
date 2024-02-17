public sealed class EnergyContainer : Component
{
	[Property] public Player Owner { get; private set; }

	public const float MaxEnergy = 100;

	/// <summary>
	/// Minimum energy decay while running (per second)
	/// </summary>
	[Property][Range(0, MaxEnergy)] public float Decay { get; set; } = 10;
	[Property][Range( 0, MaxEnergy * 2 )] public float ExhaustionPenalty { get; set; } = 50;

	//[Property] public Curve DecayCurve;

	public float CurrentEnergy { get; private set; }

	public bool IsExhausted { get; private set; } = false;



	public EnergyContainer() { CurrentEnergy = MaxEnergy; }

	public void Update()
	{
		if(IsExhausted)
		{
			CurrentEnergy += Time.Delta * Decay * 0.5f;
			if(CurrentEnergy >= MaxEnergy)
			{
				CurrentEnergy = MaxEnergy;
				IsExhausted = false;
			}
		}
		// Running and moving
		else if (Owner.InputData.WantsToRun && Owner.InputData.HasInput && Owner.InputData.IsMoving)
		{
			CurrentEnergy -= Time.Delta * Decay;

			if(CurrentEnergy < 0) { 
				CurrentEnergy = -ExhaustionPenalty; 
				IsExhausted = true;
			}

		}
		else if(CurrentEnergy < MaxEnergy)
		{
			CurrentEnergy += Time.Delta * Decay;
			if(CurrentEnergy > MaxEnergy) { CurrentEnergy = MaxEnergy; }
		}
	}

}

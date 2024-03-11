using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class Monster : Component, IKillable
{
	[Sync] public LifeState LifeState { get; protected set; } = LifeState.Alive;

	[Sync] public RealTimeSince TimeSinceDeath { get; protected set; }

	public float MaxHealth => 100;
	[Sync] public float Health { get; protected set; }

	public virtual void Kill()
	{
		if ( LifeState == LifeState.Dead ) { return; }
		TakeDamage( Health + 100, GameObject.Id );
	}

	public virtual void TakeDamage( float damage, Guid attackerId, Vector3 impulseForce = default )
	{
		if ( LifeState == LifeState.Dead )
			return;

		if ( IsProxy )
			return;

		Health = MathF.Max( Health - damage, 0f );

		if ( Health <= 0f )
		{
			LifeState = LifeState.Dead;
			//Ragdoll.Ragdoll();
			//OnKilled( Scene.Directory.FindByGuid( attackerId ) );
			TimeSinceDeath = 0;
		}
	}

}

public interface IKillable
{
	public LifeState LifeState { get; } 

	public float MaxHealth { get; }
	public float Health { get; }

	public void TakeDamage( float damage, Guid attackerId );

	public void Kill();
}

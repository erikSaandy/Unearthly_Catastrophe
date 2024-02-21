using Sandbox.UI;

public interface ICarriable
{
	[Property] public Texture Icon { get; set; }
	public abstract void OnPickup( Player player );

}

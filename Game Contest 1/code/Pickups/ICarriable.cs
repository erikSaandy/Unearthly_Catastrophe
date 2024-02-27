using Sandbox.Citizen;
using Sandbox.UI;

public interface ICarriable : IInteractable
{
	[Property] public Texture Icon { get; set; }

	public void OnDrop();

}

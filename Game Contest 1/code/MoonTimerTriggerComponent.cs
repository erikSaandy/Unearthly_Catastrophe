using Sandbox;
using Sandbox.Internal;

public sealed class MoonTimerTriggerComponent : Component, Component.ITriggerListener
{

	public void OnTriggerEnter( Collider other )
	{

		if ( other.GameObject.Components.TryGet( out Player player ) )
		{
			if ( player.IsProxy ) { return; }

			MoonTimerComponent.Instance.Show();

		}

	}

	public void OnTriggerExit( Collider other )
	{

		if(other.GameObject.Components.TryGet(out Player player))
		{
			if(player.IsProxy) { return; }

			MoonTimerComponent.Instance.Hide();
		}

	}

}

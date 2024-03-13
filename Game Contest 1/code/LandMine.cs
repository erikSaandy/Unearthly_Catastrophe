using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Sandbox.Gizmo;

public class LandMine : Component, Component.ITriggerListener
{

	[Property] public ModelRenderer ModelRensderer { get; set; }
	[Property] public float BlastRadius { get; set; } = 96f;

	[Sync] private int PassengerCount { get; set; } = 0;



	public void OnTriggerEnter( Collider other )
	{
		if(IsProxy) { return; }

		if ( GameObject.Root.IsAncestor( other.GameObject ) ) { return; }


		other.GameObject.Root.Components.TryGet<IKillable>( out IKillable killable );

		if(killable != null)
		{
			AddPassenger();
		}


	}

	public void OnTriggerExit( Collider other )
	{
		if ( IsProxy ) { return; }

		other.GameObject.Root.Components.TryGet<IKillable>( out IKillable killable );

		if ( killable != null )
		{
			RemovePassenger();
		}
	}

	private void AddPassenger()
	{
		if(PassengerCount == 0)
		{
			Trigger();
		}

		PassengerCount++;
	}

	private void RemovePassenger()
	{
		PassengerCount--;

		if ( PassengerCount == 0 )
		{
			Explode();
		}
	}

	[Broadcast]
	private void Trigger()
	{
		Components.Get<ModelRenderer>().MaterialGroup = "on";
	}

	[Broadcast]
	private void Explode()
	{
		Components.Get<ModelRenderer>().MaterialGroup = "default";

		if ( IsProxy ) { return; }

		ExplodeAsync();
	}

	private async void ExplodeAsync()
	{
		await Task.DelayRealtimeSeconds( 0.5f );

		List<Player> connections = LethalGameManager.Instance.ConnectedPlayers.ToList();

		foreach ( Player player in connections )
		{
			float dst = Vector3.DistanceBetween( player.Transform.Position, Transform.Position );
			if(dst < BlastRadius)
			{
				player.Components.Get<IKillable>().Kill();
			}
		}

		GameObject.Destroy();

	}

}

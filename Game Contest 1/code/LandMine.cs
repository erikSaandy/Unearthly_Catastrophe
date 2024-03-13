using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Sandbox.Gizmo;

public class LandMine : Item, Component.ITriggerListener
{

	[Property] public ModelRenderer Renderer { get; set; }
	[Property] public float BlastRadius { get; set; } = 96f;

	[Sync] private int PassengerCount { get; set; } = 0;

	[Property] public LegacyParticleSystem ExplosionParticle { get; set; }

	[Category("Sounds")][Property] public SoundEvent ExplosionSound { get; set; }
	[Category( "Sounds" )][Property] public SoundEvent BeepingSound { get; set; }
	[Category( "Sounds" )][Property] public SoundEvent TriggeredSound { get; set; }

	public Texture Icon { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

	public void OnTriggerEnter( Collider other )
	{
		if ( IsProxy ) { return; }

		if ( GameObject.Root.IsAncestor( other.GameObject ) ) { return; }


		other.GameObject.Root.Components.TryGet<IKillable>( out IKillable killable );

		if ( killable != null )
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
			PlaySound( TriggeredSound.ResourcePath );
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

		PlaySound( BeepingSound.ResourcePath );
		ExplodeAsync();
	}

	private async void ExplodeAsync()
	{
		await Task.DelayRealtimeSeconds( 0.4f );

		ExplosionParticle.Enabled = true;
		PlaySound( ExplosionSound.ResourcePath );

		List<Player> connections = LethalGameManager.Instance.ConnectedPlayers.ToList();
		foreach ( Player player in connections )
		{
			float dst = Vector3.DistanceBetween( player.Transform.Position, Transform.Position );
			if(dst < BlastRadius)
			{
				player.Components.Get<IKillable>().Kill();
				
			}
		}

		Renderer.Enabled = false;
		Enabled = false;

	}

	[Broadcast]
	private void PlaySound( string sound )
	{
		Sound.Play( sound, Transform.Position );
	}

	public override SceneTraceResult DropToGround()
	{

		SceneTraceResult trace = Scene.Trace.Ray( Transform.Position, Transform.Position + Vector3.Down * 512 )
		.Size( 2 )
		.UseHitboxes()
		.WithoutTags( "item", "player" )
		.UsePhysicsWorld()
		.Run();

		GameObject.SetParent( trace.GameObject?.Root );
		GameObject.Transform.Position = trace.EndPosition;

		return trace;

	}

}

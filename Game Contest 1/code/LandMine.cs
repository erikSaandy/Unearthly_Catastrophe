using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Sandbox.Gizmo;

public class LandMine : Item, Component.ITriggerListener
{

	[Property] public ModelRenderer Renderer { get; set; }
	[Property] public ModelCollider Collider { get; set; }
	[Property] public float BlastRadius { get; set; } = 96f;

	[Sync] private int PassengerCount { get; set; } = 0;

	[Property] public LegacyParticleSystem ExplosionParticle { get; set; }

	[Category("Sounds")][Property] public SoundEvent ExplosionSound { get; set; }
	[Category( "Sounds" )][Property] public SoundEvent BeepingSound { get; set; }
	[Category( "Sounds" )][Property] public SoundEvent TriggeredSound { get; set; }

	private RealTimeSince TimeSinceSpawned { get; set; } = 0;

	protected override void OnUpdate()
	{
		base.OnUpdate();

		if ( !Collider.Enabled && TimeSinceSpawned > 10 )
		{
			Collider.Enabled = true;
		}
	}

	protected override void DrawGizmos()
	{
		base.DrawGizmos();

		Gizmo.Draw.Color = Color.Red;
		Gizmo.Draw.LineSphere( Vector3.Zero, BlastRadius );

	}

	public void OnTriggerEnter( Collider other )
	{

		if ( IsProxy ) { return; }
	
		if ( GameObject.Root.IsAncestor( other.GameObject ) ) { return; }

		other.GameObject.Root.Components.TryGet<IKillable>( out IKillable killable );

		if ( killable != null )
		{
			Log.Info( other.GameObject.Name + " stepped on a mine." );
			AddPassenger();
			Log.Info( PassengerCount );
		}


	}

	public void OnTriggerExit( Collider other )
	{
		if ( IsProxy ) { return; }
		Log.Info( PassengerCount );

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

		Log.Info( GameObject.Name + " exploded." );

		PlaySound( BeepingSound.ResourcePath );
		ExplodeAsync();
	}

	private async void ExplodeAsync()
	{
		await Task.DelayRealtimeSeconds( 0.4f );

		ExplosionParticle.Enabled = true;
		PlaySound( ExplosionSound.ResourcePath );

		IEnumerable<GameObject> hits = Scene.FindInPhysics( new Sphere( Transform.Position, BlastRadius ) ).Where( x => (x.Components.Get<IKillable>() != null) );
		//Log.Info( hits.Count() );
		foreach ( GameObject killable in hits ) { killable.Components.Get<IKillable>().Kill(); }

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

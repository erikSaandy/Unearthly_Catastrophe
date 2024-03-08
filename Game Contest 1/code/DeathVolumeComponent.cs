using Sandbox;
using System.Numerics;

public sealed class DeathVolumeComponent : Component, Component.ITriggerListener
{
	private BoxCollider collider { get; set; }

	protected override void OnAwake()
	{
		if ( !GameObject.Components.TryGet<BoxCollider>( out BoxCollider bc ) )
		{
			collider = bc;
			Log.Error( $"Death volume {GameObject.Name} does not have a box collider attached!" );
		}
		else
		{
			bc.IsTrigger = true;
		}

		base.OnAwake();
	}

	public void OnTriggerEnter( Collider other )
	{
		if(other.GameObject.Components.TryGet(out IKillable killable ) )
		{
			killable.Kill();
		}
	}

	public void OnTriggerExit( Collider other )
	{

	}

	protected override void DrawGizmos()
	{
		base.DrawGizmos();



		if( GameObject.Components.TryGet<BoxCollider>(out BoxCollider bc))
		{
			Gizmo.Draw.Color = Color.Red.WithAlpha( 0.4f );;
			Gizmo.Draw.Model( "models/dev/box.vmdl_c", new Transform( bc.Center, Quaternion.Identity, bc.Scale / 50f ) );

		}


	}

}

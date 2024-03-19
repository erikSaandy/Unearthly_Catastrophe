using Saandy;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public static class CameraExtension
{

	public static bool CanSeeBounds(this CameraComponent camera, BBox bounds, float error = 0.03f)
	{
		// not looking towards bounds.
		Vector3 dir = bounds.Center - camera.Transform.Position;
		float dot = Vector3.Dot( camera.Transform.Rotation.Forward.WithZ(0).Normal, dir.WithZ(0).Normal );

		if ( dot < 0 ) { return false; }

		if ( IsOnScreen( bounds.Center ) ) 	{ return true; }

		IEnumerable<Vector3> corners = bounds.Corners.ToList();
		foreach(Vector3 corner in corners)
		{
			if(IsOnScreen( corner ) ) {
				return true; 
			}
		}

		return false;
		
		bool IsOnScreen( Vector3 position )
		{
			position = camera.PointToScreenNormal( position );

			if ( position.x <= error || position.x >= (1 - error)
				|| position.y <= error || position.y >= (1 - error) )
			{
				return false;
			}

			return true;
		}

	}

}

using Saandy;

public static class Vector3BufferExtension
{

	#region Sum and Median

	/// <summary>
	/// Get the sum of all valued excluding the newest value (frontmost).
	/// </summary>
	/// <param name="source"></param>
	/// <returns></returns>
	public static Vector3 TailSum(this ValueBuffer<Vector3> source, bool normalize = false) => normalize ? SumNormalzied(source, 1, source.Size - 1) : Sum(source, 1, source.Size - 1);

	/// <summary>
	/// Get the sum of a Vector3 buffer.
	/// </summary>
	/// <param name="source"></param>
	/// <returns></returns>
	public static Vector3 Sum(this ValueBuffer<Vector3> source, bool normalize = false) => normalize ? SumNormalzied(source, 0, source.Size) : Sum(source, 0, source.Size);

	/// <summary>
	/// Get the sum of a Vector3 buffer.
	/// </summary>
	/// <param name="source"></param>
	/// <param name="startIndex">The first index to be included in the sum.</param>
	/// <param name="length">The amount of values to be included in the sum.</param>
	/// <returns></returns>
	public static Vector3 Sum(this ValueBuffer<Vector3> source, int startIndex, int length)
	{
		Vector3 sum = Vector3.Zero;
		for (int i = startIndex; i <= length; i++) { sum += source[i]; }

		return sum;
	}

	/// <summary>
	/// Get the sum of a Vector3 buffer, but normalize each value before adding to sum.
	/// </summary>
	/// <param name="source"></param>
	/// <param name="startIndex"></param>
	/// <param name="length"></param>
	/// <returns></returns>
	public static Vector3 SumNormalzied(this ValueBuffer<Vector3> source, int startIndex, int length)
	{
		Vector3 sum = Vector3.Zero;
		for (int i = startIndex; i <= length; i++) { sum += source[i].Normal; }
		return sum;	
	}

	/// <summary>
	/// Get the median of a Vector3 buffer, excluding the frontmost value.
	/// </summary>
	/// <param name="source"></param>
	/// <returns></returns>
	public static Vector3 TailMedian(this ValueBuffer<Vector3> source) => Median(source, 1, source.Size - 1);

	/// <summary>
	/// Get the median of a Vector3 buffer.
	/// </summary>
	/// <param name="source"></param>
	/// <returns></returns>
	public static Vector3 Median(this ValueBuffer<Vector3> source) => Median(source, 0, source.Size);

	/// <summary>
	/// Get the median of a Vector3 buffer.
	/// </summary>
	/// <param name="source"></param>
	/// <param name="startIndex">The first index to be included in the median.</param>
	/// <param name="length">The amount of values to be included in the median.</param>
	/// <returns></returns>
	public static Vector3 Median(this ValueBuffer<Vector3> source, int startIndex, int length)
	{
		return Sum(source, startIndex, length) / length;
	}

	/// <summary>
	/// Get the median direction of a Vector3 buffer, excluding the frontmost value.
	/// </summary>
	/// <param name="source"></param>
	/// <param name="startIndex"></param>
	/// <param name="length"></param>
	/// <returns></returns>
	public static Vector3 TailMedianDirection(this ValueBuffer<Vector3> source) => MedianDirection(source, 1, source.Size - 1);

	/// <summary>
	/// Get the median direction of a range in Vector3 buffer.
	/// </summary>
	/// <param name="source"></param>
	/// <param name="startIndex"></param>
	/// <param name="length"></param>
	/// <returns></returns>
	public static Vector3 MedianDirection(this ValueBuffer<Vector3> source, int startIndex, int length)
	{
		return (SumNormalzied(source, startIndex, length) / length).Normal;
	}

	#endregion

	/// <summary>
	/// Get the dot between all previous values and the most recent value in the buffer.
	/// </summary>
	/// <param name="source"></param>
	/// <returns></returns>
	public static float DotTail(this ValueBuffer<Vector3> source)
	{
		Vector3 tailDirection = TailMedianDirection(source);
		return Vector3.Dot(source[0].Normal, tailDirection);
	}

	/// <summary>
	/// Get the dot between the most previous value and the most recent value in the buffer.
	/// </summary>
	/// <param name="source"></param>
	/// <returns></returns>
	public static float Dot(this ValueBuffer<Vector3> source)
	{
		return Vector3.Dot(source, source.Previous);
	}

	public static void SetCurrent(this ValueBuffer<Vector3> source, Vector3 value)
	{
		source.buffer[0] = value;
	}

	public static void SetCurrent(this ValueBuffer<Vector3> source, float x, float y, float z)
	{
		source.buffer[0].Set(x, y, z);
	}

	public static void SetCurrentX(this ValueBuffer<Vector3> source, float x) {
		source.buffer[0].x = x;
	}
	public static void SetCurrentY(this ValueBuffer<Vector3> source, float y)
	{
		source.buffer[0].y = y;
	}

	public static void SetCurrentZ(this ValueBuffer<Vector3> source, float z)
	{
		source.buffer[0].z = z;
	}


	/// <summary>
	/// Returns the rotational continuity of the buffer, in degrees.
	/// </summary>
	/// <param name="source"></param>
	/// <param name="axis"></param>
	/// <returns></returns>
	public static float RotationalContinuity(this ValueBuffer<Vector3> source, Vector3 axis)
	{
		float angle = 0;
		int dir = 0;
		Vector3 vA = source[0];
		Vector3 vB = source[1];

		
		float a = Math2d.SignedAngle( vA, vB, axis );
		// Get the direction of the rotation.
		dir = Math.Sign(a);
		angle += a;

		for (int i = 2; i < source.Size; i++)
		{
			vA = source[i-1];
			vB = source[i];
			a = Math2d.SignedAngle( vA, vB, axis );

			// Is this angle going in the right direction?
			if (Math2d.SameSign(a, dir))
			{
				angle += a;
			}
			// Wrong direction! Continuity is broken.
			else
			{
				break;
			}
		}

		return angle;
	}

}

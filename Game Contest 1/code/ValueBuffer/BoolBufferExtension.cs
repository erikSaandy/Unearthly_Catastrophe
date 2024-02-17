
public static class BoolBufferExtension
{

	#region Sum and Median


	/// <summary>
	/// Returns true if all values of buffer is true, and otherwise false.
	/// </summary>
	/// <param name="source"></param>
	/// <returns></returns>
	public static bool AND(this ValueBuffer<bool> source)
	{
		// If any value in buffer does not match polarity, return false.
		for (int i = 0; i < source.buffer.Length; i++) { if (source[i] != true) { return false; } }

		return true;
	}

	#endregion

}

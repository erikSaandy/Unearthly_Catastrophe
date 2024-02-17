using System;
using System.Collections;
using System.Collections.Generic;

public class ValueBuffer<T>
{

	public T[] buffer;

	public T this[int i] { get { return buffer[i]; } private set { buffer[i] = value; } }

	public int Size => buffer.Length;
	public T Current { get { return buffer[0]; } set { buffer[0] = value; } }
	public T Previous => buffer[1];

	public ValueBuffer(int size) { if (size < 2) { throw new ArgumentOutOfRangeException("size"); }
		buffer = new T[size];
	}

	public static implicit operator T(ValueBuffer<T> source) => source.buffer[0];
	public static bool operator ==(ValueBuffer<T> a, T b) { return a.buffer[0].Equals(b); }
	public static bool operator !=(ValueBuffer<T> a, T b) { return !a.buffer[0].Equals(b); }
	public static bool operator ==(T a, ValueBuffer<T> b) { return b.buffer[0].Equals(a); }
	public static bool operator !=(T a, ValueBuffer<T> b) { return !b.buffer[0].Equals(a); }
	public override bool Equals(object o) { return o == this; }
	public override int GetHashCode() { return GetHashCode(); }

	/// <summary>
	/// Adds the value to the front of the buffer and pushes old values back in the buffer.
	/// </summary>
	/// <param name="value"></param>
	public void Push(T value)
	{
		Push();
		// Add new value
		buffer[0] = value;
	}

	/// <summary>
	/// Pushes the buffer back to make room for new value at [0].
	/// </summary>
	/// <param name="value"></param>
	public void Push()
	{
		// Push value back
		for (int i = Size - 1; i > 0; i--) { buffer[i] = buffer[i - 1]; }
	}

	/// <summary>
	/// Returns true if any T in buffer equals value.
	/// </summary>
	/// <param name="value"></param>
	/// <returns></returns>
	public bool Has(T value) => Has(value, Size);

	/// <summary>
	/// Returns true if any T in buffer equals value.
	/// </summary>
	/// <param name="value">Value to compare against</param>
	/// <param name="maxDepth">How deep into the buffer should we search?</param>
	/// <returns></returns>
	public bool Has(T value, int maxDepth)
	{
		maxDepth = maxDepth > Size ? Size : maxDepth; // Clamp maxDepth to not be more than buffer size.

		for (int i = 0; i < maxDepth; i++) { if (value.Equals(buffer[i])) { return true; } }
		return false;
	}

	public void Clear()
	{
		for (int i = 0; i < buffer.Length; i++)
		{
			buffer[i] = default(T);
		}
	}

	public void LogData()
	{
		string s = "";
		for (int i = 0; i < Size; i++) { s += "[" + i + "] " + buffer[i] + "\n"; }
		s += "----------";
		Log.Info( s );
	}

}

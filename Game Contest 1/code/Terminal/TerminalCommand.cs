using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public abstract class TerminalCommand
{

	public string[] KeyWords { get; set; }

	public TerminalCommand( params string[] keyWords )
	{
		this.KeyWords = keyWords;
	}

	public abstract void Run( TerminalComponent Terminal, params string[] parts );

	public float GetMatch( string input )
	{
		float bestMatch = 0f;

		foreach ( string keyWord in KeyWords )
		{
			float currentMatch = 0f;

			for ( int i = 0; i < input.Length; i++ )
			{
				// Don't allow space in keyword.
				if ( input[i] == ' ' ) { break; }

				// Reached keyword end, but word keeps going.
				if ( i >= keyWord.Length ) {
					
					currentMatch -= (1f / keyWord.Length);
					continue; 
				}

				if ( keyWord[i] == input[i] ) 
				{
					currentMatch += (1f / keyWord.Length); 
				}

			}

			if ( currentMatch > bestMatch ) { bestMatch = currentMatch; }
		}

		return bestMatch;
	}


}

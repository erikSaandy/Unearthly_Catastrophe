using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
public class TerminalCommandTravel : TerminalCommand
{
	public TerminalCommandTravel( params string[] keyWords ) : base( keyWords ) { }

	public override void Run( TerminalComponent Terminal, params string[] parts )
	{

		if(parts == null) { return; }

		for ( int i = 0; i < LethalGameManager.MoonDefinitions.Length; i++ )
		{
			if( LethalGameManager.MoonDefinitions[i].ResourceName.ToLower() == parts[0].ToLower() )
			{
				Terminal.Exit();
				Terminal.OpenPage( new TerminalPageMain() );

				TerminalComponent.SelectMoon(i);

			}
		}
	}

}

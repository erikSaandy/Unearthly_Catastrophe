using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
public class TerminalCommandRoute : TerminalCommand
{
	public TerminalCommandRoute( params string[] keyWords ) : base( keyWords ) { }

	public override void Run( TerminalComponent Terminal, params string[] parts )
	{

		if(parts == null) { return; }

		for ( int i = 0; i < LethalGameManager.MoonDefinitions.Length; i++ )
		{
			if( LethalGameManager.MoonDefinitions[i].ResourceName.ToLower() == parts[0].ToLower() )
			{
				string moon = LethalGameManager.MoonDefinitions[i].ResourceName;

				Action action = new Action( () => {
						Terminal.Exit();
						TerminalComponent.SelectMoon( i );
				} );

				Terminal.OpenPage( new TerminalPageConfirm( $"Route the Auto-Pilot for {moon}?", action ) );
				return;
			}
		}
	}

}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
public class TerminalCommandExit : TerminalCommand
{
	public TerminalCommandExit( params string[] keyWords ) : base( keyWords ) { }

	public override void Run( TerminalComponent Terminal, params string[] parts )
	{
		Terminal.Exit();
		Terminal.OpenPage( new TerminalPageMain() );

	}

}

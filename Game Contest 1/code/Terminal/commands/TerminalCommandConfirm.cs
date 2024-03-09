using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
public class TerminalCommandConfirm : TerminalCommand
{
	public TerminalCommandConfirm( params string[] keyWords ) : base( keyWords ) { }

	public override void Run( TerminalComponent Terminal, params string[] parts )
	{
		if(Terminal.CurrentPage is TerminalPageConfirm)
		{
			(Terminal.CurrentPage as TerminalPageConfirm).OnConfirm?.Invoke();
		}

	}

}

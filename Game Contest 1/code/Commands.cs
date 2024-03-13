using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
public static class Commands
{
	[ConCmd( "kill" )]
	public static void Kill( string name = "" )
	{
		if ( name == string.Empty )
		{
			KillSelf();
			return;
		}

		foreach ( Player player in LethalGameManager.Instance.AlivePlayers )
		{

			if ( player.Network.OwnerConnection.DisplayName.ToLower() == name.ToLower() )
			{
				player.Components.Get<IKillable>().Kill();
			}

		}
	}

	private static void KillSelf()
	{
		//Log.Info( LethalGameManager.Instance.ConnectedPlayers.Count );

		foreach ( Player player in LethalGameManager.Instance.AlivePlayers )
		{

			if ( player.IsProxy ) { continue; }

			player.Components.Get<IKillable>().Kill();
			break;

		}
	}

	[ConCmd( "killall" )]
	private static void KillAll()
	{
		foreach ( Player player in LethalGameManager.Instance.AlivePlayers )
		{
			player.Kill();
		}

	}

}

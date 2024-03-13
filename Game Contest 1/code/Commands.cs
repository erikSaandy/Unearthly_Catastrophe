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

		foreach ( Guid playerId in LethalGameManager.Instance.ConnectedPlayers )
		{
			GameObject playerObject = LethalGameManager.Instance.Scene.Directory.FindByGuid( playerId );

			if ( playerObject.Network.OwnerConnection.DisplayName.ToLower() == name.ToLower() )
			{
				playerObject.Components.Get<IKillable>().Kill();
			}

		}
	}

	private static void KillSelf()
	{
		//Log.Info( LethalGameManager.Instance.ConnectedPlayers.Count );

		foreach ( Guid playerId in LethalGameManager.Instance.ConnectedPlayers )
		{
			GameObject playerObject = LethalGameManager.Instance.Scene.Directory.FindByGuid( playerId );

			if ( playerObject.IsProxy ) { continue; }

			playerObject.Components.Get<IKillable>().Kill();
			break;

		}
	}

	[ConCmd( "killall" )]
	private static void KillAll()
	{

		foreach ( Guid playerId in LethalGameManager.Instance.ConnectedPlayers )
		{
			LethalGameManager.Instance.Scene.Directory.FindByGuid( playerId ).Components.Get<IKillable>().Kill();

		}

	}

}

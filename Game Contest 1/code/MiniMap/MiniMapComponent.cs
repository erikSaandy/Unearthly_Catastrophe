using Saandy;
using Sandbox;
using Sandbox.UI;

public sealed class MiniMapComponent : Component
{

	public static CameraComponent Camera { get; private set; }

	private static Guid SelectedPlayerId { get; set; } = default;
	public static Player SelectedPlayer { get; private set; } = null;

	private static List<IHasMapIcon> Icons { get; set; } = new();

	[Property] public InteractionProxy NextButton { get; set; }
	[Property] public InteractionProxy PrevButton { get; set; }

	public static float Yaw => Camera.GameObject.Transform.Rotation.Angles().yaw;

	public static string SelectedPlayerName => GetSelectedPlayerName();
	private static string GetSelectedPlayerName()
	{
		return (SelectedPlayer != null) ? SelectedPlayer.Network.OwnerConnection.DisplayName : "???";
	}

	public static Texture Texture;

	public static void AddIcon( IHasMapIcon icon )
	{
		Vector2 pos = GetIconPositionOnScreen( icon );

		if(MathF.Abs( icon.Transform.Position.z - Camera.Transform.Position.z ) > 256) { return; }
		if (pos.x <= 0.05f || pos.x >= 0.95f || pos.y <= 0.05f || pos.y >= 0.95f) { return; }

		Icons.Add( icon );

	}

	public static Vector2 GetIconPositionOnScreen(IHasMapIcon icon)
	{
		Vector2 pos = MiniMapComponent.Camera.PointToScreenNormal( icon.Transform.Position );
		float height = MiniMapComponent.Camera.OrthographicHeight;
		float width = MiniMapComponent.Camera.OrthographicHeight * 1.777777f;
		float d = ((width - height) * 0.5f) / width;
		pos.x = Math2d.Map( pos.x, d, 1 - d, 0, 1 );
		return pos;
	}

	[Broadcast]
	public static void SelectPlayer( Guid playerGuid )
	{
		GameObject playerObj = LethalGameManager.Instance.Scene.Directory.FindByGuid( playerGuid );

		if ( playerObj != null )
		{
			Player player = null;
			if ( playerObj.Components.TryGet( out player ) )
			{
				SelectedPlayer = player;
				SelectedPlayerId = SelectedPlayer.GameObject.Id;
			}
		}
	}

	public static void SelectNextPlayer(bool reverse = false)
	{
		IEnumerable<Player> connections = LethalGameManager.Instance.ConnectedPlayers;
		int i = connections.TakeWhile( x => x.GameObject.Id != SelectedPlayerId ).Count();
		int next = (i - 1) % connections.Count();
		if( next < 0) { next = connections.Count() - 1; }

		SelectPlayer( connections.ElementAt( next ).GameObject.Id );
	}

	public static void SelectPreviousPlayer() { SelectNextPlayer( reverse: true ); }

	protected override void OnStart()
	{
		base.OnStart();
		Panel p = new();

		SelectPlayer( LethalGameManager.Instance.ConnectedPlayers.First().GameObject.Id );

		NextButton.OnInteracted += delegate { SelectNextPlayer(); };
		PrevButton.OnInteracted += delegate { SelectPreviousPlayer(); };

		Camera = Components.Get<CameraComponent>( );

		Texture = null;
		Texture = Texture.CreateRenderTarget()
			.WithSize( 512, 512 )
			.WithMips( 1 )
			.WithUAVBinding()
			.WithDynamicUsage()
			.Create( "minimap", true );

	}

	protected override void OnDestroy()
	{
		base.OnDestroy();

		Texture.Dispose();

	}

	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();

		MiniMapHud.UpdateIcons( Icons );
		Icons.Clear();
	}



	protected override void OnPreRender()
	{
		base.OnPreRender();

		if (SelectedPlayer == null) { return; }

		Transform.Position = (SelectedPlayer.Transform.Position + Vector3.Up * 128);
		Transform.Position = Transform.Position.WithZ( Transform.Position.z - Transform.Position.z % 48 );

		//float yaw = SelectedPlayer == null ? Yaw : SelectedPlayer.Transform.Rotation.Angles().yaw;
		//Camera.Transform.Rotation = Camera.Transform.Rotation.Angles().WithYaw( yaw );

		Camera.RenderToTexture( Texture );

	}

}

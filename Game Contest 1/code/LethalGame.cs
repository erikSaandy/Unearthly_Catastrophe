using Sandbox;

public sealed class LethalGame : Component
{
	public static LethalGame Instance { get; private set; }

	[Property] public GameObject CameraPrefab { get; set; }
	[Property] public GameObject AliveHudPrefab { get; set; }

	protected override void OnStart()
	{
		if(Instance == null) { Instance = this; }
		else if(Instance != this) { this.Destroy(); }
	}
}

using Sandbox;

namespace Dungeon;

public sealed class RoomData : Component
{
	[Property] public List<DoorSpawner> Doors { get; private set; }

	protected override void OnUpdate()
	{
	}
}

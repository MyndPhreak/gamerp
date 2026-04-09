using Sandbox;

/// <summary>
/// Player spawner that automatically instantiates the player prefab on game start.
/// Add this component to an empty GameObject in your scene to enable player spawning.
/// </summary>
public sealed class Spawner : Component
{
	[Property]
	public GameObject PlayerPrefab { get; set; }

	[Property]
	public bool SpawnOnStart { get; set; } = true;

	protected override void OnStart()
	{
		if ( SpawnOnStart && PlayerPrefab.IsValid() )
		{
			SpawnPlayer();
		}
	}

	public void SpawnPlayer()
	{
		if ( !PlayerPrefab.IsValid() )
		{
			Log.Warning( "[Spawner] Player prefab is not set!" );
			return;
		}

		var player = PlayerPrefab.Clone( Transform.Position, Transform.Rotation );
		Log.Info( "[Spawner] Player spawned at " + Transform.Position );
	}
}

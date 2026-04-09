using Sandbox;
using GameRP.Interactions;
// using GameRP.UI; // TODO: Uncomment after ClosetScreen is created
using System.Linq;

namespace GameRP.Entities;

/// <summary>
/// Closet that players interact with to customize clothing.
/// Place as a sibling to an Interactable component on the closet prefab.
/// </summary>
public sealed class Closet : Component
{
    private Interactable _interactable;
    private bool _isOpen;

    // Camera state
    private Vector3 _savedCameraPos;
    private Rotation _savedCameraRot;
    private float _orbitAngle;
    private float _orbitDistance = 150f;
    private float _orbitHeight = 50f;

    // Fill light
    private GameObject _fillLight;

    // Player reference
    private GameObject _player;
    private PlayerController _playerController;

    protected override void OnStart()
    {
        _interactable = Components.Get<Interactable>();
        if ( _interactable != null )
        {
            _interactable.OnInteract = OnClosetInteract;
        }
        else
        {
            Log.Warning( "[Closet] No Interactable component found - add one in the editor" );
        }
    }

    private void OnClosetInteract()
    {
        if ( _isOpen ) return;

        Log.Info( "[Closet] Player opened closet" );

        // Find the local player
        _player = Scene.GetAllComponents<PlayerController>()
            .FirstOrDefault( pc => !pc.IsProxy )?.GameObject;

        if ( _player == null )
        {
            Log.Warning( "[Closet] Could not find local player" );
            return;
        }

        _playerController = _player.Components.Get<PlayerController>();

        Open();
    }

    private void Open()
    {
        _isOpen = true;

        // Disable player movement
        if ( _playerController != null )
            _playerController.UseInputControls = false;

        // Save current camera state
        var camera = Scene.Camera;
        if ( camera != null )
        {
            _savedCameraPos = camera.WorldPosition;
            _savedCameraRot = camera.WorldRotation;
        }

        // Set initial orbit angle (face the player from the front)
        _orbitAngle = _player.WorldRotation.Yaw() + 180f;

        // Spawn fill light
        SpawnFillLight();

        // Position camera
        UpdateOrbitCamera();

        // Show cursor
        Mouse.Visibility = MouseVisibility.Visible;

        // TODO: Uncomment after ClosetScreen is created
        // Open the closet screen
        // var closetScreen = Scene.GetAllComponents<ClosetScreen>().FirstOrDefault();
        // if ( closetScreen != null )
        // {
        //     var rpPlayer = _player.Components.Get<RPPlayer>();
        //     closetScreen.Open( rpPlayer, this );
        // }
        // else
        // {
        //     Log.Error( "[Closet] Could not find ClosetScreen component in scene!" );
        // }
    }

    public void Close()
    {
        _isOpen = false;

        // Re-enable player movement
        if ( _playerController != null )
            _playerController.UseInputControls = true;

        // Remove fill light
        _fillLight?.Destroy();
        _fillLight = null;

        // Restore cursor
        Mouse.Visibility = MouseVisibility.Auto;
    }

    private void SpawnFillLight()
    {
        if ( _player == null ) return;

        _fillLight = new GameObject( true, "ClosetFillLight" );
        var light = _fillLight.Components.Create<PointLight>();
        light.LightColor = Color.White;
        light.Radius = 500f;

        // Position above and in front of the player
        var forward = Rotation.FromYaw( _orbitAngle ).Forward;
        _fillLight.WorldPosition = _player.WorldPosition + Vector3.Up * 120f + forward * 80f;
    }

    private void UpdateOrbitCamera()
    {
        var camera = Scene.Camera;
        if ( camera == null || _player == null ) return;

        var center = _player.WorldPosition + Vector3.Up * _orbitHeight;
        var direction = Rotation.FromYaw( _orbitAngle ).Forward;
        var targetPos = center + direction * _orbitDistance;

        camera.WorldPosition = targetPos;
        camera.WorldRotation = Rotation.LookAt( center - targetPos );
    }

    protected override void OnUpdate()
    {
        if ( !_isOpen ) return;

        // Mouse drag to orbit
        if ( Input.Down( "attack1" ) )
        {
            _orbitAngle += Mouse.Delta.x * 0.3f;
            UpdateOrbitCamera();
        }
    }

    protected override void OnDestroy()
    {
        if ( _isOpen )
        {
            Close();
        }
    }
}

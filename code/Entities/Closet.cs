using Sandbox;
using GameRP.Interactions;
using GameRP.UI;
using System.Linq;

namespace GameRP.Entities;

/// <summary>
/// Closet that players interact with to customize clothing.
/// Place as a sibling to an Interactable component on the closet prefab.
/// ClosetScreen should be on the scene's HUD GameObject (same as AtmScreen).
/// </summary>
public sealed class Closet : Component
{
    /// <summary>
    /// Optional: assign a child GameObject as the stand point.
    /// The player will be teleported here when the closet opens.
    /// </summary>
    [Property] public GameObject PlayerStandPoint { get; set; }

    private Interactable _interactable;
    private bool _isOpen;

    // Camera — computed once on open, never moves
    private float _orbitDistance = 150f;
    private float _orbitHeight = 50f;
    private CameraComponent _camera;
    private Vector3 _fixedCamPos;
    private Rotation _fixedCamRot;

    // Player rotation — drag rotates the character, camera stays fixed
    private float _savedPlayerYaw;
    private float _previewYaw;

    // Fill light
    private GameObject _fillLight;

    // Player references
    private GameObject _player;
    private PlayerController _playerController;
    private CharacterController _characterController;
    private SkinnedModelRenderer _bodyRenderer;

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

        _player = Scene.GetAllComponents<PlayerController>()
            .FirstOrDefault( pc => !pc.IsProxy )?.GameObject;

        if ( _player == null )
        {
            Log.Warning( "[Closet] Could not find local player" );
            return;
        }

        _playerController = _player.Components.Get<PlayerController>();
        _characterController = _player.Components.Get<CharacterController>();
        _camera = _player.Components.GetInChildren<CameraComponent>();
        _bodyRenderer = _player.Components.GetInChildren<SkinnedModelRenderer>();

        Open();
    }

    private void Open()
    {
        _isOpen = true;

        // Teleport player to stand point first (before disabling anything)
        if ( PlayerStandPoint != null )
        {
            _player.WorldPosition = PlayerStandPoint.WorldPosition;
            _player.WorldRotation = PlayerStandPoint.WorldRotation;
        }

        // Stop all movement — zero velocity on both controllers
        if ( _playerController != null )
        {
            _playerController.WishVelocity = Vector3.Zero;
            _playerController.UseInputControls = false;
            _playerController.UseLookControls = false;
            _playerController.HideBodyInFirstPerson = false;
        }

        if ( _characterController != null )
        {
            _characterController.Velocity = Vector3.Zero;
            _characterController.Enabled = false;
        }

        // Save player yaw for restore on close
        _savedPlayerYaw = _player.WorldRotation.Yaw();
        _previewYaw = _savedPlayerYaw;

        // Compute fixed camera position once — camera never moves, player rotates
        var camDir = Rotation.FromYaw( _previewYaw + 180f ).Forward;
        var center = _player.WorldPosition + Vector3.Up * _orbitHeight;
        _fixedCamPos = center + camDir * _orbitDistance;
        _fixedCamRot = Rotation.LookAt( center - _fixedCamPos );

        // Spawn fill light in front of player (from camera side)
        SpawnFillLight();

        // Show cursor
        Mouse.Visibility = MouseVisibility.Visible;

        // Find and open the ClosetScreen (on the HUD GameObject in the scene)
        var closetScreen = Scene.GetAllComponents<ClosetScreen>().FirstOrDefault();
        if ( closetScreen != null )
        {
            var rpPlayer = _player.Components.Get<RPPlayer>();
            closetScreen.Open( rpPlayer, this );
        }
        else
        {
            Log.Error( "[Closet] No ClosetScreen found in scene!" );
        }
    }

    public void Close()
    {
        _isOpen = false;

        // Restore player rotation to before the closet was opened
        if ( _player != null )
            _player.WorldRotation = Rotation.FromYaw( _savedPlayerYaw );

        // Re-enable player movement
        if ( _characterController != null )
        {
            _characterController.Enabled = true;
        }

        if ( _playerController != null )
        {
            _playerController.UseInputControls = true;
            _playerController.UseLookControls = true;
            _playerController.HideBodyInFirstPerson = true;
        }

        // Restore body render type
        if ( _bodyRenderer != null )
            _bodyRenderer.RenderType = ModelRenderer.ShadowRenderType.On;

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

        // Position above and slightly behind the camera (illuminating the player from the front)
        var camDir = Rotation.FromYaw( _previewYaw + 180f ).Forward;
        _fillLight.WorldPosition = _player.WorldPosition + Vector3.Up * 120f + camDir * 60f;
    }

    private void UpdateCamera()
    {
        if ( _camera == null ) return;

        // Apply the fixed camera position — computed once on open, never updated
        _camera.GameObject.WorldPosition = _fixedCamPos;
        _camera.GameObject.WorldRotation = _fixedCamRot;
    }

    protected override void OnUpdate()
    {
        if ( !_isOpen ) return;

        // Right mouse drag rotates the player character
        // (right mouse avoids conflict with UI left-click interactions)
        if ( Input.Down( "attack2" ) )
        {
            _previewYaw += Mouse.Delta.x * 0.3f;
            _player.WorldRotation = Rotation.FromYaw( _previewYaw );
        }
    }

    /// <summary>
    /// Runs after all OnUpdate calls, right before rendering.
    /// Overrides camera AFTER PlayerController has positioned it.
    /// </summary>
    protected override void OnPreRender()
    {
        if ( !_isOpen ) return;

        UpdateCamera();

        // Force body visible in case PlayerController tries to hide it
        if ( _bodyRenderer != null )
            _bodyRenderer.RenderType = ModelRenderer.ShadowRenderType.On;

    }

    protected override void OnDestroy()
    {
        if ( _isOpen )
            Close();
    }
}

using Sandbox;
using GameRP.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

/// <summary>
/// Orchestrates the character creation wizard flow.
/// Place this component in the character-creation scene alongside a Spawner,
/// DatabaseService, ClosetScreen, and LicenseScreen.
/// </summary>
public sealed class CharacterCreationManager : Component
{
	[Property] public GameObject CameraPosition { get; set; }
	[Property] public GameObject LicenseStationPosition { get; set; }
	[Property] public float CameraHeightOffset { get; set; } = 50f;
	[Property] public float CameraDistance { get; set; } = 150f;
	[Property] public float CameraOffset { get; set; } = 100f;
	[Property] public float CameraRotationOffset { get; set; } = 180f;
	[Property] public float CameraPitch { get; set; } = 0f;
	[Property] public float MinCameraDistance { get; set; } = 50f;
	[Property] public float MaxCameraDistance { get; set; } = 500f;
	[Property] public float OrbitSensitivity { get; set; } = 0.3f;
	[Property] public float ZoomSensitivity { get; set; } = 0.05f;


	private RPPlayer _rpPlayer;
	private PlayerController _playerController;
	private CameraComponent _camera;
	private SkinnedModelRenderer _bodyRenderer;

	// Cached scene component references
	private ClosetScreen _closetScreen;
	private LicenseScreen _licenseScreen;
	private Spawner _spawner;

	// Wizard state carried between steps
	private string _wizardGender = "Male";
	private float _wizardSkinTone = 0.5f;
	private float _wizardHeight = 0.5f;
	private float _wizardAge = 0.5f;
	private Dictionary<string, string> _wizardClothing = new();

	private bool _initialized;
	private Rotation _basePlayerRotation;

	protected override void OnUpdate()
	{
		if ( _initialized ) return;

		// Wait for the player to be spawned and ready
		_rpPlayer = Scene.GetAllComponents<RPPlayer>()
			.FirstOrDefault( rp => !rp.IsProxy );

		if ( _rpPlayer == null ) return;

		_initialized = true;
		StartWizard();
	}

	private void StartWizard()
	{
		Log.Info( "[CharacterCreation] Starting character creation wizard" );
		_basePlayerRotation = _rpPlayer.GameObject.WorldRotation;

		_playerController = _rpPlayer.Components.Get<PlayerController>();
		_camera = _rpPlayer.Components.GetInChildren<CameraComponent>();
		_bodyRenderer = _rpPlayer.Components.GetInChildren<SkinnedModelRenderer>();

		// Cache scene component references
		_closetScreen = Scene.GetAllComponents<ClosetScreen>().FirstOrDefault();
		_licenseScreen = Scene.GetAllComponents<LicenseScreen>().FirstOrDefault();
		_spawner = Scene.GetAllComponents<Spawner>().FirstOrDefault();

		if ( _closetScreen == null ) { Log.Error( "[CharacterCreation] No ClosetScreen found in scene!" ); return; }
		if ( _licenseScreen == null ) { Log.Error( "[CharacterCreation] No LicenseScreen found in scene!" ); return; }

		// Disable player movement
		if ( _playerController != null )
		{
			_playerController.UseInputControls = false;
			_playerController.UseLookControls = false;
			_playerController.HideBodyInFirstPerson = false;
		}

		var cc = _rpPlayer.Components.Get<CharacterController>();
		if ( cc != null )
		{
			cc.Velocity = Vector3.Zero;
			cc.Enabled = false;
		}

		// Show cursor
		Mouse.Visibility = MouseVisibility.Visible;

		// Set up camera to view the player
		SetupCamera();

		// Open the closet in wizard mode
		OpenClosetStep();
	}

	private void SetupCamera()
	{
		if ( _camera == null || _rpPlayer == null ) return;

		var playerPos = _rpPlayer.GameObject.WorldPosition;
		var playerRot = _basePlayerRotation;

		// Apply the CameraRotationOffset as a yaw to orbit the rig around the character
		var orbitRot = playerRot * Rotation.FromYaw( CameraRotationOffset );

		// The citizen model faces opposite to the entity's Forward axis. We preserve
		// the original relative math but evaluate it against the new pivoted offset.
		var camDir = -orbitRot.Right;

		var center = playerPos + Vector3.Up * CameraHeightOffset + orbitRot.Forward * CameraOffset;
		var camPos = center + camDir * CameraDistance;

		_camera.GameObject.WorldPosition = camPos;

		var baseLookRtn = Rotation.LookAt( center - camPos );
		_camera.GameObject.WorldRotation = baseLookRtn * Rotation.From( CameraPitch, 0f, 0f );
	}

	private void UpdatePlayerFacing()
	{
		if ( _rpPlayer == null || !_rpPlayer.IsValid() || _camera == null ) return;

		var playerPos = _rpPlayer.GameObject.WorldPosition;
		var camPos = _camera.GameObject.WorldPosition;

		// Horizontal direction from player to camera
		var toCamera = new Vector3( camPos.x - playerPos.x, camPos.y - playerPos.y, 0f );
		if ( toCamera.LengthSquared < 0.01f ) return;
		toCamera = toCamera.Normal;

		// Player's natural forward (horizontal only)
		var fwd = _basePlayerRotation.Forward;
		var naturalForward = new Vector3( fwd.x, fwd.y, 0f ).Normal;

		// dot >= cos(60°) means camera is within 60° of the player's natural front
		var dot = Vector3.Dot( naturalForward, toCamera );
		if ( dot >= 0.5f )
		{
			// Face the camera (yaw only — keep player upright)
			_rpPlayer.GameObject.WorldRotation = Rotation.LookAt( toCamera, Vector3.Up );
		}
		else
		{
			// Camera is to the side or behind — restore natural forward
			_rpPlayer.GameObject.WorldRotation = _basePlayerRotation;
		}
	}

	private void OpenClosetStep()
	{
		if ( _closetScreen == null ) return;

		_closetScreen.OnWizardNext = OnClosetNext;
		_closetScreen.OpenWizard( _rpPlayer );
	}

	private void OnClosetNext( string gender, float skinTone, float height, float age, Dictionary<string, string> clothing )
	{
		if ( _rpPlayer == null || !_rpPlayer.IsValid() )
		{
			Log.Error( "[CharacterCreation] RPPlayer was destroyed before wizard completed." );
			return;
		}

		Log.Info( $"[CharacterCreation] Closet step complete: gender={gender}" );

		// Store wizard state
		_wizardGender = gender;
		_wizardSkinTone = skinTone;
		_wizardHeight = height;
		_wizardAge = age;
		_wizardClothing = clothing;

		// Apply to RPPlayer so the model reflects choices
		_rpPlayer.Gender = gender;
		_rpPlayer.SkinTone = skinTone;
		_rpPlayer.Height = height;
		_rpPlayer.Age = age;
		_rpPlayer.EquippedClothing = clothing.Values.ToList();

		// Move to license station if a position is set
		if ( LicenseStationPosition != null )
		{
			_rpPlayer.GameObject.WorldPosition = LicenseStationPosition.WorldPosition;
			_rpPlayer.GameObject.WorldRotation = LicenseStationPosition.WorldRotation;
			_basePlayerRotation = LicenseStationPosition.WorldRotation;
			SetupCamera();
		}

		// Open the license form
		OpenLicenseStep();
	}

	private void OpenLicenseStep()
	{
		if ( _licenseScreen == null ) return;

		_licenseScreen.OnBack = OnLicenseBack;
		_licenseScreen.OnPrintLicense = OnLicensePrint;
		_licenseScreen.Open( _rpPlayer, _wizardGender );
	}

	private void OnLicenseBack()
	{
		if ( _rpPlayer == null || !_rpPlayer.IsValid() )
		{
			Log.Error( "[CharacterCreation] RPPlayer was destroyed before wizard completed." );
			return;
		}

		Log.Info( "[CharacterCreation] Going back to closet step" );

		// Always return player to spawner origin regardless of LicenseStationPosition
		if ( _spawner != null )
		{
			_rpPlayer.GameObject.WorldPosition = _spawner.GameObject.WorldPosition;
			_rpPlayer.GameObject.WorldRotation = _spawner.GameObject.WorldRotation;
			_basePlayerRotation = _spawner.GameObject.WorldRotation;
		}
		SetupCamera();

		// Re-open closet with preserved state
		if ( _closetScreen != null )
		{
			_closetScreen.OnWizardNext = OnClosetNext;
			_closetScreen.ReopenWizard();
		}
	}

	private async void OnLicensePrint( string displayName, string dateOfBirth, string gender )
	{
		try
		{
			if ( _rpPlayer == null || !_rpPlayer.IsValid() )
			{
				Log.Error( "[CharacterCreation] RPPlayer was destroyed before wizard completed." );
				return;
			}

			Log.Info( $"[CharacterCreation] License printed for {displayName}" );

			// Update RPPlayer with all final data
			_rpPlayer.DisplayName = displayName;
			_rpPlayer.DateOfBirth = dateOfBirth;
			_rpPlayer.Gender = gender;
			_rpPlayer.SkinTone = _wizardSkinTone;
			_rpPlayer.Height = _wizardHeight;
			_rpPlayer.Age = _wizardAge;
			_rpPlayer.EquippedClothing = _wizardClothing.Values.ToList();
			_rpPlayer.HasCompletedCharacterCreation = true;

			// Apply appearance to dresser
			_rpPlayer.ApplyClothingToDresser();
			_rpPlayer.ApplyBodyToDresser();

			// Save to database
			var data = new PlayerData
			{
				SteamId = Game.SteamId,
				DisplayName = displayName,
				Gender = gender,
				DateOfBirth = dateOfBirth,
				SkinTone = _wizardSkinTone,
				Height = _wizardHeight,
				Age = _wizardAge,
				Money = _rpPlayer.Money,
				JobTitle = _rpPlayer.JobTitle,
				ClothingList = JsonSerializer.Serialize( _rpPlayer.EquippedClothing ),
				HasCompletedCharacterCreation = true,
				LastSeen = DateTime.Now
			};

			DatabaseService.Instance?.SavePlayer( data );

			// Brief delay for the flash effect to complete, then load main scene
			await Task.Delay( 500 );

			Log.Info( "[CharacterCreation] Loading main map..." );
			var mainScene = ResourceLibrary.Get<SceneFile>( "Assets/scenes/minimal.scene" );
			if ( mainScene != null )
				Scene.Load( mainScene );
			else
				Log.Error( "[CharacterCreation] Could not find Assets/scenes/minimal.scene" );
		}
		catch ( Exception ex )
		{
			Log.Error( $"[CharacterCreation] Failed to complete character creation: {ex.Message}" );
		}
	}

	protected override void OnPreRender()
	{
		if ( !_initialized ) return;

		if ( _bodyRenderer != null )
			_bodyRenderer.RenderType = ModelRenderer.ShadowRenderType.On;

		// Re-apply camera placement every frame — PlayerController.OnUpdate repositions
		// the camera each tick, so we must override it here (OnPreRender wins the race).
		SetupCamera();
		UpdatePlayerFacing();
	}
}

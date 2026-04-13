using Sandbox;
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

		// Position camera to view the player from the front
		var playerPos = _rpPlayer.GameObject.WorldPosition;
		var playerRot = _rpPlayer.GameObject.WorldRotation;
		var camDir = playerRot.Forward;

		var center = playerPos + Vector3.Up * CameraHeightOffset;
		var camPos = center + camDir * CameraDistance;

		_camera.GameObject.WorldPosition = camPos;
		_camera.GameObject.WorldRotation = Rotation.LookAt( center - camPos );
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
			var mainScene = ResourceLibrary.Get<SceneFile>( "scenes/minimal.scene" );
			if ( mainScene != null )
				Scene.Load( mainScene );
			else
				Log.Error( "[CharacterCreation] Could not find scenes/minimal.scene" );
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
	}
}

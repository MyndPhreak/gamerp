using Sandbox;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace GameRP.CharacterCreation;

/// <summary>
/// Orchestrates the character creation wizard flow.
/// Place this component in the character-creation scene alongside a Spawner,
/// DatabaseService, ClosetScreen, and LicenseScreen.
/// </summary>
public sealed class CharacterCreationManager : Component
{
	[Property] public GameObject CameraPosition { get; set; }
	[Property] public GameObject LicenseStationPosition { get; set; }

	private RPPlayer _rpPlayer;
	private PlayerController _playerController;
	private CameraComponent _camera;
	private SkinnedModelRenderer _bodyRenderer;

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

		var center = playerPos + Vector3.Up * 50f;
		var camPos = center + camDir * 150f;

		_camera.GameObject.WorldPosition = camPos;
		_camera.GameObject.WorldRotation = Rotation.LookAt( center - camPos );
	}

	private void OpenClosetStep()
	{
		var closetScreen = Scene.GetAllComponents<ClosetScreen>().FirstOrDefault();
		if ( closetScreen == null )
		{
			Log.Error( "[CharacterCreation] No ClosetScreen found in scene!" );
			return;
		}

		closetScreen.OnWizardNext = OnClosetNext;
		closetScreen.OpenWizard( _rpPlayer );
	}

	private void OnClosetNext( string gender, float skinTone, float height, float age, Dictionary<string, string> clothing )
	{
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
		var licenseScreen = Scene.GetAllComponents<LicenseScreen>().FirstOrDefault();
		if ( licenseScreen == null )
		{
			Log.Error( "[CharacterCreation] No LicenseScreen found in scene!" );
			return;
		}

		licenseScreen.OnBack = OnLicenseBack;
		licenseScreen.OnPrintLicense = OnLicensePrint;
		licenseScreen.Open( _rpPlayer, _wizardGender );
	}

	private void OnLicenseBack()
	{
		Log.Info( "[CharacterCreation] Going back to closet step" );

		// Move player back if needed
		if ( LicenseStationPosition != null )
		{
			var spawner = Scene.GetAllComponents<Spawner>().FirstOrDefault();
			if ( spawner != null )
			{
				_rpPlayer.GameObject.WorldPosition = spawner.GameObject.WorldPosition;
				_rpPlayer.GameObject.WorldRotation = spawner.GameObject.WorldRotation;
			}
			SetupCamera();
		}

		// Re-open closet with preserved state
		var closetScreen = Scene.GetAllComponents<ClosetScreen>().FirstOrDefault();
		if ( closetScreen != null )
		{
			closetScreen.OnWizardNext = OnClosetNext;
			closetScreen.ReopenWizard();
		}
	}

	private async void OnLicensePrint( string displayName, string dateOfBirth, string gender )
	{
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
			LastSeen = System.DateTime.Now
		};

		DatabaseService.Instance?.SavePlayer( data );

		// Brief delay for the flash effect to complete, then load main scene
		await Task.Delay( 500 );

		Log.Info( "[CharacterCreation] Loading main map..." );
		Scene.Load( "scenes/minimal.scene" );
	}

	protected override void OnPreRender()
	{
		// Keep camera locked and body visible
		if ( !_initialized ) return;

		SetupCamera();

		if ( _bodyRenderer != null )
			_bodyRenderer.RenderType = ModelRenderer.ShadowRenderType.On;
	}
}

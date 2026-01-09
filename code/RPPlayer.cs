using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public sealed class RPPlayer : Component
{
    [Property] public int Money { get; set; } = 100;
    [Property] public string JobTitle { get; set; } = "Unemployed";
    [Property] public Color JobColor { get; set; } = Color.Gray;

    [Property, Group( "Movement" )] public float WalkSpeed { get; set; } = 150.0f;
    [Property, Group( "Movement" )] public float RunSpeed { get; set; } = 300.0f;
    [Property, Group( "Movement" )] public float CrouchSpeed { get; set; } = 80.0f;
    [Property, Group( "Movement" )] public float JumpStrength { get; set; } = 300.0f;
    [Property, Group( "Movement" )] public float EyeHeight { get; set; } = 64.0f;

    [Property, Group( "Camera" )] public bool FirstPerson { get; set; } = true;
    [Property, Group( "Camera" )] public float MouseSensitivity { get; set; } = 0.1f;
    [Property, Group( "Camera" )] public float ThirdPersonDistance { get; set; } = 150.0f;

    [Property, Group( "References" )] public GameObject Body { get; set; }
    [Property, Group( "References" )] public GameObject Eye { get; set; }
    [Property, Group( "References" )] public CameraComponent Camera { get; set; }

    private CharacterController _controller;
    private SkinnedModelRenderer _bodyRenderer;
    private Angles _eyeAngles;
    private bool _isCrouching;

    public List<Sandbox.UI.Tablet.BankLogEntry> BankLogs { get; private set; } = new();

    public void RecordTransaction( string title, int amount )
    {
        Money += amount;
        BankLogs.Insert( 0, new Sandbox.UI.Tablet.BankLogEntry 
        { 
            Title = title, 
            Amount = amount, 
            Time = DateTime.Now.ToString( "HH:mm" ) 
        } );
        
        if ( BankLogs.Count > 50 )
            BankLogs.RemoveAt( BankLogs.Count - 1 );

        SaveToDatabase();
        Sandbox.Services.Stats.SetValue( "money", Money );
    }

    private void SaveToDatabase()
    {
        if ( IsProxy ) return;
        
        var data = new PlayerData
        {
            SteamId = Game.SteamId,
            Name = Game.SteamId.ToString(), // TODO: Get display name
            Money = Money,
            JobTitle = JobTitle,
            LastSeen = DateTime.Now
        };

        DatabaseService.Instance?.SavePlayer( data );
    }

    private async void LoadFromDatabase()
    {
        if ( IsProxy ) return;

        var data = await DatabaseService.Instance?.GetPlayer( Game.SteamId );
        if ( data != null )
        {
            Money = data.Money;
            JobTitle = data.JobTitle;
            Log.Info( $"Loaded player data for {data.Name}: ${data.Money}" );
        }
        else
        {
            Log.Info( "No existing player data found, starting fresh." );
        }
    }

    protected override void OnAwake()
    {
        // Ensure we have a CharacterController
        _controller = Components.GetOrCreate<CharacterController>();
        _controller.Height = 72;
        _controller.Radius = 16;

        // Get body renderer if we have a body
        if ( Body != null )
        {
            _bodyRenderer = Body.Components.Get<SkinnedModelRenderer>();
        }

        // Auto-create camera if not assigned
        if ( Camera == null )
        {
            var cameraGO = Scene.CreateObject();
            cameraGO.Name = "Player Camera";
            cameraGO.SetParent( GameObject );

            Camera = cameraGO.Components.Create<CameraComponent>();
            Camera.IsMainCamera = true;

            Log.Info( "[RPPlayer] Auto-created camera" );
        }

        // Auto-add InteractionManager for interacting with objects
        var interactionManager = Components.Get<GameRP.Interactions.InteractionManager>();
        if ( interactionManager == null )
        {
            interactionManager = Components.Create<GameRP.Interactions.InteractionManager>();
            interactionManager.Camera = Camera;
            interactionManager.ShowDebug = false;

            Log.Info( "[RPPlayer] Auto-created InteractionManager" );
        }
    }

    protected override void OnStart()
    {
        if ( !IsProxy )
        {
            LoadFromDatabase();

            // Lock mouse by default
            Mouse.Visibility = MouseVisibility.Hidden;
        }
    }

    private TimeSince _lastSave = 0;

    protected override void OnUpdate()
    {
        if ( IsProxy ) return;

        // Toggle mouse visibility for UI
        if ( Input.Pressed( "Menu" ) )
        {
            if ( Mouse.Visibility == MouseVisibility.Hidden )
                Mouse.Visibility = MouseVisibility.Visible;
            else
                Mouse.Visibility = MouseVisibility.Hidden;
        }

        if ( Mouse.Visibility == MouseVisibility.Hidden )
        {
            _eyeAngles.pitch += Input.MouseDelta.y * MouseSensitivity;
            _eyeAngles.yaw -= Input.MouseDelta.x * MouseSensitivity;
            _eyeAngles.pitch = _eyeAngles.pitch.Clamp( -89, 89 );
        }

        WorldRotation = Rotation.FromYaw( _eyeAngles.yaw );

        UpdateCamera();

        UpdateAnimation();
        
        if ( _lastSave > 30f )
        {
            SaveToDatabase();
            Sandbox.Services.Stats.SetValue( "money", Money );
            _lastSave = 0;
        }
    }

    protected override void OnFixedUpdate()
    {
        if ( IsProxy ) return;
        if ( _controller == null ) return;

        _isCrouching = Input.Down( "Crouch" );
        
        var wishDir = Input.AnalogMove.Normal;
        float speed = WalkSpeed;
        if ( _isCrouching ) speed = CrouchSpeed;
        else if ( Input.Down( "Run" ) ) speed = RunSpeed;

        var velocity = WorldRotation * wishDir * speed;

        if ( _controller.IsOnGround )
        {
            _controller.Accelerate( velocity );
            _controller.ApplyFriction( 5.0f );

            if ( Input.Pressed( "Jump" ) )
            {
                _controller.Punch( Vector3.Up * JumpStrength );
                _bodyRenderer?.Set( "b_jump", true );
            }
        }
        else
        {
            _controller.Velocity += Scene.PhysicsWorld.Gravity * Time.Delta;
            _controller.Accelerate( velocity * 0.2f );
        }

        _controller.Move();
    }

    private void UpdateCamera()
    {
        if ( Camera == null ) return;

        // Get eye position - use Eye GameObject if available, otherwise offset from player position
        var eyePosition = Eye != null ? Eye.WorldPosition : WorldPosition + Vector3.Up * EyeHeight;

        if ( FirstPerson )
        {
            // First person - camera at eye position
            Camera.WorldPosition = eyePosition;
            Camera.WorldRotation = Rotation.From( _eyeAngles.pitch, _eyeAngles.yaw, 0 );

            // Hide body in first person if we have one
            if ( Body != null )
                Body.Enabled = false;
        }
        else
        {
            // Third person - camera behind player
            var targetPos = eyePosition;
            Camera.WorldPosition = targetPos + WorldRotation.Backward * ThirdPersonDistance + WorldRotation.Up * 10;
            Camera.WorldRotation = Rotation.From( _eyeAngles.pitch, _eyeAngles.yaw, 0 );

            // Show body in third person
            if ( Body != null )
                Body.Enabled = true;
        }
    }

    private void UpdateAnimation()
    {
        if ( _bodyRenderer == null ) return;

        var moveVec = Input.AnalogMove;
        
        _bodyRenderer.Set( "move_x", moveVec.x );
        _bodyRenderer.Set( "move_y", moveVec.y );
        _bodyRenderer.Set( "move_direction", Rotation.LookAt( moveVec ).Yaw() );
        _bodyRenderer.Set( "move_speed", _controller.Velocity.Length );
        _bodyRenderer.Set( "move_groundspeed", _controller.Velocity.WithZ( 0 ).Length );
        _bodyRenderer.Set( "is_grounded", _controller.IsOnGround );
        _bodyRenderer.Set( "duck", _isCrouching ? 1.0f : 0.0f );
        
        // Look at rotation for the head
        _bodyRenderer.Set( "lookat_pos", Camera.WorldPosition + Camera.WorldRotation.Forward * 1000 );
    }
}


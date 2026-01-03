using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;

public sealed class RPPlayer : Component
{
    [Property] public int Money { get; set; } = 100;
    [Property] public string JobTitle { get; set; } = "Unemployed";
    [Property] public Color JobColor { get; set; } = Color.Gray;

    [Property] public float WalkSpeed { get; set; } = 150.0f;
    [Property] public float RunSpeed { get; set; } = 300.0f;

    private CharacterController _controller;
    private Angles _eyeAngles;

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
        
        // Keep logs at a reasonable size
        if ( BankLogs.Count > 50 )
            BankLogs.RemoveAt( BankLogs.Count - 1 );

        // Force a save to cloud stats
        Sandbox.Services.Stats.SetValue( "money", Money );
    }

    protected override void OnAwake()
    {
        _controller = Components.Get<CharacterController>();
    }

    protected override void OnStart()
    {
        // Load initial money from cloud stats if we are the local player
        if ( !IsProxy )
        {
            var stats = Sandbox.Services.Stats.LocalPlayer.Get( "money" );
            if ( stats.Sum > 0 )
            {
                Money = (int)stats.Sum;
                Log.Info( $"RPPlayer loaded cached balance of ${Money}" );
            }
        }

        Log.Info( $"RPPlayer started with ${Money} as {JobTitle}" );
    }

    private TimeSince _lastSave = 0;

    protected override void OnUpdate()
    {
        // Save current money to cloud stats every 30 seconds
        if ( !IsProxy && _lastSave > 30f )
        {
            Sandbox.Services.Stats.SetValue( "money", Money );
            _lastSave = 0;
            // Note: Sandbox.Services.Stats automatically batches and sends updates
        }
        // Update eye angles based on mouse input
        _eyeAngles.pitch += Input.MouseDelta.y * 0.1f;
        _eyeAngles.yaw -= Input.MouseDelta.x * 0.1f;
        _eyeAngles.pitch = _eyeAngles.pitch.Clamp( -89, 89 );

        // Rotate the player to match the view yaw
        Transform.Rotation = Rotation.FromYaw( _eyeAngles.yaw );

        var camera = GameObject.GetComponentInChildren<CameraComponent>();
        if ( camera != null )
        {
            // Position the camera behind the player (Third Person)
            camera.Transform.LocalPosition = new Vector3( -150, 0, 80 );
            camera.Transform.LocalRotation = Rotation.From( _eyeAngles.pitch, 0, 0 );
        }
    }

    protected override void OnFixedUpdate()
    {
        if ( _controller == null ) return;

        var wishDir = Input.AnalogMove.Normal;
        var wishSpeed = Input.Down( "Run" ) ? RunSpeed : WalkSpeed;
        
        // Move relative to the player's rotation
        var velocity = Transform.Rotation * wishDir * wishSpeed;

        if ( _controller.IsOnGround )
        {
            _controller.Accelerate( velocity );
            _controller.ApplyFriction( 5.0f );

            if ( Input.Pressed( "Jump" ) )
            {
                _controller.Punch( Vector3.Up * 300.0f );
            }
        }
        else
        {
            _controller.Velocity += Scene.PhysicsWorld.Gravity * Time.Delta;
            _controller.Accelerate( velocity * 0.2f ); // Reduced control in air
        }

        _controller.Move();
    }
}

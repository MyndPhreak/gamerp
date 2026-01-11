using Sandbox;
using System;
using System.Linq;

public sealed class RPManager : Component
{
    [Property] public float PaydayInterval { get; set; } = 60.0f;
    [Property] public int PaydayAmount { get; set; } = 50;

    private float _nextPayday;

    protected override void OnStart()
    {
        _nextPayday = Time.Now + PaydayInterval;
    } 

    protected override void OnUpdate()  
    {  
        if ( Time.Now >= _nextPayday )
        {
            GivePayday(); 
            _nextPayday = Time.Now + PaydayInterval;
        }
    }

    private void GivePayday()
    {
        // For a minimal example, we find the local player and give them money
        // In a real game, you might iterate over all players or handle this server-side
        var player = Scene.GetAll<RPPlayer>().FirstOrDefault();
        if ( player != null )
        {
            player.RecordTransaction( "Payday", PaydayAmount );
            Log.Info( $"Payday! ${PaydayAmount} added. New balance: ${player.Money}" );
        }
    }
}

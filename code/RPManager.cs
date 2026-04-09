using Sandbox;
using GameRP.Systems;
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

    private async void GivePayday()
    {
        var player = Scene.GetAll<RPPlayer>().FirstOrDefault();
        if ( player == null ) return;

        try
        {
            var result = await EconomySystem.Deposit( Game.SteamId, PaydayAmount, "Payday - Direct Deposit" );
            if ( result != null )
            {
                player.RecordBankLog( "Payday", PaydayAmount );
                Log.Info( $"Payday! ${PaydayAmount} deposited to bank. Bank balance: ${result.Balance}" );
            }
            else
            {
                Log.Warning( "Payday deposit failed - null result from API" );
            }
        }
        catch ( Exception ex )
        {
            Log.Warning( $"Payday deposit failed: {ex.Message}" );
        }
    }
}

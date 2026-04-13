using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using GameRP.Inventory;

/// <summary>
/// RP-specific player logic (economy, database, jobs).
/// Add this as a sibling component alongside the built-in PlayerController on your player prefab.
/// PlayerController handles all movement, camera, input, and animation automatically.
/// </summary>
public sealed class RPPlayer : Component
{
    [Property] public int Money { get; set; } = 100;
    [Property] public string JobTitle { get; set; } = "Unemployed";
    [Property] public Color JobColor { get; set; } = Color.Gray;

    [Property] public List<string> EquippedClothing { get; set; } = new();

    [Property] public string DisplayName { get; set; } = "";
    [Property] public string Gender { get; set; } = "Male";
    [Property] public string DateOfBirth { get; set; } = "";
    [Property] public float SkinTone { get; set; } = 0.5f;
    [Property] public float Height { get; set; } = 0.5f;
    [Property] public float Age { get; set; } = 0.5f;
    [Property] public bool HasCompletedCharacterCreation { get; set; }

    private PlayerController _playerController;
    private Dresser _dresser;

    public List<Sandbox.UI.Tablet.BankLogEntry> BankLogs { get; private set; } = new();

    public void RecordTransaction( string title, int amount )
    {
        Money += amount;
        RecordBankLog( title, amount );
        SaveToDatabase();
        Sandbox.Services.Stats.SetValue( "money", Money );
    }

    public void RecordBankLog( string title, int amount )
    {
        BankLogs.Insert( 0, new Sandbox.UI.Tablet.BankLogEntry
        {
            Title = title,
            Amount = amount,
            Time = DateTime.Now.ToString( "HH:mm" )
        } );

        if ( BankLogs.Count > 50 )
            BankLogs.RemoveAt( BankLogs.Count - 1 );
    }

    private void SaveToDatabase()
    {
        if ( IsProxy ) return;

        var data = new PlayerData
        {
            SteamId = Game.SteamId,
            DisplayName = string.IsNullOrEmpty( DisplayName ) ? Game.SteamId.ToString() : DisplayName,
            Gender = Gender,
            DateOfBirth = DateOfBirth,
            SkinTone = SkinTone,
            Height = Height,
            Age = Age,
            Money = Money,
            JobTitle = JobTitle,
            ClothingList = JsonSerializer.Serialize( EquippedClothing ),
            HasCompletedCharacterCreation = HasCompletedCharacterCreation,
            LastSeen = DateTime.Now
        };

        DatabaseService.Instance?.SavePlayer( data );
    }

    public void ApplyClothingToDresser()
    {
        if ( _dresser == null ) return;

        var entries = new List<Sandbox.ClothingContainer.ClothingEntry>();
        foreach ( var path in EquippedClothing )
        {
            var item = ResourceLibrary.Get<Sandbox.Clothing>( path );
            if ( item != null )
                entries.Add( new Sandbox.ClothingContainer.ClothingEntry { Clothing = item } );
        }

        _dresser.Clothing = entries;
        _dresser.Apply();
    }

    public void ApplyBodyToDresser()
    {
        if ( _dresser == null ) return;

        _dresser.ManualTint = SkinTone;
        _dresser.ManualHeight = Height;
        _dresser.ManualAge = Age;
        _dresser.Apply();
    }

    private async void LoadFromDatabase()
    {
        if ( IsProxy ) return;

        var data = await DatabaseService.Instance?.GetPlayer( Game.SteamId );
        if ( data != null )
        {
            DisplayName = data.DisplayName;
            Gender = data.Gender;
            DateOfBirth = data.DateOfBirth;
            SkinTone = data.SkinTone;
            Height = data.Height;
            Age = data.Age;
            Money = data.Money;
            JobTitle = data.JobTitle;
            HasCompletedCharacterCreation = data.HasCompletedCharacterCreation;
            if ( !string.IsNullOrEmpty( data.ClothingList ) )
            {
                EquippedClothing = JsonSerializer.Deserialize<List<string>>( data.ClothingList ) ?? new();
                ApplyClothingToDresser();
            }
            ApplyBodyToDresser();
            Log.Info( $"Loaded player data for {data.DisplayName}: ${data.Money}" );
        }
        else
        {
            Log.Info( "No existing player data found, starting fresh." );
        }
    }

    protected override void OnAwake()
    {
        _playerController = Components.Get<PlayerController>();
        if ( _playerController == null )
        {
            Log.Warning( "[RPPlayer] No PlayerController found - add one as a sibling component!" );
        }

        _dresser = Components.Get<Dresser>();
        if ( _dresser == null )
        {
            Log.Warning( "[RPPlayer] No Dresser found - clothing system won't work" );
        }

        // Ensure inventory components exist (fallback if prefab doesn't load them)
        Components.GetOrCreate<PlayerInventory>();
        Components.GetOrCreate<HotbarSelector>();
    }

    protected override void OnStart()
    {
        if ( !IsProxy )
        {
            LoadFromDatabase();
        }
    }

    private TimeSince _lastSave = 0;

    protected override void OnUpdate()
    {
        if ( IsProxy ) return;

        if ( _lastSave > 30f )
        {
            SaveToDatabase();
            Sandbox.Services.Stats.SetValue( "money", Money );
            _lastSave = 0;
        }
    }
}


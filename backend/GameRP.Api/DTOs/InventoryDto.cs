namespace GameRP.Api.DTOs;

public class InventoryItemDto
{
    public string InstanceId { get; set; } = "";
    public string Container { get; set; } = "main";
    public int SlotIndex { get; set; }
    public string ItemId { get; set; } = "";
    public int Quantity { get; set; }
    public int Durability { get; set; } = -1;
    public string NbtJson { get; set; } = "{}";
}

public class InventoryDto
{
    public long SteamId { get; set; }
    public int MainSlots { get; set; } = 27;
    public int HotbarSlots { get; set; } = 9;
    public int SelectedHotbarSlot { get; set; } = 0;
    public List<InventoryItemDto> Items { get; set; } = new();
    public string RowVersion { get; set; } = "";
}

public class SaveInventoryRequest
{
    public int MainSlots { get; set; } = 27;
    public int HotbarSlots { get; set; } = 9;
    public int SelectedHotbarSlot { get; set; } = 0;
    public List<InventoryItemDto> Items { get; set; } = new();
    /// <summary>
    /// Optional: pass back the RowVersion received from a previous load to enable
    /// optimistic concurrency. Empty string = no concurrency check.
    /// </summary>
    public string RowVersion { get; set; } = "";
}

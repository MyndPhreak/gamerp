using Sandbox;
using Sandbox.UI;
using System.Collections.Generic;

namespace Sandbox.UI.Tablet;

public interface ITabletApp
{
    string AppName { get; }
    string AppIcon { get; }
    Color AppColor { get; }
}

public struct BankLogEntry
{
    public string Title { get; set; }
    public int Amount { get; set; }
    public string Time { get; set; }
    public bool IsIncome => Amount > 0;
}

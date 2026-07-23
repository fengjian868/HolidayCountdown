using ClassIsland.Core.Abstractions.Controls;
using HolidayCountdown.Models.ComponentSettings;
using HolidayCountdown.Views.SettingsPages;

namespace HolidayCountdown.Views.ComponentSettings;

public partial class WorldClockComponentSettings : ComponentBase<WorldClockSettings>
{
    public WorldClockComponentSettings()
    {
        InitializeComponent();
        RootContent.Content = new UnifiedSettingsPage(standalone: true).GetStandalonePanel("clock");
    }
}

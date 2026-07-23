using ClassIsland.Core.Abstractions.Controls;
using HolidayCountdown.Models.ComponentSettings;
using HolidayCountdown.Views.SettingsPages;

namespace HolidayCountdown.Views.ComponentSettings;

public partial class CustomHolidayComponentSettings : ComponentBase<CustomHolidaySettings>
{
    public CustomHolidayComponentSettings()
    {
        InitializeComponent();
        RootContent.Content = new UnifiedSettingsPage(standalone: true).GetStandalonePanel("custom");
    }
}

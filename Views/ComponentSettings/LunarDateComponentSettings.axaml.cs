using ClassIsland.Core.Abstractions.Controls;
using HolidayCountdown.Models.ComponentSettings;
using HolidayCountdown.Views.SettingsPages;

namespace HolidayCountdown.Views.ComponentSettings;

public partial class LunarDateComponentSettings : ComponentBase<LunarDateSettings>
{
    public LunarDateComponentSettings()
    {
        InitializeComponent();
        RootContent.Content = new UnifiedSettingsPage(standalone: true).GetStandalonePanel("lunar");
    }
}

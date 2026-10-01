using Avalonia.Controls;
using ClassIsland.Core.Abstractions.Controls;
using HolidayCountdown.Models.ComponentSettings;
using HolidayCountdown.Views.SettingsPages;

namespace HolidayCountdown.Views.ComponentSettings;

public class MinorHolidayCountdownComponentSettings : ComponentBase<MinorHolidaySettings>
{
    public MinorHolidayCountdownComponentSettings()
    {
        var page = new UnifiedSettingsPage(standalone: true);
        var panel = page.GetStandalonePanelContent("minor");
        Content = new ScrollViewer
        {
            Content = panel,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };
    }
}

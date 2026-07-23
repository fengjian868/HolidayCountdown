using ClassIsland.Core.Abstractions.Controls;
using HolidayCountdown.Models.ComponentSettings;
using HolidayCountdown.Views.SettingsPages;

namespace HolidayCountdown.Views.ComponentSettings;

public partial class StudyTimeComponentSettings : ComponentBase<StudyTimeSettings>
{
    public StudyTimeComponentSettings()
    {
        InitializeComponent();
        RootContent.Content = new UnifiedSettingsPage(standalone: true).GetStandalonePanel("study");
    }
}

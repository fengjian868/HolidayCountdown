using ClassIsland.Core.Abstractions.Controls;
using HolidayCountdown.Models.ComponentSettings;
using HolidayCountdown.Views.SettingsPages;

namespace HolidayCountdown.Views.ComponentSettings;

public partial class ExamCountdownComponentSettings : ComponentBase<ExamCountdownSettings>
{
    public ExamCountdownComponentSettings()
    {
        InitializeComponent();
        RootContent.Content = new UnifiedSettingsPage(standalone: true).GetStandalonePanel("exam");
    }
}

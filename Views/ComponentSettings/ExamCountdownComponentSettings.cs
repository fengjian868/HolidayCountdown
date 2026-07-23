using ClassIsland.Core.Abstractions.Controls;
using HolidayCountdown.Models.ComponentSettings;
using HolidayCountdown.Views.SettingsPages;

namespace HolidayCountdown.Views.ComponentSettings;

/// <summary>
/// 大考倒计时组件的原生组件设置入口。
/// 设置界面复用 UnifiedSettingsPage 的 standalone 大考面板，设置仍读写全局 PluginSettings。
/// </summary>
public class ExamCountdownComponentSettings : ComponentBase<ExamCountdownSettings>
{
    public ExamCountdownComponentSettings()
    {
        Content = new UnifiedSettingsPage(standalone: true).GetStandalonePanel("exam");
    }
}

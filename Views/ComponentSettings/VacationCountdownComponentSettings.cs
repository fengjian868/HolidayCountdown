using ClassIsland.Core.Abstractions.Controls;
using HolidayCountdown.Models.ComponentSettings;
using HolidayCountdown.Views.SettingsPages;

namespace HolidayCountdown.Views.ComponentSettings;

/// <summary>
/// 寒暑假倒计时组件的原生组件设置入口。
/// 设置界面复用 UnifiedSettingsPage 的 standalone 寒暑假面板，设置仍读写全局 PluginSettings。
/// </summary>
public class VacationCountdownComponentSettings : ComponentBase<VacationCountdownSettings>
{
    public VacationCountdownComponentSettings()
    {
        Content = new UnifiedSettingsPage(standalone: true).GetStandalonePanel("vacation");
    }
}

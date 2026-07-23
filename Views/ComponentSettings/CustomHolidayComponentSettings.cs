using ClassIsland.Core.Abstractions.Controls;
using HolidayCountdown.Models.ComponentSettings;
using HolidayCountdown.Views.SettingsPages;

namespace HolidayCountdown.Views.ComponentSettings;

/// <summary>
/// 自定义节日倒计时组件的原生组件设置入口。
/// 设置界面复用 UnifiedSettingsPage 的 standalone 自定义节日面板，设置仍读写全局 PluginSettings。
/// </summary>
public class CustomHolidayComponentSettings : ComponentBase<CustomHolidaySettings>
{
    public CustomHolidayComponentSettings()
    {
        Content = new UnifiedSettingsPage(standalone: true).GetStandalonePanel("custom");
    }
}

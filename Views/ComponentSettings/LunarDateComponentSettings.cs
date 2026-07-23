using ClassIsland.Core.Abstractions.Controls;
using HolidayCountdown.Models.ComponentSettings;
using HolidayCountdown.Views.SettingsPages;

namespace HolidayCountdown.Views.ComponentSettings;

/// <summary>
/// 农历日期组件的原生组件设置入口。
/// 设置界面复用 UnifiedSettingsPage 的 standalone 农历面板，设置仍读写全局 PluginSettings。
/// </summary>
public class LunarDateComponentSettings : ComponentBase<LunarDateSettings>
{
    public LunarDateComponentSettings()
    {
        Content = new UnifiedSettingsPage(standalone: true).GetStandalonePanel("lunar");
    }
}

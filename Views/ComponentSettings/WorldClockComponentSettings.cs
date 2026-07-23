using ClassIsland.Core.Abstractions.Controls;
using HolidayCountdown.Models.ComponentSettings;
using HolidayCountdown.Views.SettingsPages;

namespace HolidayCountdown.Views.ComponentSettings;

/// <summary>
/// 世界时钟组件的原生组件设置入口。
/// 设置界面复用 UnifiedSettingsPage 的 standalone 世界时钟面板，设置仍读写全局 PluginSettings。
/// </summary>
public class WorldClockComponentSettings : ComponentBase<WorldClockSettings>
{
    public WorldClockComponentSettings()
    {
        Content = new UnifiedSettingsPage(standalone: true).GetStandalonePanel("clock");
    }
}

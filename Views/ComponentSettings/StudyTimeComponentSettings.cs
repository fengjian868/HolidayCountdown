using ClassIsland.Core.Abstractions.Controls;
using HolidayCountdown.Models.ComponentSettings;
using HolidayCountdown.Views.SettingsPages;

namespace HolidayCountdown.Views.ComponentSettings;

/// <summary>
/// 学习时长统计组件的原生组件设置入口。
/// 设置界面复用 UnifiedSettingsPage 的 standalone 学习时长面板，设置仍读写全局 PluginSettings。
/// </summary>
public class StudyTimeComponentSettings : ComponentBase<StudyTimeSettings>
{
    public StudyTimeComponentSettings()
    {
        Content = new UnifiedSettingsPage(standalone: true).GetStandalonePanel("study");
    }
}

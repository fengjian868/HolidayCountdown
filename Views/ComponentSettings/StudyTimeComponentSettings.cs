using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using ClassIsland.Core.Abstractions.Controls;
using HolidayCountdown.Models.ComponentSettings;
using HolidayCountdown.Views.SettingsPages;

namespace HolidayCountdown.Views.ComponentSettings;

/// <summary>
/// 学习时长统计组件的原生组件设置入口。
/// 设置界面复用 UnifiedSettingsPage 的 standalone 学习时长面板内容（不含 SettingsPageBase 外壳）。
/// </summary>
public class StudyTimeComponentSettings : ComponentBase<StudyTimeSettings>
{
    public StudyTimeComponentSettings()
    {
        var page = new UnifiedSettingsPage(standalone: true);
        var panel = page.GetStandalonePanelContent("study");
        Content = new ScrollViewer
        {
            Content = panel,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };
    }
}

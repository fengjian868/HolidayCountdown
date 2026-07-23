using Avalonia.Controls;
using ClassIsland.Core.Abstractions.Controls;
using HolidayCountdown.Models.ComponentSettings;
using HolidayCountdown.Views.SettingsPages;

namespace HolidayCountdown.Views.ComponentSettings;

/// <summary>
/// 农历日期组件的原生组件设置入口。
/// 设置界面复用 UnifiedSettingsPage 的 standalone 农历面板内容（不含 SettingsPageBase 外壳），
/// 避免组件设置抽屉中 SettingsPageBase 样式/资源冲突导致 ToggleSwitch 模板异常。
/// </summary>
public class LunarDateComponentSettings : ComponentBase<LunarDateSettings>
{
    public LunarDateComponentSettings()
    {
        var page = new UnifiedSettingsPage(standalone: true);
        var panel = page.GetStandalonePanelContent("lunar");
        Content = new ScrollViewer
        {
            Content = panel,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };
    }
}

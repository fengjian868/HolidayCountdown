using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Abstractions.Controls;
using HolidayCountdown.Services;

namespace HolidayCountdown.Views.SettingsPages;

[SettingsPageInfo("holidaycountdown.solarterm", "24节气设置", "\uE9CA", "\uE9CA")]
public class SolarTermSettingsPage : SettingsPageBase
{
    private readonly HolidayService _svc;
    public SolarTermSettingsPage() { _svc = new HolidayService(); Content = Build(); }

    Control Build()
    {
        var s = new StackPanel { Spacing = 0, Margin = new Thickness(20, 16) };
        s.Children.Add(SettingsUI.PageHeader("🌿 24节气设置"));

        // 颜色
        var colorPanel = new StackPanel { Spacing = 0 };
        var colors = _svc.Settings.TermColors.OrderBy(x => x.Key).ToList();
        for (int i = 0; i < colors.Count; i++)
        {
            var kv = colors[i];
            var key = kv.Key;
            colorPanel.Children.Add(SettingsUI.SettingItem(key, null,
                SettingsUI.Color(kv.Value, c => _svc.Settings.TermColors[key] = c)));
            if (i < colors.Count - 1)
                colorPanel.Children.Add(SettingsUI.Separator());
        }
        s.Children.Add(SettingsUI.Expander("颜色", "各节气显示颜色自定义", colorPanel));

        s.Children.Add(SettingsUI.SaveButton(() => _svc.SaveSettings()));
        return new ScrollViewer { Content = s };
    }
}

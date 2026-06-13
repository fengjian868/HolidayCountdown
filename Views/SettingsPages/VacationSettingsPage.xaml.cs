using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Abstractions.Controls;
using HolidayCountdown.Services;

namespace HolidayCountdown.Views.SettingsPages;

[SettingsPageInfo("holidaycountdown.vacation", "寒暑假设置", "\uE7BE", "\uE7BE")]
public class VacationSettingsPage : SettingsPageBase
{
    private readonly HolidayService _svc;
    public VacationSettingsPage() { _svc = new HolidayService(); Content = Build(); }

    Control Build()
    {
        var s = new StackPanel { Spacing = 0, Margin = new Thickness(20, 16) };
        s.Children.Add(SettingsUI.PageHeader("🏖️ 寒暑假设置"));

        // 开关
        var togglePanel = new StackPanel { Spacing = 0 };
        togglePanel.Children.Add(SettingsUI.SettingItem("显示寒暑假倒计时", null,
            SettingsUI.Toggle(_svc.Settings.ShowVacationCountdown, v => _svc.Settings.ShowVacationCountdown = v)));
        s.Children.Add(SettingsUI.Expander("开关", "寒暑假组件总开关", togglePanel));

        // 暑假
        var summerPanel = new StackPanel { Spacing = 0 };
        summerPanel.Children.Add(SettingsUI.SettingItem("开始日期", null,
            SettingsUI.Date(_svc.Settings.SummerStart, v => _svc.Settings.SummerStart = v)));
        summerPanel.Children.Add(SettingsUI.Separator());
        summerPanel.Children.Add(SettingsUI.SettingItem("结束日期", null,
            SettingsUI.Date(_svc.Settings.SummerEnd, v => _svc.Settings.SummerEnd = v)));
        s.Children.Add(SettingsUI.Expander("暑假", "暑假时间安排", summerPanel));

        // 寒假
        var winterPanel = new StackPanel { Spacing = 0 };
        winterPanel.Children.Add(SettingsUI.SettingItem("开始日期", null,
            SettingsUI.Date(_svc.Settings.WinterStart, v => _svc.Settings.WinterStart = v)));
        winterPanel.Children.Add(SettingsUI.Separator());
        winterPanel.Children.Add(SettingsUI.SettingItem("结束日期", null,
            SettingsUI.Date(_svc.Settings.WinterEnd, v => _svc.Settings.WinterEnd = v)));
        s.Children.Add(SettingsUI.Expander("寒假", "寒假时间安排", winterPanel));

        s.Children.Add(SettingsUI.SaveButton(() => _svc.SaveSettings()));
        return new ScrollViewer { Content = s };
    }
}

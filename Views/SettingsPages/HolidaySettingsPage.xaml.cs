using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Abstractions.Controls;
using HolidayCountdown.Services;

namespace HolidayCountdown.Views.SettingsPages;

[SettingsPageInfo("holidaycountdown.holiday", "节假日设置", "\uE8F5", "\uE8F5")]
public class HolidaySettingsPage : SettingsPageBase
{
    private readonly HolidayService _svc;
    public HolidaySettingsPage() { _svc = new HolidayService(); Content = Build(); }

    Control Build()
    {
        var s = new StackPanel { Spacing = 0, Margin = new Thickness(20, 16) };
        s.Children.Add(SettingsUI.PageHeader("📅 节假日倒计时设置"));

        // 显示设置
        var displayPanel = new StackPanel { Spacing = 0 };
        displayPanel.Children.Add(SettingsUI.SettingItem("显示数量", "同时显示多少个节日",
            SettingsUI.Combo(new[] { "1", "3", "5" }, _svc.Settings.DisplayCount == 1 ? 0 : _svc.Settings.DisplayCount == 3 ? 1 : 2,
                v => _svc.Settings.DisplayCount = v == 0 ? 1 : v == 1 ? 3 : 5)));
        displayPanel.Children.Add(SettingsUI.Separator());
        displayPanel.Children.Add(SettingsUI.SettingItem("显示放假天数", "如：春节（放7天）",
            SettingsUI.Toggle(_svc.Settings.ShowDaysOff, v => _svc.Settings.ShowDaysOff = v)));
        displayPanel.Children.Add(SettingsUI.Separator());
        displayPanel.Children.Add(SettingsUI.SettingItem("显示小时数", "节日当天显示剩余小时",
            SettingsUI.Toggle(_svc.Settings.ShowHours, v => _svc.Settings.ShowHours = v)));
        displayPanel.Children.Add(SettingsUI.Separator());
        displayPanel.Children.Add(SettingsUI.SettingItem("显示进度环", "首个节日显示弧形进度",
            SettingsUI.Toggle(_svc.Settings.ShowProgressRing, v => _svc.Settings.ShowProgressRing = v)));
        displayPanel.Children.Add(SettingsUI.Separator());
        displayPanel.Children.Add(SettingsUI.SettingItem("自动播放下一个", "节日过后自动显示下一个",
            SettingsUI.Toggle(_svc.Settings.AutoNextHoliday, v => _svc.Settings.AutoNextHoliday = v)));
        displayPanel.Children.Add(SettingsUI.Separator());
        displayPanel.Children.Add(SettingsUI.SettingItem("显示假期占比", "当年剩余假期百分比",
            SettingsUI.Toggle(_svc.Settings.ShowYearRatio, v => _svc.Settings.ShowYearRatio = v)));
        displayPanel.Children.Add(SettingsUI.Separator());
        displayPanel.Children.Add(SettingsUI.SettingItem("周末倒计时", "列表中显示周六周日",
            SettingsUI.Toggle(_svc.Settings.ShowWeekendCountdown, v => _svc.Settings.ShowWeekendCountdown = v)));
        s.Children.Add(SettingsUI.Expander("显示", "节假日组件的显示选项", displayPanel));

        // 调休设置
        var workdayPanel = new StackPanel { Spacing = 0 };
        workdayPanel.Children.Add(SettingsUI.SettingItem("调休提醒", "周末调休上课提前提醒",
            SettingsUI.Toggle(_svc.Settings.ShowWorkdayReminder, v => _svc.Settings.ShowWorkdayReminder = v)));
        workdayPanel.Children.Add(SettingsUI.Separator());
        workdayPanel.Children.Add(SettingsUI.SettingItem("提前提醒天数", "调休提醒提前多少天显示",
            SettingsUI.Number(_svc.Settings.WorkdayReminderDays, 1, 30, v => _svc.Settings.WorkdayReminderDays = v)));
        s.Children.Add(SettingsUI.Expander("调休", "调休上课提醒设置", workdayPanel));

        // 颜色设置
        var colorPanel = new StackPanel { Spacing = 0 };
        colorPanel.Children.Add(SettingsUI.SettingItem("自动节日颜色", "根据节日自动匹配颜色",
            SettingsUI.Toggle(_svc.Settings.AutoHolidayColor, v => _svc.Settings.AutoHolidayColor = v)));
        colorPanel.Children.Add(SettingsUI.Separator());
        foreach (var kv in _svc.Settings.HolidayColors.ToList())
        {
            var key = kv.Key;
            colorPanel.Children.Add(SettingsUI.SettingItem(key, null,
                SettingsUI.ColorPicker(kv.Value, c => _svc.Settings.HolidayColors[key] = c)));
            if (key != _svc.Settings.HolidayColors.Keys.Last())
                colorPanel.Children.Add(SettingsUI.Separator());
        }
        s.Children.Add(SettingsUI.Expander("颜色", "节日颜色自定义", colorPanel));

        // 节日开关
        var switchPanel = new StackPanel { Spacing = 0 };
        var allHolidays = new[] { "元旦", "春节", "清明节", "劳动节", "端午节", "中秋节", "国庆节" };
        foreach (var name in allHolidays)
        {
            var disabled = _svc.Settings.DisabledHolidays.Contains(name);
            var chk = new CheckBox { Content = name, IsChecked = !disabled };
            chk.IsCheckedChanged += (a, b) =>
            {
                if (chk.IsChecked == true) _svc.Settings.DisabledHolidays.Remove(name);
                else if (!_svc.Settings.DisabledHolidays.Contains(name)) _svc.Settings.DisabledHolidays.Add(name);
                _svc.SaveSettings();
            };
            switchPanel.Children.Add(SettingsUI.SettingItem(name, null, chk));
            if (name != allHolidays.Last())
                switchPanel.Children.Add(SettingsUI.Separator());
        }
        s.Children.Add(SettingsUI.Expander("节日开关", "选择要显示的节假日", switchPanel));

        s.Children.Add(SettingsUI.SaveButton(() => _svc.SaveSettings()));
        return new ScrollViewer { Content = s };
    }
}

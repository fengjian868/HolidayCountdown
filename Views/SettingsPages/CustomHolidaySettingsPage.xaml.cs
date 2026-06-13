using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Abstractions.Controls;
using HolidayCountdown.Models;
using HolidayCountdown.Services;

namespace HolidayCountdown.Views.SettingsPages;

[SettingsPageInfo("holidaycountdown.customholiday", "自定义节日", "\uE915", "\uE915")]
public class CustomHolidaySettingsPage : SettingsPageBase
{
    private readonly HolidayService _svc;
    public CustomHolidaySettingsPage() { _svc = new HolidayService(); Content = Build(); }

    Control Build()
    {
        var s = new StackPanel { Spacing = 0, Margin = new Thickness(20, 16) };
        s.Children.Add(SettingsUI.PageHeader("🎂 自定义节日设置"));

        // 组件显示
        var displayPanel = new StackPanel { Spacing = 0 };
        displayPanel.Children.Add(SettingsUI.SettingItem("显示数量", "同时显示多少个自定义节日",
            SettingsUI.Combo(new[] { "1", "2", "3", "5" },
                _svc.Settings.CustomHolidayDisplayCount == 1 ? 0 :
                _svc.Settings.CustomHolidayDisplayCount == 2 ? 1 :
                _svc.Settings.CustomHolidayDisplayCount == 3 ? 2 : 3,
                v => _svc.Settings.CustomHolidayDisplayCount = v == 0 ? 1 : v == 1 ? 2 : v == 2 ? 3 : 5)));
        displayPanel.Children.Add(SettingsUI.Separator());
        displayPanel.Children.Add(SettingsUI.SettingItem("显示图标", null,
            SettingsUI.Toggle(_svc.Settings.CustomHolidayShowIcon, v => _svc.Settings.CustomHolidayShowIcon = v)));
        displayPanel.Children.Add(SettingsUI.Separator());
        displayPanel.Children.Add(SettingsUI.SettingItem("显示天数", null,
            SettingsUI.Toggle(_svc.Settings.CustomHolidayShowDays, v => _svc.Settings.CustomHolidayShowDays = v)));
        s.Children.Add(SettingsUI.Expander("组件显示", "自定义节日组件显示选项", displayPanel));

        // 节日列表
        s.Children.Add(SettingsUI.Expander("节日列表", "添加和管理你的自定义节日", BuildList()));

        s.Children.Add(SettingsUI.SaveButton(() => _svc.SaveSettings()));
        return new ScrollViewer { Content = s };
    }

    Control BuildList()
    {
        var p = new StackPanel { Spacing = 0 };
        var holidays = _svc.Settings.CustomHolidays.ToList();
        for (int i = 0; i < holidays.Count; i++)
        {
            p.Children.Add(MakeItem(holidays[i]));
            if (i < holidays.Count - 1)
                p.Children.Add(SettingsUI.Separator());
        }
        var btn = new Button { Content = "➕ 添加", Padding = new Thickness(12, 6), HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(16, 8, 16, 8) };
        btn.Click += (a, e) =>
        {
            var h = new CustomHoliday { Name = "新节日", Date = DateTime.Now.AddDays(1) };
            _svc.Settings.CustomHolidays.Add(h);
            // 重新构建列表
            Content = Build();
        };
        p.Children.Add(btn);
        return p;
    }

    Control MakeItem(CustomHoliday h)
    {
        var g = new Grid { ColumnDefinitions = new ColumnDefinitions("120 100 80 Auto Auto"), Margin = new Thickness(16, 8, 16, 8) };
        var n = new TextBox { Text = h.Name, Margin = new Thickness(0, 0, 8, 0) };
        n.TextChanged += (a, b) => h.Name = n.Text ?? "";
        Grid.SetColumn(n, 0);

        var dateText = new TextBlock { Text = $"{h.Date.Month}月{h.Date.Day}日", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) };
        Grid.SetColumn(dateText, 1);

        var d = new DatePicker { SelectedDate = h.Date, Margin = new Thickness(0, 0, 8, 0) };
        d.SelectedDateChanged += (a, b) =>
        {
            if (d.SelectedDate.HasValue)
            {
                h.Date = d.SelectedDate.Value.DateTime;
                dateText.Text = $"{h.Date.Month}月{h.Date.Day}日";
            }
        };
        Grid.SetColumn(d, 2);

        var r = new CheckBox { Content = "每年", IsChecked = h.RepeatYearly, VerticalAlignment = VerticalAlignment.Center };
        r.IsCheckedChanged += (a, b) => h.RepeatYearly = r.IsChecked == true;
        Grid.SetColumn(r, 3);

        var del = new Button { Content = "删除", Width = 50 };
        del.Click += (a, b) => { _svc.Settings.CustomHolidays.Remove(h); Content = Build(); };
        Grid.SetColumn(del, 4);

        g.Children.Add(n); g.Children.Add(dateText); g.Children.Add(d); g.Children.Add(r); g.Children.Add(del);
        return g;
    }
}

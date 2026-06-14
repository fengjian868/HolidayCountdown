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

[SettingsPageInfo("holidaycountdown.greeting", "问候语设置", "\uE9D2", "\uE9D2")]
public class GreetingSettingsPage : SettingsPageBase
{
    private readonly HolidayService _svc;
    public GreetingSettingsPage() { _svc = new HolidayService(); Content = Build(); }

    Control Build()
    {
        var s = new StackPanel { Spacing = 0, Margin = new Thickness(20, 16) };
        s.Children.Add(SettingsUI.PageHeader("💬 问候语设置"));

        // 开关
        var togglePanel = new StackPanel { Spacing = 0 };
        togglePanel.Children.Add(SettingsUI.SettingItem("启用问候语", null,
            SettingsUI.Toggle(_svc.Settings.ShowGreeting, v => _svc.Settings.ShowGreeting = v)));
        togglePanel.Children.Add(SettingsUI.Separator());
        togglePanel.Children.Add(SettingsUI.SettingItem("合并到节假日组件", "问候语显示在节假日下方",
            SettingsUI.Toggle(_svc.Settings.MergeGreeting, v => _svc.Settings.MergeGreeting = v)));
        s.Children.Add(SettingsUI.Expander("开关", "问候语基础设置", togglePanel));

        // 每周提醒
        var weeklyPanel = new StackPanel { Spacing = 0 };
        weeklyPanel.Children.Add(SettingsUI.SettingItem("启用每周提醒", null,
            SettingsUI.Toggle(_svc.Settings.WeeklyReminderEnabled, v => _svc.Settings.WeeklyReminderEnabled = v)));
        weeklyPanel.Children.Add(SettingsUI.Separator());
        var dayCombo = new ComboBox { Width = 80, HorizontalAlignment = HorizontalAlignment.Right };
        var days = new[] { "周一", "周二", "周三", "周四", "周五", "周六", "周日" };
        foreach (var d in days) dayCombo.Items.Add(d);
        dayCombo.SelectedIndex = _svc.Settings.WeeklyReminderDay == 7 ? 6 : _svc.Settings.WeeklyReminderDay - 1;
        dayCombo.SelectionChanged += (a, b) => _svc.Settings.WeeklyReminderDay = dayCombo.SelectedIndex == 6 ? 7 : dayCombo.SelectedIndex + 1;
        weeklyPanel.Children.Add(SettingsUI.SettingItem("提醒日期", "每周哪天显示提醒", dayCombo));
        weeklyPanel.Children.Add(SettingsUI.Separator());
        weeklyPanel.Children.Add(SettingsUI.SettingItem("开始时间（时）", "提醒开始显示的小时",
            SettingsUI.Number(_svc.Settings.WeeklyReminderStartHour, 0, 23, v => _svc.Settings.WeeklyReminderStartHour = v)));
        weeklyPanel.Children.Add(SettingsUI.Separator());
        weeklyPanel.Children.Add(SettingsUI.SettingItem("结束时间（时）", "提醒结束显示的小时",
            SettingsUI.Number(_svc.Settings.WeeklyReminderEndHour, 0, 23, v => _svc.Settings.WeeklyReminderEndHour = v)));
        weeklyPanel.Children.Add(SettingsUI.Separator());
        weeklyPanel.Children.Add(SettingsUI.Info("内置提醒按标签分类，每天自动从本地随机刷新一条。标签：周一/周二/周三/周四/周五/周末"));
        s.Children.Add(SettingsUI.Expander("每周提醒", "自定义每周提醒的日期和时段", weeklyPanel));

        // 放学
        var schoolPanel = new StackPanel { Spacing = 0 };
        schoolPanel.Children.Add(SettingsUI.SettingItem("放学时间", "时:分",
            new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4, HorizontalAlignment = HorizontalAlignment.Right }.Also(h =>
            {
                h.Children.Add(SettingsUI.Text(_svc.Settings.SchoolEndHour.ToString("D2"), 40, v =>
                {
                    if (int.TryParse(v, out var hval)) _svc.Settings.SchoolEndHour = Math.Max(0, Math.Min(23, hval));
                }));
                h.Children.Add(new TextBlock { Text = ":", VerticalAlignment = VerticalAlignment.Center });
                h.Children.Add(SettingsUI.Text(_svc.Settings.SchoolEndMinute.ToString("D2"), 40, v =>
                {
                    if (int.TryParse(v, out var mval)) _svc.Settings.SchoolEndMinute = Math.Max(0, Math.Min(59, mval));
                }));
            })));
        schoolPanel.Children.Add(SettingsUI.Separator());
        schoolPanel.Children.Add(SettingsUI.SettingItem("提前提醒分钟", "放学前多少分钟切换提醒",
            SettingsUI.Number(_svc.Settings.SchoolEndReminderMinutes, 1, 60, v => _svc.Settings.SchoolEndReminderMinutes = v)));
        schoolPanel.Children.Add(SettingsUI.Separator());
        schoolPanel.Children.Add(SettingsUI.SettingItem("放学前文案", null,
            SettingsUI.Text(_svc.Settings.BeforeSchoolEndText, 200, v => _svc.Settings.BeforeSchoolEndText = v)));
        schoolPanel.Children.Add(SettingsUI.Separator());
        schoolPanel.Children.Add(SettingsUI.SettingItem("放学后文案", null,
            SettingsUI.Text(_svc.Settings.AfterSchoolEndText, 200, v => _svc.Settings.AfterSchoolEndText = v)));
        s.Children.Add(SettingsUI.Expander("放学", "放学提醒设置", schoolPanel));

        // 时段文案
        s.Children.Add(SettingsUI.Expander("时段文案", "自定义多个时间段的问候语", BuildTimeSlotPanel()));

        // 特殊日期
        s.Children.Add(SettingsUI.Expander("特殊日期", "设置特定星期几的问候语", BuildSpecialDatePanel()));

        s.Children.Add(SettingsUI.SaveButton(() => _svc.SaveSettings()));
        return new ScrollViewer { Content = s };
    }

    StackPanel BuildTimeSlotPanel()
    {
        var panel = new StackPanel { Spacing = 8 };
        var listPanel = new StackPanel { Spacing = 0 };

        void RefreshList()
        {
            listPanel.Children.Clear();
            var slots = _svc.Settings.TimeSlotGreetings.OrderBy(x => x.StartHour * 60 + x.StartMinute).ToList();
            for (int i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, Margin = new Thickness(16, 8, 16, 8) };
                var startBox = SettingsUI.Text($"{slot.StartHour:D2}:{slot.StartMinute:D2}", 50, v =>
                {
                    if (TimeSpan.TryParse(v, out var ts)) { slot.StartHour = ts.Hours; slot.StartMinute = ts.Minutes; }
                });
                var endBox = SettingsUI.Text($"{slot.EndHour:D2}:{slot.EndMinute:D2}", 50, v =>
                {
                    if (TimeSpan.TryParse(v, out var ts)) { slot.EndHour = ts.Hours; slot.EndMinute = ts.Minutes; }
                });
                var textBox = SettingsUI.Text(slot.Text, 180, v => slot.Text = v);
                var delBtn = new Button { Content = "🗑️", Padding = new Thickness(4, 2) };
                delBtn.Click += (a, e) => { _svc.Settings.TimeSlotGreetings.Remove(slot); RefreshList(); };

                row.Children.Add(new TextBlock { Text = "从", VerticalAlignment = VerticalAlignment.Center, Opacity = 0.6, FontSize = 11 });
                row.Children.Add(startBox);
                row.Children.Add(new TextBlock { Text = "到", VerticalAlignment = VerticalAlignment.Center, Opacity = 0.6, FontSize = 11 });
                row.Children.Add(endBox);
                row.Children.Add(textBox);
                row.Children.Add(delBtn);
                listPanel.Children.Add(row);
                if (i < slots.Count - 1)
                    listPanel.Children.Add(SettingsUI.Separator());
            }
        }

        RefreshList();
        panel.Children.Add(listPanel);

        var addBtn = new Button { Content = "+ 添加时段", Padding = new Thickness(12, 4), HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(16, 4, 16, 8) };
        addBtn.Click += (a, e) =>
        {
            _svc.Settings.TimeSlotGreetings.Add(new TimeSlotGreeting { StartHour = 8, StartMinute = 0, EndHour = 12, EndMinute = 0, Text = "" });
            RefreshList();
        };
        panel.Children.Add(addBtn);

        panel.Children.Add(SettingsUI.Info("留空的时段会自动使用本地数据库按标签（早晨/上午/中午/下午/傍晚/夜晚）每天刷新一条问候语"));

        return panel;
    }

    StackPanel BuildSpecialDatePanel()
    {
        var panel = new StackPanel { Spacing = 8 };
        var listPanel = new StackPanel { Spacing = 0 };

        void RefreshList()
        {
            listPanel.Children.Clear();
            var items = _svc.Settings.SpecialDateGreetings.ToList();
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                var row = new StackPanel { Spacing = 4, Margin = new Thickness(16, 8, 16, 8) };

                var headerRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
                var nameBox = SettingsUI.Text(item.Name, 80, v => item.Name = v);
                var dayCombo = new ComboBox { Width = 70 };
                var days = new[] { "周一", "周二", "周三", "周四", "周五", "周六", "周日" };
                foreach (var d in days) dayCombo.Items.Add(d);
                dayCombo.SelectedIndex = Math.Max(0, Math.Min(6, item.DayOfWeek - 1));
                dayCombo.SelectionChanged += (a, b) => item.DayOfWeek = dayCombo.SelectedIndex + 1;
                var enabledChk = new CheckBox { Content = "启用", IsChecked = item.Enabled };
                enabledChk.IsCheckedChanged += (a, b) => item.Enabled = enabledChk.IsChecked == true;
                var delBtn = new Button { Content = "🗑️", Padding = new Thickness(4, 2) };
                delBtn.Click += (a, e) => { _svc.Settings.SpecialDateGreetings.Remove(item); RefreshList(); };

                headerRow.Children.Add(nameBox);
                headerRow.Children.Add(dayCombo);
                headerRow.Children.Add(enabledChk);
                headerRow.Children.Add(delBtn);
                row.Children.Add(headerRow);

                var timeRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
                var startBox = SettingsUI.Text($"{item.StartHour:D2}:{item.StartMinute:D2}", 50, v =>
                {
                    if (TimeSpan.TryParse(v, out var ts)) { item.StartHour = ts.Hours; item.StartMinute = ts.Minutes; }
                });
                var endBox = SettingsUI.Text($"{item.EndHour:D2}:{item.EndMinute:D2}", 50, v =>
                {
                    if (TimeSpan.TryParse(v, out var ts)) { item.EndHour = ts.Hours; item.EndMinute = ts.Minutes; }
                });
                var textBox = SettingsUI.Text(item.Text, 180, v => item.Text = v);

                timeRow.Children.Add(new TextBlock { Text = "从", VerticalAlignment = VerticalAlignment.Center, Opacity = 0.6, FontSize = 11 });
                timeRow.Children.Add(startBox);
                timeRow.Children.Add(new TextBlock { Text = "到", VerticalAlignment = VerticalAlignment.Center, Opacity = 0.6, FontSize = 11 });
                timeRow.Children.Add(endBox);
                timeRow.Children.Add(textBox);
                row.Children.Add(timeRow);

                listPanel.Children.Add(row);
                if (i < items.Count - 1)
                    listPanel.Children.Add(SettingsUI.Separator());
            }
        }

        RefreshList();
        panel.Children.Add(listPanel);

        var addBtn = new Button { Content = "+ 添加特殊日期", Padding = new Thickness(12, 4), HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(16, 4, 16, 8) };
        addBtn.Click += (a, e) =>
        {
            _svc.Settings.SpecialDateGreetings.Add(new SpecialDateGreeting { Name = "新日期", DayOfWeek = 1, StartHour = 0, StartMinute = 0, EndHour = 23, EndMinute = 59, Text = "" });
            RefreshList();
        };
        panel.Children.Add(addBtn);

        return panel;
    }
}

public static class PanelExt
{
    public static T Also<T>(this T t, Action<T> a) { a(t); return t; }
}
